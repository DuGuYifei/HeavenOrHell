using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using utils;

namespace Minimap
{
    public class MiniMapController : MonoBehaviour
    {
        [SerializeField] private RawImage minimapDarkMaskRawImage;
        [SerializeField] private RawImage minimapRawImage;
        [SerializeField] private int width = 31;
        [SerializeField] private int height = 31;
        [SerializeField] private float minimapImageSize = 280f;
        [SerializeField] private int darkMaskScale = 10;
        [SerializeField] private int soulRevealRange = 1;
        [SerializeField] private RectTransform gateParent;
        [SerializeField] private Image gatePrefab;
        [SerializeField] private RectTransform playerParent;
        [SerializeField] private Image playerPrefab;
        [SerializeField] private Image otherPlayerPrefab; // icon for other players
        [SerializeField] private Vector2 maximizedSizeDelta;
        [SerializeField] private Vector2 maximizedAnchoredPosition;
        [SerializeField] private Vector2 maximizedAnchorMin;
        [SerializeField] private Vector2 maximizedAnchorMax;

        private Texture2D _minimapDarkMaskTexture;
        private CharacterContainer _player;
        private RectTransform _minimapPlayer;
        private RectTransform _minimapTransform;

        private Vector2 _minimizedSizeDelta;
        private Vector2 _minimizedAnchoredPosition;
        private Vector2 _minimizedAnchorMin;
        private Vector2 _minimizedAnchorMax;
        private float _instanceSize;

        private readonly Color _playerColor = new (163 / 255f, 110 / 255f, 52 / 255f);

        // store icons of other players keyed by their unique id
        private readonly Dictionary<int, RectTransform> _otherPlayerIcons = new();

        // reaper-only reveal control
        [SerializeField] private float revealCycle = 30f;      // total cycle length (seconds)
        [SerializeField] private float revealDuration = 3f;     // visible window within cycle (seconds)
        private float _revealTimer;
        private bool _isReaper;

        public void SetMinimapTexture(Texture2D texture)
        {
            minimapRawImage.texture = texture;
        }

        public void InitializeMinimapDarkMask(CharacterContainer player, List<Vector3> gatePositions)
        {
            _player = player;
            _isReaper = player is ReaperContainer;

            var playerPos = player.transform.position / Consts.MapScale;
            _minimapPlayer = Instantiate(playerPrefab, playerParent).transform as RectTransform;
            SetPrefabPos(_minimapPlayer, playerPos);
            if (player is ReaperContainer)
            {
                minimapDarkMaskRawImage.gameObject.SetActive(false);
            }
            else
            {
                _minimapDarkMaskTexture = new Texture2D(width * darkMaskScale, height * darkMaskScale);
                for (int x = 0; x < width * darkMaskScale; x++)
                    for (int y = 0; y < height * darkMaskScale; y++)
                        _minimapDarkMaskTexture.SetPixel(x, y, new Color(0, 0, 0, 1));

                UpdateDarkMask(playerPos);
                _minimapDarkMaskTexture.filterMode = FilterMode.Point;
                minimapDarkMaskRawImage.texture = _minimapDarkMaskTexture;
            }

            _instanceSize = 0.5f / width;

            // set gates
            foreach (var gatePosition in gatePositions)
            {
                var gate = Instantiate(gatePrefab, gateParent);
                var gatePos = gatePosition / Consts.MapScale;
                SetPrefabPos(gate.rectTransform, gatePos);
            }
            _minimapTransform = transform as RectTransform;
            _minimizedSizeDelta = _minimapTransform.sizeDelta;
            _minimizedAnchoredPosition = _minimapTransform.anchoredPosition;
            _minimizedAnchorMin = _minimapTransform.anchorMin;
            _minimizedAnchorMax = _minimapTransform.anchorMax;
        }

        private void Update()
        {
            if (!_player) return;
            var playerPos = _player.transform.position / Consts.MapScale;
            SetPrefabPos(_minimapPlayer, playerPos);
            if (_player is not ReaperContainer) UpdateDarkMask(playerPos);

            // handle reaper reveal cycle
            if (_isReaper)
            {
                _revealTimer += Time.deltaTime;
                bool showOthers = _revealTimer < revealDuration;

                // ensure icons exist & positions updated first
                UpdateOtherPlayers(showOthers);

                // then toggle visibility so newly created icons respect current state
                SetOthersActive(showOthers);

                if (_revealTimer >= revealCycle)
                {
                    _revealTimer = 0f;
                }
            }

        }

        private void SetPrefabPos(RectTransform instanceTransform, Vector2 pos)
        {
            instanceTransform.anchorMin = pos / width - new Vector2(_instanceSize, _instanceSize);
            instanceTransform.anchorMax = pos / width + new Vector2(_instanceSize, _instanceSize);
        }

        private void UpdateDarkMask(Vector2 playerPos)
        {
            for (int x = -soulRevealRange * darkMaskScale; x <= soulRevealRange * darkMaskScale; x++)
            {
                for (int y = -soulRevealRange * darkMaskScale; y <= soulRevealRange * darkMaskScale; y++)
                {
                    int posX = Mathf.RoundToInt(playerPos.x * darkMaskScale) + x;
                    int posY = Mathf.RoundToInt(playerPos.y * darkMaskScale) + y;
                    if (posX >= 0 && posX < width * darkMaskScale && posY >= 0 && posY < height * darkMaskScale)
                    {
                        _minimapDarkMaskTexture.SetPixel(posX, posY, new Color(0, 0, 0, 0));
                    }
                }
            }
            _minimapDarkMaskTexture.Apply();
        }

        // -------------------- Other players --------------------

        private void UpdateOtherPlayers(bool iconsActive)
        {
            if (!_isReaper) return; // only reaper can see others

            // iterate game characters, register icons for new ones and update positions
            var characters = GameManager.Instance ? GameManager.Instance.Characters : null;
            if (characters == null) return;

            foreach (var character in characters)
            {
                if (character == null || character.isPlayer) continue;

                // ensure icon exists
                if (!_otherPlayerIcons.TryGetValue(character.id, out var icon) || icon == null)
                {
                    // instantiate new icon
                    icon = Instantiate(otherPlayerPrefab, playerParent).transform as RectTransform;
                    icon.gameObject.SetActive(iconsActive);
                    _otherPlayerIcons[character.id] = icon;
                }

                // update position
                var pos = character.transform.position / Consts.MapScale;
                SetPrefabPos(icon, pos);
            }

            // clean up icons whose characters are gone
            var idsToRemove = new List<int>();
            foreach (var kv in _otherPlayerIcons)
            {
                var exists = characters.Exists(c => c.id == kv.Key && !c.isPlayer);
                if (!exists)
                {
                    Destroy(kv.Value.gameObject);
                    idsToRemove.Add(kv.Key);
                }
            }
            foreach (var id in idsToRemove)
            {
                _otherPlayerIcons.Remove(id);
            }
        }

        private void SetOthersActive(bool active)
        {
            foreach (var icon in _otherPlayerIcons.Values)
            {
                if (icon) icon.gameObject.SetActive(active);
            }
        }

        public void SetVisibility(bool visible)
        {
            if (visible)
            {
                _minimapTransform.sizeDelta = maximizedSizeDelta;
                _minimapTransform.anchoredPosition = maximizedAnchoredPosition;
                _minimapTransform.anchorMin = maximizedAnchorMin;
                _minimapTransform.anchorMax = maximizedAnchorMax;
            }
            else
            {
                _minimapTransform.sizeDelta = _minimizedSizeDelta;
                _minimapTransform.anchoredPosition = _minimizedAnchoredPosition;
                _minimapTransform.anchorMin = _minimizedAnchorMin;
                _minimapTransform.anchorMax = _minimizedAnchorMax;
            }
        }
    }
}