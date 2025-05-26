using UnityEngine;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using Message;
using KcpProject;
using System;

namespace AntMill.Liu.Scripts.networks
{
    public class KcpNetwork : MonoBehaviour
    {
        [Header("Server Settings")] [SerializeField]
        private string serverIp = "172.28.63.176";

        [SerializeField] private int serverPort = 8888;
        private bool _startConnect = false;
        public int roomId = 0; // 0 to create new room, otherwise join existing
        public int playerId = 0;
        private bool _connected = false;
        private bool _roomJoined = false;
        private float _lastHelloTime = -10;
        private const float HelloIntervalTime = 10f;
        private const float KcpSendIntervalTime = 0.02f;

        private UdpClient _udpClient;
        private IPEndPoint _serverEndPoint;
        private KCP _kcp;
        private uint _conv = 0;

        void Start()
        {
            // 初始化 UDP 与 KCP 会话
            _udpClient = new UdpClient(0);
            _serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIp), serverPort);
            _udpClient.Connect(_serverEndPoint);
            
            // Start receiving UDP packets
            StartReceiving();
        }
        
        
        private void Update()
        {
            // Send Hello message if not connected
            if (!_connected)
            {
                Debug.Log($"Waiting for connection to server {serverIp}:{serverPort}");
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
                    _kcp.Update();
                    _lastHelloTime = Time.time;
                    ReceiveKcpMessages();
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
        
                _udpClient?.Close();
                _udpClient?.Dispose();
                _udpClient = null;
                _kcp = null;
            }
            catch (Exception e)
            {
                Debug.LogError($"Error during cleanup: {e.Message}");
            }
        }

        public void StartConnect(int targetRoomId = 0)
        {
            roomId = targetRoomId;
            _startConnect = true;
        }

        private void StartReceiving()
        {
            _udpClient.BeginReceive(OnUdpReceive, null);
        }

        private void OnUdpReceive(IAsyncResult result)
        {
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
                Debug.LogError($"UDP receive error: {e.Message}");
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
                    _conv = conv;
                    
                    // Setup KCP
                    _kcp = new KCP(conv, OnKcpOutput);
                    _kcp.NoDelay(1, 1, 2, 1);  // Fast mode
                    _kcp.WndSize(32 * 4, 32 * 4);     // Set window size
                    
                    _connected = true;
                    
                    // Process this initial packet
                    _kcp.Input(data, 0, data.Length, true, true);
                }
                else if (_connected)
                {
                    // Normal packet for existing KCP session
                    _kcp.Input(data, 0, data.Length, true, true);
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
                        HandleMessageWrapper(wrapper);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Error parsing message: {e.Message}");
                    }
                }
            }
        }
        
        private void HandleMessageWrapper(MessageWrapper wrapper)
        {
            switch (wrapper.PayloadCase)
            {
                case MessageWrapper.PayloadOneofCase.RoomMessage:
                    var roomMsg = wrapper.RoomMessage;
                    Debug.Log($"[Server→Client] RoomMessage: room_id={roomMsg.RoomId}, player_id={roomMsg.PlayerId}, is_join={roomMsg.IsJoin}");
                    
                    if (roomMsg.IsJoin)
                    {
                        _roomJoined = true;
                        playerId = roomMsg.PlayerId;
                        roomId = roomMsg.RoomId;
                        
                        Debug.Log($"Successfully joined room {roomId} as player {playerId}");
                        
                        // Print other players in the room
                        foreach (var character in roomMsg.Characters)
                        {
                            Debug.Log($"Player {character.PlayerId} is a {character.CharacterType}");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Failed to join room {roomId}");
                    }
                    break;
                    
                case MessageWrapper.PayloadOneofCase.StringMessage:
                    var stringMsg = wrapper.StringMessage;
                    Debug.Log($"[Server→Client] StringMessage type={stringMsg.MessageType}");
                    
                    if (stringMsg.MessageType == (int)StringMessageType.MazeMap)
                    {
                        Debug.Log($"[Server→Client] MazeMap: {stringMsg.MessageContent}");
                    }
                    break;
                    
                case MessageWrapper.PayloadOneofCase.SoulBasicMessage:
                    var soulMsg = wrapper.SoulBasicMessage;
                    Debug.Log($"[Server→Client] SoulBasicMessage: player_id={soulMsg.PlayerId}, pos=({soulMsg.PositionX},{soulMsg.PositionY}), hp={soulMsg.Hp}/{soulMsg.MaxHp}");
                    break;
                    
                case MessageWrapper.PayloadOneofCase.ReaperAttackMessage:
                    var attackMsg = wrapper.ReaperAttackMessage;
                    Debug.Log($"[Server→Client] ReaperAttackMessage: soul_player_id={attackMsg.SoulPlayerId}, skill_id={attackMsg.SkillId}");
                    break;
                    
                case MessageWrapper.PayloadOneofCase.PropTryGetMessage:
                    var propTryMsg = wrapper.PropTryGetMessage;
                    Debug.Log($"[Server→Client] PropTryGetMessage: player_id={propTryMsg.PlayerId}, prop_id={propTryMsg.PropId}, prop_type={propTryMsg.PropType}");
                    break;
                    
                case MessageWrapper.PayloadOneofCase.PropGetMessage:
                    var propGetMsg = wrapper.PropGetMessage;
                    Debug.Log($"[Server→Client] PropGetMessage: player_id={propGetMsg.PlayerId}, prop_id={propGetMsg.PropId}, is_get={propGetMsg.IsGet}");
                    break;
                    
                case MessageWrapper.PayloadOneofCase.ReaperAttackResultMessage:
                    var attackResultMsg = wrapper.ReaperAttackResultMessage;
                    Debug.Log($"[Server→Client] ReaperAttackResultMessage: soul_player_id={attackResultMsg.SoulPlayerId}, is_hit={attackResultMsg.IsHit}");
                    break;
                    
                default:
                    Debug.Log($"[Server→Client] Unknown message type: {wrapper.PayloadCase}");
                    break;
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
        public void SendSoulBasicMessage(float posX, float posY, float hp, float maxHp)
        {
            if (!_connected || !_roomJoined) return;
            
            try
            {
                SoulBasicMessage soulMsg = new SoulBasicMessage
                {
                    PlayerId = playerId,
                    PositionX = posX,
                    PositionY = posY,
                    Hp = hp,
                    MaxHp = maxHp
                };
                
                MessageWrapper wrapper = new MessageWrapper
                {
                    SoulBasicMessage = soulMsg
                };
                
                SendProtobufMessage(wrapper);
                Debug.Log($"[Client→Server] Sent SoulBasicMessage: pos=({posX},{posY}), hp={hp}/{maxHp}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending soul basic message: {e.Message}");
            }
        }
        
        // Send ReaperAttackMessage
        public void SendReaperAttackMessage(int targetSoulPlayerId, int skillId)
        {
            if (!_connected || !_roomJoined) return;
            
            try
            {
                ReaperAttackMessage attackMsg = new ReaperAttackMessage
                {
                    SoulPlayerId = targetSoulPlayerId,
                    SkillId = skillId
                };
                
                MessageWrapper wrapper = new MessageWrapper
                {
                    ReaperAttackMessage = attackMsg
                };
                
                SendProtobufMessage(wrapper);
                Debug.Log($"[Client→Server] Sent ReaperAttackMessage: target={targetSoulPlayerId}, skill={skillId}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending reaper attack message: {e.Message}");
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
}