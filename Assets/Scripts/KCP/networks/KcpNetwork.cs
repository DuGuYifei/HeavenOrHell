using UnityEngine;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Message;
using KcpProject;
using System;
using Google.Protobuf.Collections;
using UnityEngine.Events;

namespace AntMill.Liu.Scripts.networks
{
    public class KcpNetwork : MonoBehaviour
    {
        [Header("Server Settings")]
        [SerializeField]
        // private string serverIp = "172.28.183.56";
        public string serverIp = "172.28.183.56";

        // [SerializeField] private int serverPort = 8888;
        [SerializeField] public int serverPort = 8888;
        public KcpRecvMessageEvent onRecvMessage;

        private bool _startConnect = false;
        public int roomId = 0; // 0 to create new room, otherwise join existing
        // public int playerId = 0;
        private bool _connected = false;
        private bool _roomJoined = false;
        private float _lastHelloTime = -10;
        private const float HelloIntervalTime = 10f;
        private const float KcpSendIntervalTime = 0.02f;
        readonly object kcpLock = new object();
        
        private bool _debugBasicMessage = false; // Debug flag for basic message sending
        private UdpClient _udpClient;
        private IPEndPoint _serverEndPoint;
        private KCP _kcp;
        private bool _udpStarted = false;


        #region DontDestroyOnLoad
        
        private static KcpNetwork _instance;

        public static KcpNetwork Instance
        {
            get => _instance;
        }

        private void Awake()
        {
            if (_instance)
                Destroy(gameObject);
            else 
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        #endregion
        
        public void StartClient()
        {
            // 初始化 UDP 与 KCP 会话
            _udpClient = new UdpClient(0);
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            _udpClient.Connect(_serverEndPoint);
            _udpStarted = true;
            enabled = true;
            // Start receiving UDP packets
            StartReceiving();
        }


        private void Update()
        {
            // Send Hello message if not connected
            if (!_connected)
            {
                // Debug.Log($"Waiting for connection to server {serverIp}:{serverPort}");
                if (_startConnect && Time.time - _lastHelloTime > HelloIntervalTime)
                {
                    Debug.Log($"Sending HelloMessage to server {serverIp}:{serverPort}");
                    SendHelloMessage();
                    _lastHelloTime = Time.time;
                }
            }

            // Update KCP and receive messages
            else
            {
                if (_kcp != null && Time.time - _lastHelloTime > KcpSendIntervalTime)
                {
                    lock (kcpLock)
                    {
                        _kcp.Update();
                        _lastHelloTime = Time.time;
                        ReceiveKcpMessages();
                    }
                }
            }
        }

        private void OnDestroy()
        {
            try
            {
                if (_connected && _kcp != null)
                {
                    // Send any pending data before closing
                    _kcp.Flush(false);
                }

                _udpStarted = false;
                _udpClient?.Close();
                // _udpClient?.Dispose();
                _udpClient = null;
                _kcp = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during cleanup: {e.Message}");
            }
        }
        

        public void StartUdpConnect(int targetRoomId = 0)
        {
            roomId = targetRoomId;
            _startConnect = true;
        }

        public void DisconnectEverything()
        {
            StopUdpConnect();
            _connected = false;
            _roomJoined = false;
            _lastHelloTime = -10;
            enabled = false;
            roomId = 0;
        }

        public void StopUdpConnect()
        {
            _startConnect = false;
            _udpStarted = false;
            _udpClient?.Close();
        }

        private void StartReceiving()
        {
            if (!_udpStarted) return;
            _udpClient.BeginReceive(OnUdpReceive, null);
        }

        private void OnUdpReceive(IAsyncResult result)
        {
            if (!_udpStarted) return;
            try
            {
                IPEndPoint remoteEp = null;
                byte[] data = _udpClient.EndReceive(result, ref remoteEp);

                ProcessReceivedData(data);

                // Continue receiving
                
                StartReceiving();
            }
            catch (Exception e)
            {
                Debug.LogError("UDP receive error");
                Debug.LogException(e);
                StartReceiving();
            }
        }

        private void ProcessReceivedData(byte[] data)
        {
            if (data.Length >= 4)
            {
                uint conv = BitConverter.ToUInt32(data, 0);

                if (!_connected && conv != 0)
                {
                    // Initial connection setup with conv from server
                    Debug.Log($"Received conv={conv} from server, establishing KCP session");

                    // Setup KCP
                    _kcp = new KCP(conv, OnKcpOutput);
                    _kcp.NoDelay(1, 20, 2, 1);  // Fast mode
                    _kcp.WndSize(32 * 4, 32 * 4);     // Set window size
                    // _kcp.logmask = KCP.IKCP_LOG_OUTPUT | KCP.IKCP_LOG_INPUT; // Enable logging
                    // _kcp.SetLogger(Debug.Log);
                    _connected = true;

                    // Process this initial packet
                    lock (kcpLock)
                    {
                        _kcp.Input(data, 0, data.Length, true, true);
                    }
                }
                else if (_connected)
                {
                    // Normal packet for existing KCP session
                    lock (kcpLock)
                    {
                        _kcp.Input(data, 0, data.Length, true, true);
                    }
                }
            }
        }

        private void OnKcpOutput(byte[] buffer, int size)
        {
            try
            {
                byte[] data = new byte[size];
                Buffer.BlockCopy(buffer, 0, data, 0, size);
                _udpClient.Send(data, size);
            }
            catch (Exception e)
            {
                Debug.LogError($"UDP send error: {e.Message}");
            }
        }

        private void ReceiveKcpMessages()
        {
            int msgSize = 0;
            while ((msgSize = _kcp.PeekSize()) > 0)
            {
                byte[] buffer = new byte[msgSize];
                if (_kcp.Recv(buffer) > 0)
                {
                    try
                    {
                        // Try to parse received data as a MessageWrapper
                        MessageWrapper wrapper = MessageWrapper.Parser.ParseFrom(buffer);
                        onRecvMessage?.Invoke(wrapper);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing message: {e.Message}");
                    }
                }
            }
        }

        public void JoinRoom(int messageRoomId, int messagePlayerId, bool isJoin, RepeatedField<Message.Character> characters)
        {
            if (isJoin)
            {
                _roomJoined = true;
                // playerId = messagePlayerId;
                roomId = messageRoomId;

                Debug.Log($"Successfully joined room {roomId} as player {messagePlayerId}");

                // Print other players in the room
                foreach (var character in characters)
                {
                    Debug.Log($"Player {character.PlayerId} is a {character.CharacterType}");
                }
            }
            else
            {
                Debug.LogWarning($"Failed to join room {roomId}");
            }
        }


        private void SendHelloMessage()
        {
            try
            {
                // Create the HelloMessage
                HelloMessage hello = new HelloMessage
                {
                    RoomId = roomId
                };

                byte[] msgData = hello.ToByteArray();

                // Prepare the packet with conv=0 header (4 bytes) + serialized message
                byte[] packet = new byte[msgData.Length + 4];
                BitConverter.GetBytes((uint)0).CopyTo(packet, 0);
                msgData.CopyTo(packet, 4);

                // Send the raw packet
                _udpClient.Send(packet, packet.Length);
                Debug.Log($"[Client→Server] Sent HelloMessage with room_id={roomId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending hello message: {e.Message}");
            }
        }

        // Send SoulBasicMessage (position, HP)
        public void SendPlayerBasicMessage(float posX, float posY, int playerId, PlayerAnimationType animationType)
        {
            if (!_connected || !_roomJoined) return;

            try
            {
                PlayerBasicMessage soulMsg = new PlayerBasicMessage()
                {
                    PlayerId = playerId,
                    PositionX = posX,
                    PositionY = posY,
                    AnimationType = animationType
                };

                MessageWrapper wrapper = new MessageWrapper
                {
                    PlayerBasicMessage = soulMsg
                };

                SendProtobufMessage(wrapper);
                if (_debugBasicMessage) Debug.Log($"[Client→Server] Sent SoulBasicMessage: pos=({posX},{posY}), player_id={playerId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending soul basic message: {e.Message}");
            }
        }

        // Send ReaperAttackMessage
        public void SendReaperAttackMessage(int target_player_id)
        {
            if (!_connected || !_roomJoined) return;

            try
            {
                IntegerMessage msg = new IntegerMessage
                {
                    Value = target_player_id,
                    MessageType = IntegerMessageType.ReaperAttackResult
                };

                MessageWrapper wrapper = new MessageWrapper
                {
                    IntegerMessage = msg,
                };

                SendProtobufMessage(wrapper);
                Debug.Log($"[Client→Server] Sent AttackMessage: TargetMessage={target_player_id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending reaper altar minigame success message: {e.Message}");
            }
            // if (!_connected || !_roomJoined) return;

            // try
            // {
            //     ReaperAttackMessage attackMsg = new ReaperAttackMessage
            //     {
            //         SoulPlayerId = targetSoulPlayerId,
            //         SkillId = skillId
            //     };

            //     MessageWrapper wrapper = new MessageWrapper
            //     {
            //         ReaperAttackMessage = attackMsg
            //     };

            //     SendProtobufMessage(wrapper);
            //     Debug.Log($"[Client→Server] Sent ReaperAttackMessage: target={targetSoulPlayerId}, skill={skillId}");
            // }
            // catch (Exception e)
            // {
            //     Debug.LogError($"Error sending reaper attack message: {e.Message}");
            // }
        }
        
        public void SendEnterGateMessage(int playerId, GateDirection gateDirection)
        {
            if (!_connected || !_roomJoined) return;

            try
            {
                EnterGateMessage gateMsg = new EnterGateMessage
                {
                    GateDirection = gateDirection
                };

                MessageWrapper wrapper = new MessageWrapper
                {
                    EnterGateMessage = gateMsg
                };

                SendProtobufMessage(wrapper);
                Debug.Log($"[Client→Server] Sent GateMessage: PlayerId={playerId}, GateDirection={gateDirection}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending gate message: {e.Message}");
            }
        }

        public void SendStartReceiveMessage(int playerId)
        {
            if (!_connected || !_roomJoined) return;
            try
            {
                StartReceiveMsgMessage msg = new StartReceiveMsgMessage
                {
                    PlayerId = playerId
                };
                MessageWrapper wrapper = new MessageWrapper
                {
                    StartReceiveMsgMessage = msg
                };
                SendProtobufMessage(wrapper);
                Debug.Log($"[Client→Server] Sent StartReceiveMsgMessage: PlayerId={playerId}");
            } catch (Exception e)
            {
                Debug.LogError($"Error sending reaper attack message: {e.Message}");
            }
        }

        public void SendLobbyMessage(int player_id, bool is_ready, CharacterType character_type)
        {
            if (!_connected || !_roomJoined) return;

            try
            {
                LobbyMessage lobbyMsg = new LobbyMessage
                {
                    PlayerId = player_id,
                    IsReady = is_ready,
                    CharacterType = character_type,
                };

                MessageWrapper wrapper = new MessageWrapper
                {
                    LobbyMessage = lobbyMsg
                };

                SendProtobufMessage(wrapper);
                Debug.Log($"[Client→Server] Sent LobbyMessage: PlayerId={player_id}, IsReady={is_ready}, CharacterType={character_type}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending reaper lobby message: {e.Message}");
            }
        }

        public void SendAltarSuccessMessage(int weak_soul_id)
        {
            if (!_connected || !_roomJoined) return;

            try
            {
                IntegerMessage msg = new IntegerMessage
                {
                    Value = weak_soul_id,
                    MessageType = IntegerMessageType.AltarMiniGameSuccess,
                };

                MessageWrapper wrapper = new MessageWrapper
                {
                    IntegerMessage = msg,
                };

                SendProtobufMessage(wrapper);
                Debug.Log($"[Client→Server] Sent AltarSuccessMessage: WeakSoulId={weak_soul_id}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending reaper altar minigame success message: {e.Message}");
            }
        }

        private void SendProtobufMessage(IMessage message)
        {
            if (_kcp == null) return;

            byte[] data = message.ToByteArray();
            int ret = _kcp.Send(data);
            if (ret < 0)
            {
                Debug.LogError($"Error sending message: {ret}");
            }
        }
    }

    [Serializable]
    public class KcpRecvMessageEvent : UnityEvent<MessageWrapper>
    {
        
    }
}