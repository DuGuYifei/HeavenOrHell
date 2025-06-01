using System.Collections.Generic;
using AntMill.Liu.Scripts.networks;
using DefaultNamespace;
using Message;
using network;
using Player;
using Unity.Collections;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private SoulContainer dogContainerPrefab;
    [SerializeField] private SoulContainer psyContainerPrefab;
    [SerializeField] private SoulContainer detectiveContainerPrefab;
    [SerializeField] private PlayerContainer playerContainerPrefab;
    [SerializeField] private ReaperContainer reaperContainerPrefab;
    [SerializeField] private KcpNetwork kcpNetwork;
    [SerializeField] private Transform characterParent;

    private Dictionary<int, CharacterContainer> _idToCharContainer = new Dictionary<int, CharacterContainer>();
    private int _playerId;
    

    private void Start()
    {
        KcpRecvMessageParser.Instance.onRoomMessageReceived.AddListener(OnRoomMessageReceived);
        
    }

    private void OnRoomMessageReceived(RoomMessage roomMessage)
    {
        var i = 0;
        foreach (var character in roomMessage.Characters)
        {
            CharacterContainer selectedContainer = null;
            switch (character.CharacterType)
            {
                case CharacterType.SoulDog:
                    selectedContainer = dogContainerPrefab;
                    break;
                case CharacterType.SoulPsychologist:
                    selectedContainer = psyContainerPrefab;
                    break;
                case CharacterType.SoulDetective:
                    selectedContainer = detectiveContainerPrefab;
                    break;
                case CharacterType.Reaper:
                    selectedContainer = reaperContainerPrefab;
                    break;
                default:
                    Debug.LogWarning($"Unknown character type: {character.CharacterType}");
                    break;
            }
            //TODO: character message should have a position and rotation
            var position = new Vector3(TestValues.CharacterPositions[i].x, TestValues.CharacterPositions[i].y, 0);
            i++;
            var charContainer = Instantiate(selectedContainer, position, Quaternion.identity, characterParent);
            if (kcpNetwork.playerId == character.PlayerId)
            {
                // instantiate player container, add as child of character parent
                _playerId = character.PlayerId;
                var playerContainer = Instantiate(playerContainerPrefab, Consts.PlayerPrefabPosition, Quaternion.identity, charContainer.transform);
            }
            else
            {
                _idToCharContainer[character.PlayerId] = charContainer;
            }
        }
        
    }
}