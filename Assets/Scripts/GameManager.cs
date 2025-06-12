using System.Collections.Generic;
using AntMill.Liu.Scripts.networks;
using Character;
using DefaultNamespace;
using Message;
using network;
using Player;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

public class GameManager : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] private KcpNetwork kcpNetwork;
    
    [Header("Characters")]
    [SerializeField] private Transform characterParent;

    [Header("EnvObjects")] [SerializeField]
    private Grid gameGrid;
    
    [Header("Character Prefabs")]
    [SerializeField] private SoulContainer dogContainerPrefab;
    [SerializeField] private SoulContainer psyContainerPrefab;
    [SerializeField] private SoulContainer detectiveContainerPrefab;
    [SerializeField] private PlayerContainer playerContainerPrefab;
    [SerializeField] private ReaperContainer reaperContainerPrefab;

    public List<Vector2> spawnPositions = new();
    private List<CharacterContainer> _characters = new();
    
    
    
    [Header("Dog Path Manager")]
    public DogPathManager dogPathManager;
    public Camera mainCamera;
    
    [Header("Game Events")]
    public UnityEvent OnGameInitializeFinished;
    
    private Dictionary<int, CharacterContainer> _idToCharContainer = new Dictionary<int, CharacterContainer>();
    private int _playerId;
    private GameState _gameState = GameState.BeforeMap;
    private CharacterContainer _playerContainer;
    
    #region Properties

    public GameState State => _gameState;

    public List<CharacterContainer> Characters => _characters;
    
    public Vector3 GridSize => gameGrid.cellSize;

    #endregion

    #region Singleton

    private static GameManager _instance;

    public static GameManager Instance
    {
        get => _instance;
    }

    private void Awake()
    {
        if (_instance) return;
        _instance = this;
    }

    #endregion
    

    private void Start()
    {
        KcpRecvMessageParser.Instance.onRoomMessageReceived.AddListener(OnRoomMessageReceived);
        mainCamera.enabled = false;
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
                playerContainer.transform.localPosition = Vector3.zero;
                _playerContainer = charContainer;
            }
            else
            {
                _idToCharContainer[character.PlayerId] = charContainer;
            }
            _characters.Add(charContainer);
        }
        
        _gameState = GameState.GameGenerated;

    }

    private void Update()
    {
        if (_gameState == GameState.GameGenerated)
        {
            OnGameInitializeFinished?.Invoke();
            _gameState = GameState.GameInitialized;
        }
    }

    public void SetSpawnPositions(List<Vector2> spawnPositions, float mapScale)
    {
        this.spawnPositions = spawnPositions;
        mainCamera.enabled = true;
        print(spawnPositions[0]);
        _playerContainer.transform.position = new Vector3(spawnPositions[_playerId].x * mapScale, spawnPositions[_playerId].y * mapScale, 0)
            + new Vector3(1.5f,1.5f,0);
    }

    public enum GameState
    {
        BeforeMap,
        GameGenerated,
        GameInitialized
    }
}