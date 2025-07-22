using System.Collections.Generic;
using AntMill.Liu.Scripts.networks;
using Audio;
using Character;
using MapGeneration;
using Message;
using MiniGames.Runner;
using Minimap;
using network;
using Player;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] private KcpNetwork kcpNetwork;
    
    [Header("Characters")]
    [SerializeField] private Transform characterParent;

    [Header("EnvObjects")] 
    [SerializeField] private Grid gameGrid;
    
    [Header("Character Prefabs")]
    [SerializeField] private SoulContainer dogContainerPrefab;
    [SerializeField] private SoulContainer psyContainerPrefab;
    [SerializeField] private SoulContainer detectiveContainerPrefab;
    [SerializeField] private PlayerContainer playerContainerPrefab;
    [SerializeField] private ReaperContainer reaperContainerPrefab;

    [Header("Map")] 
    [SerializeField] private MapParser mapParser;
    [SerializeField] private MiniMapController miniMapController;
    public MapInfoContainer mapInfoContainer;
    private List<CharacterContainer> _characters = new();

    [Header("Minigames")] 
    [SerializeField] private RunnerSetup runnerPrefab;
    [SerializeField] private Vector3 runnerInstancePosition = new (1000, 0, 0);
    
    
    [Header("Dog Path Manager")]
    public DogPathManager dogPathManager;
    public Camera mainCamera;
    
    [Header("Game Events")]
    public UnityEvent OnGameInitializeFinished;
    
    private Dictionary<int, CharacterContainer> _idToCharContainer = new Dictionary<int, CharacterContainer>();
    private int _playerId;
    private GameState _gameState = GameState.BeforeMap;
    private CharacterContainer _playerContainer;
    private List<GateContainer> _gates = new ();
    
    #region Properties

    public GameState State => _gameState;

    public List<CharacterContainer> Characters => _characters;
    
    public Vector3 GridSize => gameGrid.cellSize;

    public int PlayerId => _playerId;

    public MiniMapController MiniMapController
    {
        get => miniMapController;
        set => miniMapController = value;
    }

    #endregion

    #region Singleton

    private static GameManager _instance;

    public static GameManager Instance
    {
        get => _instance;
    }

    private void Awake()
    {
        if (_instance)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    #endregion
    

    private void Start()
    {
        AudioManager.Instance?.Play("BackgroundMusic");
        if (!GameStartData.Instance)
        {
            // KcpRecvMessageParser.Instance.onRoomMessageReceived.AddListener(OnRoomMessageReceived);
        }
        else
        {
            PopulateGame();
        }
        // mainCamera.enabled = false;
    }
    
    private void Update()
    {
        if (_gameState == GameState.GameGenerated)
        {
            OnGameInitializeFinished?.Invoke();
            _gameState = GameState.GameInitialized;
        }
    }
    
    private void OnDestroy()
    {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
    
    private void OnSceneUnloaded(Scene scene)
    {
        if (scene.name == Consts.GameScene)
        {
            _instance = null;
            DestroyImmediate(gameObject);
        }
    }


    private void PopulateGame()
    {
        var gameStartData = GameStartData.Instance;
        _playerId = gameStartData.playerId;
        var i = 0;
        
        mapInfoContainer = gameStartData.mapInfoContainer;
        mapParser.ParseMap(gameStartData.mapInfoContainer.mapString);
        var souldId = 0;
        foreach (var character in gameStartData.characters)
        {
            CharacterContainer selectedContainer = null;
            Vector3 position = Vector3.zero;
            switch (character.type)
            {
                case CharacterType.SoulDog:
                    selectedContainer = dogContainerPrefab;
                    position = mapInfoContainer.soulSpawnPositions[souldId];
                    souldId++;
                    break;
                case CharacterType.SoulPsychologist:
                    selectedContainer = psyContainerPrefab;
                    position = mapInfoContainer.soulSpawnPositions[souldId];
                    souldId++;
                    break;
                case CharacterType.SoulDetective:
                    selectedContainer = detectiveContainerPrefab;
                    position = mapInfoContainer.soulSpawnPositions[souldId];
                    souldId++;
                    break;
                case CharacterType.Reaper:
                    selectedContainer = reaperContainerPrefab;
                    position = mapInfoContainer.reaperPosition;
                    break;
                default:
                    Debug.LogWarning($"Unknown character type: {character.type}");
                    break;
            }
            //TODO: character message should have a position and rotation
            i++;
            var charContainer = Instantiate(selectedContainer, position, Quaternion.identity, characterParent);
            charContainer.id = character.id;
            if (_playerId == character.id)
            {
                // instantiate player container, add as child of character parent
                var playerContainer = Instantiate(playerContainerPrefab, Consts.PlayerPrefabPosition, Quaternion.identity, charContainer.transform);
                playerContainer.transform.localPosition = Vector3.zero;
                _playerContainer = charContainer;
                charContainer.isPlayer = true;
            }
            else
            {
                _idToCharContainer[character.id] = charContainer;
            }
            _characters.Add(charContainer);
        }

        miniMapController.InitializeMinimapDarkMask(_playerContainer, gameStartData.mapInfoContainer.gatePositions);
        _gameState = GameState.GameGenerated;
        KcpNetwork.Instance.SendStartReceiveMessage(_playerId);
        KcpRecvMessageParser.Instance?.onGateResultReceived.AddListener(OnGateResultReceived);
        KcpRecvMessageParser.Instance?.onResultMessageReceived.AddListener(GameResultMessageReceived);
    }

    public void SetGates(List<GateContainer> gates)
    {
        _gates = gates;
    }
    
    public void SetGateColor(int gateId)
    {
        if (_gates.Count <= gateId) return;
        var gate = _gates[gateId];
        if (gate == null) return;
        gate.ChangeColor(mapInfoContainer.heavenGateDirections.Contains(mapInfoContainer.gateDirections[gateId]));
    }

    private void OnGateResultReceived(EnterGateResultMessage message)
    {
        
    }
    
    private void GameResultMessageReceived(GameResultMessage message)
    {
        var result = message.GameResult;
        GameEndUI.Instance?.TurnOnGameEndPanel(result == GameResult.SoulWin, _playerContainer is SoulContainer, result == GameResult.Tie);
    }

    public void SpawnGateMinigame(SoulType soulType)
    {
        var runnerMinigame = Instantiate(runnerPrefab, runnerInstancePosition, Quaternion.identity);
        runnerMinigame.runnerCamera.enabled = true;
        runnerMinigame.InitializeSoul((int) soulType);
        mainCamera.enabled = false;
    }

    public void MinigameFinished(bool isHeavenGate)
    {
        mainCamera.enabled = true;
        ((SoulContainer)_playerContainer).RunnerMinigameFinished(isHeavenGate);
    }
    

    public enum GameState
    {
        BeforeMap,
        GameGenerated,
        GameInitialized
    }
}