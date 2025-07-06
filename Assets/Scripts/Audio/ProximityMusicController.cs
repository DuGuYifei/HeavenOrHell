using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Audio
{
    public class ProximityMusicController : MonoBehaviour
    {
        [Tooltip("Distance threshold (meters) below which tense music is triggered")] [SerializeField]
        private float switchDistance = 7f;

        [Tooltip("Name of calm music clip (must exist in AudioManager.twoDSounds)")] [SerializeField]
        private string calmMusicName = "BackgroundMusic";

        [Tooltip("Name of tense music clip (must exist in AudioManager.twoDSounds)")] [SerializeField]
        private string tenseMusicName = "TenseMusic";

        [Tooltip("Cross-fade duration (seconds)")] [SerializeField]
        private float fadeDuration = 0.5f;

        private CharacterContainer _player; // local player
        private List<CharacterContainer> _allCharacters; // all characters in scene
        private ReaperContainer _reaper; // the single Reaper instance on map
        private bool _playerIsReaper;
        private AudioSource _calmSrc;
        private AudioSource _tenseSrc;
        private bool _initialized;
        private bool _isTense; // whether we are in tense state currently
        private Coroutine _fadeCoroutine;

        private void Start()
        {
            // Subscribe to game initialization event
            if (GameManager.Instance)
            {
                GameManager.Instance.OnGameInitializeFinished.AddListener(InitCharacters);
                // If this script started after initialization already finished, initialize immediately
                if (GameManager.Instance.State == GameManager.GameState.GameInitialized)
                {
                    InitCharacters();
                }
            }
        }

        private void InitCharacters()
        {
            if (_initialized) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            _allCharacters = gm.Characters;
            _player = _allCharacters.Find(c => c.isPlayer);
            if (_player == null) return; // local player not spawned yet

            _playerIsReaper = _player is ReaperContainer;
            _reaper = _allCharacters.Find(c => c is ReaperContainer) as ReaperContainer;

            // Retrieve AudioSources and ensure both tracks play simultaneously
            _calmSrc = AudioManager.Instance?.GetAudioSource(calmMusicName);
            _tenseSrc = AudioManager.Instance?.GetAudioSource(tenseMusicName);
            if (_calmSrc == null || _tenseSrc == null)
            {
                Debug.LogError("ProximityMusicController: specified AudioSource not found");
                return;
            }

            _calmSrc.volume = 1f;
            _tenseSrc.volume = 0f;
            if (!_calmSrc.isPlaying) _calmSrc.Play();
            if (!_tenseSrc.isPlaying) _tenseSrc.Play();
            _isTense = false;
            _initialized = true;
        }

        private void Update()
        {
            if (!_initialized) return;
            if (_player == null) return; // safety check

            var needTense = ShouldPlayTense();
            if (needTense != _isTense)
            {
                StartCrossFade(needTense);
                _isTense = needTense;
            }
        }

        /// <summary>
        /// Determine whether tense music should be playing.
        /// </summary>
        private bool ShouldPlayTense()
        {
            if (_playerIsReaper)
            {
                // Reaper is close to any other player
                foreach (var c in _allCharacters)
                {
                    if (c == null || c == _player) continue;
                    if (Vector3.Distance(_player.transform.position, c.transform.position) < switchDistance)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                // Soul is close to Reaper
                if (_reaper == null) return false;
                return Vector3.Distance(_player.transform.position, _reaper.transform.position) < switchDistance;
            }
        }

        private void StartCrossFade(bool toTense)
        {
            if (_fadeCoroutine != null)
            {
                StopCoroutine(_fadeCoroutine);
            }

            _fadeCoroutine = StartCoroutine(CrossFade(toTense ? _calmSrc : _tenseSrc, toTense ? _tenseSrc : _calmSrc));
        }

        private IEnumerator CrossFade(AudioSource from, AudioSource to)
        {
            float t = 0f;
            to.volume = 0f;
            if (!to.isPlaying) to.Play();
            while (t < fadeDuration)
            {
                t += Time.deltaTime;
                var v = t / fadeDuration;
                from.volume = 1f - v;
                to.volume = v;
                yield return null;
            }

            from.volume = 0f;
            to.volume = 1f;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance)
            {
                GameManager.Instance.OnGameInitializeFinished.RemoveListener(InitCharacters);
            }

            if (_calmSrc) _calmSrc.volume = 1f; // reset volumes to avoid persistence between scenes
            if (_tenseSrc) _tenseSrc.volume = 1f;
        }
    }
}
