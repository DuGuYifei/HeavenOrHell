using System;
using AntMill.Liu.Scripts.networks;
using Message;
using UnityEngine;

namespace network
{
    public class KcpRecvMessageParser : MonoBehaviour
    {
        [SerializeField]
        private KcpNetwork kcpNetwork;
        
        public RoomMessageReceivedEvent onRoomMessageReceived;
        public MapMessageEvent onMapReceived;
        public SoulBasicMessageEvent onSoulBasicReceived;
        public ReaperAttackResultMessageEvent onReaperAttackResultReceived;
        public PropGetMessageEvent onPropGetReceived;
        
        #region Singleton
        
        private static KcpRecvMessageParser _instance;

        public static KcpRecvMessageParser Instance
        {
            get => _instance;
            set => _instance = value;
        }

        #endregion

        private void Awake()
        {
            if (_instance) return;
            _instance = this;
            if (kcpNetwork == null)
            {
                kcpNetwork = GetComponent<KcpNetwork>();
            }
            
            kcpNetwork.onRecvMessage.AddListener(HandleMessageWrapper);
        }
        
        private void HandleMessageWrapper(MessageWrapper wrapper)
        {
            switch (wrapper.PayloadCase)
            {
                case MessageWrapper.PayloadOneofCase.RoomMessage:
                    var roomMsg = wrapper.RoomMessage;
                    Debug.Log($"[Server→Client] RoomMessage: room_id={roomMsg.RoomId}, player_id={roomMsg.PlayerId}, is_join={roomMsg.IsJoin}");
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
                    
                case MessageWrapper.PayloadOneofCase.SoulBasicMessage:
                    var soulMsg = wrapper.SoulBasicMessage;
                    Debug.Log($"[Server→Client] SoulBasicMessage: player_id={soulMsg.PlayerId}, pos=({soulMsg.PositionX},{soulMsg.PositionY}), hp={soulMsg.Hp}/{soulMsg.MaxHp}");
                    onSoulBasicReceived?.Invoke(soulMsg);
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
                    onPropGetReceived?.Invoke(propGetMsg);
                    break;
                    
                case MessageWrapper.PayloadOneofCase.ReaperAttackResultMessage:
                    var attackResultMsg = wrapper.ReaperAttackResultMessage;
                    Debug.Log($"[Server→Client] ReaperAttackResultMessage: soul_player_id={attackResultMsg.SoulPlayerId}, is_hit={attackResultMsg.IsHit}");
                    onReaperAttackResultReceived?.Invoke(attackResultMsg);
                    break;
                    
                default:
                    Debug.Log($"[Server→Client] Unknown message type: {wrapper.PayloadCase}");
                    break;
            }
        }
        
    }
    
    
    // Message Events have to be declared separately
    [Serializable]
    public class MapMessageEvent : UnityEngine.Events.UnityEvent<StringMessage>
    {
    }
    
    [Serializable]
    public class RoomMessageReceivedEvent : UnityEngine.Events.UnityEvent<RoomMessage>
    {
    }
    
    [Serializable]
    public class SoulBasicMessageEvent : UnityEngine.Events.UnityEvent<SoulBasicMessage>
    {
    }
    
    [Serializable]
    public class ReaperAttackResultMessageEvent : UnityEngine.Events.UnityEvent<ReaperAttackResultMessage>
    {
    }
    
    [Serializable]
    public class PropGetMessageEvent : UnityEngine.Events.UnityEvent<PropGetMessage>
    {
    }
    
}