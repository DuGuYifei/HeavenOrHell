using System;
using System.Collections.Generic;
using System.Linq;
using AntMill.Liu.Scripts.networks;
using DefaultNamespace;
using MapGeneration;
using Message;
using network;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    internal enum MenuState
    {
        MainMenu,
        Connection,
        Lobby,
        Tutorial
    }

    [Serializable]
    public struct LobbyPlayerInfo
    {
        public bool IsReady;
        public int PlayerId;
        public CharacterType CharacterType;
    }

    public class UIManager : MonoBehaviour
    {
        public string ServerIP;
        public int ServerPort;

        public GameObject MainMenu;

        public GameObject Connection;

        public GameObject Lobby;

        public GameObject LobbyId;

        public GameObject LobbyEntryField;

        public GameObject ReadyFlag;

        public KcpNetwork kcpNetwork;

        public GameObject tutorialPage;

        public LobbyPlayerInfo[] OtherPlayers = new LobbyPlayerInfo[3];
        public GameObject[] PlayerIcons;
        public TMP_Dropdown PlayerRoleDropdown;

        public float StartTimer = 2.0f;
        public float ActualTimer;
        public List<GateDirection> gateDirections;
        public int heavenGateIndex = 1;
        private bool IsHost;

        private string mapString = "";

        private LobbyPlayerInfo PlayerLobbyState;

        private MenuState state = MenuState.MainMenu;

        private void Start()
        {
            ActualTimer = StartTimer;

            state = MenuState.MainMenu;
            MainMenu.SetActive(true);
            Connection.SetActive(false);
            Lobby.SetActive(false);
            ReadyFlag.SetActive(false);
            tutorialPage.SetActive(false);

            for (var i = 0; i < 3; i++)
            {
                OtherPlayers[i].IsReady = false;
                OtherPlayers[i].PlayerId = -1;
                OtherPlayers[i].CharacterType = CharacterType.SoulDog;
            }

            PlayerLobbyState.IsReady = false;
            PlayerLobbyState.PlayerId = -1;
            PlayerLobbyState.CharacterType = CharacterType.SoulDog;

            // SetUpKcp();
        }

        private void Update()
        {
            if (IsGameStartable())
                ActualTimer -= Time.deltaTime;
            else
                ActualTimer = StartTimer;
            if (ActualTimer <= 0.0f) StartTheGame();
            for (var i = 0; i < 3; i++)
                if (OtherPlayers[i].PlayerId == -1)
                {
                    PlayerIcons[i].SetActive(false);
                }
                else
                {
                    PlayerIcons[i].SetActive(true);
                    try
                    {
                        var ct = PlayerIcons[i].GetComponent<LobbyOtherPlayerController>();
                        ct.SetReady(OtherPlayers[i].IsReady);
                        ct.UpdateRole(OtherPlayers[i].CharacterType);
                    }
                    catch
                    {
                        Debug.Log("Could not properly change UI elements for other players");
                    }
                }
        }

        public void GoBack()
        {
            switch (state)
            {
                case MenuState.Connection:
                    Connection.SetActive(false);
                    MainMenu.SetActive(true);
                    state = MenuState.MainMenu;
                    break;
                case MenuState.Lobby: 
                    DisconnectFromLobby();
                    for (var i = 0; i < 3; i++)
                    {
                        OtherPlayers[i].IsReady = false;
                        OtherPlayers[i].PlayerId = -1;
                        OtherPlayers[i].CharacterType = CharacterType.SoulDog;
                    }
                    Lobby.SetActive(false);
                    MainMenu.SetActive(true);
                    state = MenuState.MainMenu;
                    break;
                case MenuState.Tutorial:
                    tutorialPage.SetActive(false);
                    MainMenu.SetActive(true);
                    break;
            }
        }

        public void DisconnectFromLobby()
        {
            Debug.Log("Disconnect from Lobby");
            kcpNetwork.DisconnectEverything();
            IsHost = false;
        }

        public void CreateLobby()
        {
            try
            {
                Debug.Log("Creating a lobby");

                SetUpKcp();

                kcpNetwork.StartUdpConnect();
                IsHost = true;
                PlayerLobbyState.PlayerId = 0;

                MainMenu.SetActive(false);
                Lobby.SetActive(true);
                state = MenuState.Lobby;
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not create a lobby: {e}");
            }
        }

        public void ConnectToLobby()
        {
            try
            {
                SetUpKcp();

                var roomId = int.Parse(LobbyEntryField.GetComponent<TMP_InputField>().text);

                Debug.Log($"Connecting to the lobby with a code '{roomId}'");

                kcpNetwork.StartUdpConnect(roomId);
                LobbyId.GetComponent<TMP_Text>().text = $"room: {roomId}";
                IsHost = false;

                Connection.SetActive(false);
                Lobby.SetActive(true);
                state = MenuState.Lobby;
            }
            catch (Exception e)
            {
                Debug.LogError($"Could not connect: {e}");
            }
        }

        public void GoToConnection()
        {
            Connection.SetActive(true);
            MainMenu.SetActive(false);
            state = MenuState.Connection;
        }

        public void GoToTutorial()
        {
            MainMenu.SetActive(false);
            tutorialPage.SetActive(true);
            state = MenuState.Tutorial;
        }

        public void OnReceivingRoomMessage(RoomMessage roomMsg)
        {
            Debug.Log($"Room Message is received: {roomMsg}");
            Debug.Log($"user 1 playerid {roomMsg.Characters[0].PlayerId}");
            LobbyId.GetComponent<TMP_Text>().text = $"room: {roomMsg.RoomId}";
            if (roomMsg.IsJoin && PlayerLobbyState.PlayerId == -1) PlayerLobbyState.PlayerId = roomMsg.PlayerId;
            Debug.Log("Setting up players " + PlayerLobbyState.PlayerId);
            // TODO

            for (var i = 0; i < roomMsg.Characters.Count; i++)
                if (i == PlayerLobbyState.PlayerId)
                {
                    switch (roomMsg.Characters[i].CharacterType)
                    {
                        case CharacterType.SoulDog:
                            PlayerRoleDropdown.SetValueWithoutNotify(0);
                            break;
                        case CharacterType.SoulPsychologist:
                            PlayerRoleDropdown.SetValueWithoutNotify(1);
                            break;
                        case CharacterType.SoulDetective:
                            PlayerRoleDropdown.SetValueWithoutNotify(2);
                            break;
                        case CharacterType.Reaper:
                            PlayerRoleDropdown.SetValueWithoutNotify(3);
                            break;
                        default:
                            Debug.LogError("Unknown character type");
                            break;
                    }

                    PlayerLobbyState.CharacterType = roomMsg.Characters[i].CharacterType;
                }
                else
                {
                    var existingPlayer = false;
                    for (var j = 0; j < 3; j++)
                        if (OtherPlayers[j].PlayerId == i)
                        {
                            OtherPlayers[j].CharacterType = roomMsg.Characters[i].CharacterType;
                            existingPlayer = true;
                            break;
                        }

                    if (!existingPlayer)
                        for (var j = 0; j < 3; j++)
                            if (OtherPlayers[j].PlayerId == -1)
                            {
                                OtherPlayers[j].PlayerId = roomMsg.Characters[i].PlayerId;
                                OtherPlayers[j].CharacterType = roomMsg.Characters[i].CharacterType;
                                break;
                            }
                }

            Debug.Log($" {OtherPlayers[0]} {OtherPlayers[1]} {OtherPlayers[2]}");
        }

        public void OnReceivingLobbyMessage(LobbyMessage lobbyMsg)
        {
            Debug.Log($"LOBBY MESSAGE TRIGGERED {lobbyMsg}");
            var playerID = lobbyMsg.PlayerId;
            if (PlayerLobbyState.PlayerId == playerID)
                Debug.LogWarning("Should user get their own lobby messages?");
            else
                for (var i = 0; i < 3; i++)
                    if (OtherPlayers[i].PlayerId == playerID)
                    {
                        Debug.Log(
                            $"Updating other players info: PlayerId={playerID}, IsReady={lobbyMsg.IsReady}, CharType={lobbyMsg.CharacterType}");
                        OtherPlayers[i].IsReady = lobbyMsg.IsReady;
                        OtherPlayers[i].CharacterType = lobbyMsg.CharacterType;
                        break;
                    }

            Debug.Log($" {OtherPlayers[0].PlayerId} {OtherPlayers[1].PlayerId} {OtherPlayers[2].PlayerId}");
        }

        public void SetReadyFlag()
        {
            if (ReadyFlag.activeSelf)
                PlayerLobbyState.IsReady = false;
            else
                PlayerLobbyState.IsReady = true;
            ReadyFlag.SetActive(PlayerLobbyState.IsReady);
            kcpNetwork.SendLobbyMessage(
                PlayerLobbyState.PlayerId,
                PlayerLobbyState.IsReady,
                PlayerLobbyState.CharacterType
            );
        }

        public void ChangeRole(GameObject dpObj)
        {
            var change = dpObj.GetComponent<TMP_Dropdown>();
            switch (change.value.ToString())
            {
                case "0":
                {
                    PlayerLobbyState.CharacterType = CharacterType.SoulDog;
                    break;
                }
                case "1":
                {
                    PlayerLobbyState.CharacterType = CharacterType.SoulPsychologist;
                    break;
                }
                case "2":
                {
                    PlayerLobbyState.CharacterType = CharacterType.SoulDetective;
                    break;
                }
                case "3":
                {
                    PlayerLobbyState.CharacterType = CharacterType.Reaper;
                    break;
                }
            }

            kcpNetwork.SendLobbyMessage(
                PlayerLobbyState.PlayerId,
                PlayerLobbyState.IsReady,
                PlayerLobbyState.CharacterType
            );
            Debug.Log($"Player - Role change - {PlayerLobbyState.CharacterType} ({change.value.ToString()})");
        }

        public void SetUpKcp()
        {
            kcpNetwork.serverIp = ServerIP;
            kcpNetwork.serverPort = ServerPort;
            kcpNetwork.StartClient();

            var kcpRecvMessageParser = kcpNetwork.GetComponent<KcpRecvMessageParser>();
            kcpRecvMessageParser.onRoomMessageReceived.AddListener(OnReceivingRoomMessage);
            kcpRecvMessageParser.onLobbyMessageReceived.AddListener(OnReceivingLobbyMessage);
            kcpRecvMessageParser.onMapReceived.AddListener(OnMapReceived);
            kcpRecvMessageParser.onGateMessageReceived.AddListener(OnGateMessageReceived);
        }

        private void OnGateMessageReceived(GateMessage message)
        {
            var i = 0;
            foreach (var gate in message.Gates)
            {
                gateDirections.Add(gate.GateDirection);
                if (gate.GateType == GateType.GateHeaven) heavenGateIndex = i;
                i++;
            }
        }

        private void OnMapReceived(StringMessage message)
        {
            mapString = message.MessageContent;
        }

        public bool IsGameStartable()
        {
            var player_count = 1;
            var ready_count = PlayerLobbyState.IsReady ? 1 : 0;
            var reaper_count = PlayerLobbyState.CharacterType == CharacterType.Reaper ? 1 : 0;
            for (var i = 0; i < 3; i++)
            {
                player_count += OtherPlayers[i].PlayerId != -1 ? 1 : 0;
                ready_count += OtherPlayers[i].IsReady ? 1 : 0;
                reaper_count += OtherPlayers[i].CharacterType == CharacterType.Reaper ? 1 : 0;
            }

            if (player_count > 1 && player_count == ready_count && reaper_count == 1) return true;
            return false;
        }

        public void StartTheGame()
        {
            // Populate GameStartData
            var gameStartData = GameStartData.Instance;
            gameStartData.characters = new List<CharacterData>();
            for (var i = 0; i < 3; i++)
                if (OtherPlayers[i].PlayerId != -1)
                    gameStartData.characters.Add(new CharacterData
                    {
                        id = OtherPlayers[i].PlayerId,
                        type = OtherPlayers[i].CharacterType,
                        isPlayer = false,
                        spawnPosition = Vector3.zero // TODO: Set proper spawn position
                    });

            gameStartData.characters.Add(new CharacterData
            {
                id = PlayerLobbyState.PlayerId,
                type = PlayerLobbyState.CharacterType,
                isPlayer = true,
                spawnPosition = Vector3.zero // TODO: Set proper spawn position
            });

            gameStartData.characters = gameStartData.characters.OrderBy(x => x.id).ToList();

            gameStartData.mapInfoContainer = new MapInfoContainer
            {
                soulSpawnPositions = new List<Vector3>(),
                gatePositions = new List<Vector3>(),
                heavenGateIndex = heavenGateIndex,
                mapString = mapString,
                gateDirections = gateDirections
            };

            gameStartData.playerId = PlayerLobbyState.PlayerId;

            // change scene
            Debug.Log("Starting the game");
            SceneManager.LoadScene(Consts.GameScene);
        }

        public void CloseGame()
        {
            Debug.Log("Closing the game");
            Application.Quit();
        }
    }
}