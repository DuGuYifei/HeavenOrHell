using System;
using AntMill.Liu.Scripts.networks;
using Message;
using UnityEngine;
using UnityEngine.Events;

namespace network
{
    public class KcpRecvMessageParser : MonoBehaviour
    {
        [SerializeField] private KcpNetwork kcpNetwork;

        public RoomMessageReceivedEvent onRoomMessageReceived;
        public MapMessageEvent onMapReceived;
        public SoulBasicMessageEvent onSoulBasicReceived;
        public PropGetMessageEvent onPropGetReceived;
        public LobbyMessageEvent onLobbyMessageReceived;
        public GateMessageEvent onGateMessageReceived;
        public GateResultMessageEvent onGateResultReceived;
        public IntegerMessageEvent onReaperResultReceived;
        public IntegerMessageEvent onAltarSuccessReceived;

        private readonly bool _debugBasicMessage = false;

        private void HandleMessageWrapper(MessageWrapper wrapper)
        {
            switch (wrapper.PayloadCase)
            {
                case MessageWrapper.PayloadOneofCase.RoomMessage:
                    var roomMsg = wrapper.RoomMessage;
                    Debug.Log(
                        $"[Server→Client] RoomMessage: room_id={roomMsg.RoomId}, player_id={roomMsg.PlayerId}, is_join={roomMsg.IsJoin}");
                    kcpNetwork.JoinRoom(roomMsg.RoomId, roomMsg.PlayerId, roomMsg.IsJoin, roomMsg.Characters);
                    onRoomMessageReceived?.Invoke(roomMsg);
                    break;

                case MessageWrapper.PayloadOneofCase.StringMessage:
                    var stringMsg = wrapper.StringMessage;
                    Debug.Log($"[Server→Client] StringMessage type={stringMsg.MessageType}");

                    if (stringMsg.MessageType == (int)StringMessageType.MazeMap)
                    {
                        Debug.Log($"[Server→Client] MazeMap: {stringMsg.MessageContent}");
                        onMapReceived?.Invoke(stringMsg);
                    }

                    break;

                case MessageWrapper.PayloadOneofCase.PlayerBasicMessage:
                    var soulMsg = wrapper.PlayerBasicMessage;
                    if (_debugBasicMessage)
                        Debug.Log(
                            $"[Server→Client] SoulBasicMessage: {soulMsg}");
                    onSoulBasicReceived?.Invoke(soulMsg);
                    break;

                case MessageWrapper.PayloadOneofCase.PropTryGetMessage:
                    var propTryMsg = wrapper.PropTryGetMessage;
                    Debug.Log(
                        $"[Server→Client] PropTryGetMessage: player_id={propTryMsg.PlayerId}, prop_id={propTryMsg.PropId}, prop_type={propTryMsg.PropType}");
                    break;

                case MessageWrapper.PayloadOneofCase.PropGetMessage:
                    var propGetMsg = wrapper.PropGetMessage;
                    Debug.Log(
                        $"[Server→Client] PropGetMessage: player_id={propGetMsg.PlayerId}, prop_id={propGetMsg.PropId}, is_get={propGetMsg.IsGet}");
                    onPropGetReceived?.Invoke(propGetMsg);
                    break;
                case MessageWrapper.PayloadOneofCase.LobbyMessage:
                    var lobbyResultMsg = wrapper.LobbyMessage;
                    Debug.Log(
                        $"[Server→Client] LobbyMessage: player_id={lobbyResultMsg.PlayerId}, is_ready={lobbyResultMsg.IsReady}, character_type={lobbyResultMsg.CharacterType}");
                    onLobbyMessageReceived?.Invoke(lobbyResultMsg);
                    break;
                case MessageWrapper.PayloadOneofCase.GateMessage:
                    var gateMsg = wrapper.GateMessage;
                    Debug.Log("[Server→Client] GateMessage");
                    onGateMessageReceived?.Invoke(gateMsg);
                    break;
                case MessageWrapper.PayloadOneofCase.EnterGateResultMessage:
                    var enterGateResultMsg = wrapper.EnterGateResultMessage;
                    Debug.Log(
                        $"[Server→Client] EnterGateResultMessage: player_id={enterGateResultMsg.PlayerId}, is_success={enterGateResultMsg.Gate.GateType}");
                    break;
                case MessageWrapper.PayloadOneofCase.IntegerMessage:
                    var integerMsg = wrapper.IntegerMessage;
                    if (integerMsg.MessageType == IntegerMessageType.ReaperAttackResult)
                    {
                        Debug.Log($"[Server→Client] ReaperAttackResult: {integerMsg.Value}");
                        onReaperResultReceived?.Invoke(integerMsg.Value);
                    } else if (integerMsg.MessageType == IntegerMessageType.AltarMiniGameSuccess)
                    {
                        Debug.Log($"[Server→Client] AltarMiniGameSuccess: {integerMsg.Value}");
                        onAltarSuccessReceived?.Invoke(integerMsg.Value);
                    }
                    break;
                default:
                    Debug.Log($"[Server→Client] Unknown message type: {wrapper.PayloadCase}");
                    break;
            }
        }

        #region Singleton

        public static KcpRecvMessageParser Instance { get; private set; }
        
        private void Awake()
        {
            if (Instance) return;
            Instance = this;
            if (kcpNetwork == null) kcpNetwork = GetComponent<KcpNetwork>();

            kcpNetwork.onRecvMessage.AddListener(HandleMessageWrapper);
        }

        #endregion
    }


    // Message Events have to be declared separately
    [Serializable]
    public class MapMessageEvent : UnityEvent<StringMessage>
    {
    }

    [Serializable]
    public class GateMessageEvent : UnityEvent<GateMessage>
    {
    }

    [Serializable]
    public class RoomMessageReceivedEvent : UnityEvent<RoomMessage>
    {
    }

    [Serializable]
    public class SoulBasicMessageEvent : UnityEvent<PlayerBasicMessage>
    {
    }

    [Serializable]
    public class PropGetMessageEvent : UnityEvent<PropGetMessage>
    {
    }

    [Serializable]
    public class LobbyMessageEvent : UnityEvent<LobbyMessage>
    {
    }

    [Serializable]
    public class GateResultMessageEvent : UnityEvent<EnterGateResultMessage>
    {
    }

    [Serializable]
    public class IntegerMessageEvent : UnityEvent<int>
    {
    }
}