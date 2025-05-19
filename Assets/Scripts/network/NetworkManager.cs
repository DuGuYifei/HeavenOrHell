using System;
using System.Threading;
using kcp2k;
using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif
namespace network
{
    public class NetworkManager : MonoBehaviour
    {
        [SerializeField] private string serverAddress = "127.0.0.1";
        [SerializeField] private ushort serverPort = 8888;
        public NetworkEvent onNetworkEvent;
        
        private KcpClient _client;
        

        #region Singleton

        private static NetworkManager _instance;

        public static NetworkManager Instance
        {
            get => _instance;
            set => _instance = value;
        }

        private void Awake()
        {
            if (_instance) return;
            _instance = this;
        }

        #endregion
        
        private static KcpConfig config = new KcpConfig(
            // force NoDelay and minimum interval.
            // this way UpdateSeveralTimes() doesn't need to wait very long and
            // tests run a lot faster.
            NoDelay: true,
            // not all platforms support DualMode.
            // run tests without it, so they work on all platforms.
            DualMode: false,
            Interval: 1, // 1ms so at interval code at least runs.
            Timeout: 2000,

            // large window sizes so large messages are flushed with very few
            // update calls. otherwise, tests take too long.
            SendWindowSize: Kcp.WND_SND * 1000,
            ReceiveWindowSize: Kcp.WND_RCV * 1000,

            // congestion window _heavily_ restricts send/recv window sizes
            // sending a max sized message would require thousands of updates.
            CongestionWindow: false,

            // maximum retransmit attempts until dead_link detected
            // default * 2 to check if the configuration works
            MaxRetransmits: Kcp.DEADLINK * 2
        );

        private void Start()
        {
            _client = new KcpClient(
                () =>
                {
                    Log.Info("[KCP] OnClientConnected");
                },
                (message, channel) =>
                {
                    Log.Info(
                        $"[KCP] OnClientDataReceived({BitConverter.ToString(message.Array, message.Offset, message.Count)} @ {channel})");
                    onNetworkEvent?.Invoke(BitConverter.ToString(message.Array, message.Offset, message.Count));
                },
                () => {},
                (error, reason) => Log.Warning($"[KCP] OnClientError({error}, {reason}"),
                config
            );
        }
        
        private void OnDestroy() 
        {
            _client?.Disconnect();
            _client = null;
        }
        
        public void Connect()
        {
            print("connecting to server");
            _client.Connect(serverAddress, serverPort);
            for (int i = 0; i < 5; ++i)
            {
                _client.Tick();
                // update 'interval' milliseconds.
                // the lower the interval, the faster the tests will run.
                Thread.Sleep((int)config.Interval);
            }
            print(_client.connected);
        }
    }
    
    
    #if UNITY_EDITOR
    [CustomEditor(typeof(NetworkManager)), CanEditMultipleObjects]
    public class NetworkManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var manager = (NetworkManager)target;

            if (GUILayout.Button("Connect"))
            {
                manager.Connect();
            }
        }
    }
    #endif

    [Serializable]
    public class NetworkEvent : UnityEvent<string>
    {
        
    }
}