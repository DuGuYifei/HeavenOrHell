using System;
using UnityEngine;
using UnityEngine.UI;

namespace Minimap
{
    public class MiniMapController : MonoBehaviour
    {
        [SerializeField] private RawImage minimapPlayerPositionRawImage;
        [SerializeField] private RawImage minimapDarkMaskRawImage;
        [SerializeField] private int width = 31;
        [SerializeField] private int height = 31;
        [SerializeField] private float mapScale = 3f;
        
        private Texture2D _minimapPlayerPositionTexture;
        private Texture2D _minimapDarkMaskTexture;
        private CharacterContainer _player;
        private GameObject _minimapParent;
        
        private readonly Color _playerColor = new (163 / 255f, 110 / 255f, 52 / 255f);

        public void InitializeMinimap(CharacterContainer player)
        {
            _player = player;
            // 将 _minimapPlayerPositionTexture 设为全透明 
            _minimapPlayerPositionTexture = new Texture2D(width, height);
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _minimapPlayerPositionTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
            
            // 将 player _charTransform 在 scale * width 和 scale * height 位置映射到 width 和 height 小地图范围内
            int playerX = Mathf.FloorToInt(player.transform.position.x / mapScale);
            int playerY = Mathf.FloorToInt(player.transform.position.y / mapScale);
            // 宝藏暗红
            _minimapPlayerPositionTexture.SetPixel(playerX, playerY, _playerColor);
            _minimapPlayerPositionTexture.Apply();
            minimapPlayerPositionRawImage.texture = _minimapPlayerPositionTexture;
            
            if (player is ReaperContainer)
            {
                // 将 _minimapDarkMaskTexture 设为全透明 
                _minimapDarkMaskTexture = new Texture2D(width, height);
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        _minimapDarkMaskTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
            }
            else
            {
                // 将 _minimapDarkMaskTexture 设为全黑
                _minimapDarkMaskTexture = new Texture2D(width, height);
                for (int x = 0; x < width; x++)
                    for (int y = 0; y < height; y++)
                        _minimapDarkMaskTexture.SetPixel(x, y, new Color(0, 0, 0, 1));
                
                // 将 player 周围 9隔和自己位置设为全透明
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        int posX = playerX + x;
                        int posY = playerY + y;
                        if (posX >= 0 && posX < width && posY >= 0 && posY < height)
                        {
                            _minimapDarkMaskTexture.SetPixel(posX, posY, new Color(0, 0, 0, 0));
                        }
                    }
                }
            }
            _minimapDarkMaskTexture.Apply();
            minimapDarkMaskRawImage.texture = _minimapDarkMaskTexture;
            _minimapParent = transform.parent.gameObject;
        }
        
        private void Update()
        {
            if (!_player) return;
            // 更新 player 在小地图上的位置
            int playerX = Mathf.FloorToInt(_player.transform.position.x / mapScale);
            int playerY = Mathf.FloorToInt(_player.transform.position.y / mapScale);
            
            // 清除之前的玩家位置
            for (int x = 0; x < width; x++)
                for (int y = 0; y < height; y++)
                    _minimapPlayerPositionTexture.SetPixel(x, y, new Color(0, 0, 0, 0));
            
            // 设置新的玩家位置
            _minimapPlayerPositionTexture.SetPixel(playerX, playerY, new Color(150f/255f, 17f/255f, 30f/255f, 1));
            _minimapPlayerPositionTexture.Apply();
            
            // 设置玩家周围 9格和自己位置为透明
            for (int x = -1; x <= 1; x++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    int posX = playerX + x;
                    int posY = playerY + y;
                    if (posX >= 0 && posX < width && posY >= 0 && posY < height)
                    {
                        _minimapDarkMaskTexture.SetPixel(posX, posY, new Color(0, 0, 0, 0));
                    }
                }
            }
            _minimapDarkMaskTexture.Apply();
        }

        public void SetVisibility(bool visible)
        {
            _minimapParent.SetActive(visible);
        }
    }
}