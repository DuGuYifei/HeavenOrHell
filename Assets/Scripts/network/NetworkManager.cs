using AntMill.Liu.Scripts.networks;
using Player;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace network
{
    public class NetworkManager : MonoBehaviour
    {
        [SerializeField] private KcpNetwork kcp;
        
        private PlayerContainer _playerContainer;
        private Transform _playerTransform;
        private bool _foundPlayerTransform;

        private void Update()
        {
            if (!GameManager.Instance || GameManager.Instance.State == GameManager.GameState.BeforeMap) return;
            
            if (!_foundPlayerTransform)
            {
                _playerContainer = PlayerContainer.Instance;
                _playerTransform = _playerContainer.transform;
                _foundPlayerTransform = true;
            }
            var pos = _playerTransform.position;
            kcp.SendSoulBasicMessage(pos.x, pos.y, _playerContainer.hp, _playerContainer.maxHp);
        }

        public void ConnectToServer()
        {
            kcp.StartConnect();
        }
    }
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(NetworkManager))]
    public class NetworkManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            NetworkManager networkManager = (NetworkManager)target;
            if (GUILayout.Button("Connect")) networkManager.ConnectToServer();
        }
    }
    #endif
    
}