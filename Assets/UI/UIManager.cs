using UnityEditor;
using UnityEngine;

using Message;
using AntMill.Liu.Scripts.networks;
using System.Net;
using System.Net.Sockets;
using Google.Protobuf;
using KcpProject;
using System;
using System.Collections.Generic;
using DefaultNamespace;
using Google.Protobuf.Collections;
using MapGeneration;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;
using network;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;


namespace UI
{

    enum MenuState
    {
        MainMenu,
        Connection,
        Lobby
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

        public GameObject KcpNetworkPrefab;
        private GameObject KcpNetworkEntity;
        public GameObject MainMenu;

        public GameObject Connection;

        public GameObject Lobby;

        public GameObject LobbyId;

        public GameObject LobbyEntryField;

        public GameObject ReadyFlag;

        public KcpNetwork kcpNetwork;

        LobbyPlayerInfo PlayerLobbyState;
        bool IsHost = false;

        public LobbyPlayerInfo[] OtherPlayers = new LobbyPlayerInfo[3];
        public GameObject[] PlayerIcons;
        public TMP_Dropdown PlayerRoleDropdown;

        private MenuState state = MenuState.MainMenu;

        public float StartTimer = 2.0f;
        public float ActualTimer;

        private string mapString = "";
        void Start()
        {
            ActualTimer = StartTimer;

            state = MenuState.MainMenu;
            MainMenu.SetActive(true);
            Connection.SetActive(false);
            Lobby.SetActive(false);
            ReadyFlag.SetActive(false);

            for (int i = 0; i < 3; i++)
            {
                OtherPlayers[i].IsReady = false;
                OtherPlayers[i].PlayerId = -1;
                OtherPlayers[i].CharacterType = CharacterType.SoulDog;
            }
            PlayerLobbyState.IsReady = false;
            PlayerLobbyState.PlayerId = -1;
            PlayerLobbyState.CharacterType = CharacterType.SoulDog;

            SetUpKcp();
        }

        void Update()
        {
            if (IsGameStartable())
            {
                ActualTimer -= Time.deltaTime;
            }
            else
            {
                ActualTimer = StartTimer;
            }
            if (ActualTimer <= 0.0f)
            {
                StartTheGame();
            }
            for (int i = 0; i < 3; i++)
                {
                    if (OtherPlayers[i].PlayerId == -1)
                    {
                        PlayerIcons[i].SetActive(false);
                    }
                    else
                    {
                        PlayerIcons[i].SetActive(true);
                        try
                        {
                            LobbyOtherPlayerController ct = PlayerIcons[i].GetComponent<LobbyOtherPlayerController>();
                            ct.SetReady(OtherPlayers[i].IsReady);
                            ct.UpdateRole(OtherPlayers[i].CharacterType);
                        }
                        catch
                        {
                            Debug.Log("Could not properly change UI elements for other players");
                        }
                    }
                }
        }

        public void GoBack()
        {
            switch (state)
            {
                case MenuState.Connection:
                    {
                        Connection.SetActive(false);
                        MainMenu.SetActive(true);
                        state = MenuState.MainMenu;
                        break;
                    }
                case MenuState.Lobby:
                    {
                        DisconnectFromLobby();
                        Lobby.SetActive(false);
                        MainMenu.SetActive(true);
                        state = MenuState.MainMenu;
                        break;
                    }
                default:
                    {
                        break;
                    }
            }
        }

        public void DisconnectFromLobby()
        {
            Debug.Log("Disconnect from Lobby");
            Destroy(KcpNetworkEntity);
            IsHost = false;
        }

        public void CreateLobby()
        {
            try
            {
                Debug.Log("Creating a lobby");

                SetUpKcp();

                kcpNetwork.StartConnect();
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

                int roomId = int.Parse(LobbyEntryField.GetComponent<TMP_InputField>().text);

                Debug.Log($"Connecting to the lobby with a code '{roomId}'");

                kcpNetwork.StartConnect(roomId);
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

        public void OnReceivingRoomMessage(RoomMessage roomMsg)
        {
            Debug.Log($"Room Message is received: {roomMsg}");
            Debug.Log($"user 1 playerid {roomMsg.Characters[0].PlayerId}");
            LobbyId.GetComponent<TMP_Text>().text = $"room: {roomMsg.RoomId}";
            if (roomMsg.IsJoin && PlayerLobbyState.PlayerId == -1)
            {
                PlayerLobbyState.PlayerId = roomMsg.PlayerId;
            }
            Debug.Log("Setting up players " + PlayerLobbyState.PlayerId);
            // TODO
            
            for (int i = 0; i < roomMsg.Characters.Count; i++)
            {
                if (i == PlayerLobbyState.PlayerId)
                {
                    switch(roomMsg.Characters[i].CharacterType)
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
                }
                else
                {
                    bool existingPlayer = false;
                    for (int j = 0; j < 3; j++)
                    {
                        if (OtherPlayers[j].PlayerId == i)
                        {
                            OtherPlayers[j].CharacterType = roomMsg.Characters[i].CharacterType;
                            existingPlayer = true;
                            break;
                        }
                    }
                    if (!existingPlayer)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            if (OtherPlayers[j].PlayerId == -1)
                            {
                                OtherPlayers[j].PlayerId = roomMsg.Characters[i].PlayerId;
                                OtherPlayers[j].CharacterType = roomMsg.Characters[i].CharacterType;
                                break;
                            }
                        }
                    }
                }
                // if (OtherPlayers[i].PlayerId == -1)
                // {
                //     newPlIndex = i;
                // }
                // roomMsg.Characters[i];
            }

            Debug.Log($" {OtherPlayers[0]} {OtherPlayers[1]} {OtherPlayers[2]}");
        }

        public void OnReceivingLobbyMessage(LobbyMessage lobbyMsg)
        {
            Debug.Log("LOBBY MESSAGE TRIGGERED");
            int playerID = lobbyMsg.PlayerId;
            if (PlayerLobbyState.PlayerId == playerID)
            {
                Debug.LogWarning("Should user get their own lobby messages?");
            }
            else
            {
                for (int i = 0; i < 3; i++)
                {
                    if (OtherPlayers[i].PlayerId == playerID)
                    {
                        Debug.Log($"Updating other players info: PlayerId={playerID}, IsReady={lobbyMsg.IsReady}, CharType={lobbyMsg.CharacterType}");
                        OtherPlayers[i].IsReady = lobbyMsg.IsReady;
                        OtherPlayers[i].CharacterType = lobbyMsg.CharacterType;
                        break;
                    }
                }
            }

            Debug.Log($" {OtherPlayers[0].PlayerId} {OtherPlayers[1].PlayerId} {OtherPlayers[2].PlayerId}");
        }

        public void SetReadyFlag()
        {
            if (ReadyFlag.activeSelf)
            {
                PlayerLobbyState.IsReady = false;
            }
            else
            {
                PlayerLobbyState.IsReady = true;
            }
            ReadyFlag.SetActive(PlayerLobbyState.IsReady);
            kcpNetwork.SendLobbyMessage(
                PlayerLobbyState.PlayerId,
                PlayerLobbyState.IsReady,
                PlayerLobbyState.CharacterType
            );
        }

        public void ChangeRole(GameObject dpObj)
        {
            TMP_Dropdown change = dpObj.GetComponent<TMP_Dropdown>();
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
                default:
                    break;
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
            if (KcpNetworkEntity == null)
            {
                kcpNetwork.serverIp = ServerIP;
                kcpNetwork.serverPort = ServerPort;
                kcpNetwork.StartClient();
            }
            
            var kcpRecvMessageParser = kcpNetwork.GetComponent<KcpRecvMessageParser>();
            kcpRecvMessageParser.onRoomMessageReceived.AddListener(OnReceivingRoomMessage);
            kcpRecvMessageParser.onLobbyMessageReceived.AddListener(OnReceivingLobbyMessage);
            kcpRecvMessageParser.onMapReceived.AddListener(OnMapReceived);
        }

        private void OnMapReceived(StringMessage message)
        {
            mapString = message.MessageContent;
        }

        public bool IsGameStartable()
        {
            int player_count = 1;
            int ready_count = PlayerLobbyState.IsReady ? 1 : 0;
            int reaper_count = PlayerLobbyState.CharacterType == CharacterType.Reaper ? 1 : 0;
            for (int i = 0; i < 3; i++)
            {
                player_count += OtherPlayers[i].PlayerId != -1 ? 1 : 0;
                ready_count += OtherPlayers[i].IsReady ? 1 : 0;
                reaper_count += OtherPlayers[i].CharacterType == CharacterType.Reaper ? 1 : 0;
            }
            if (player_count > 1 && player_count == ready_count && reaper_count == 1)
            {
                return true;
            }
            return false;
        }

        public void StartTheGame()
        {
            // Populate GameStartData
            var gameStartData = GameStartData.Instance;
            gameStartData.characters = new List<CharacterData>();
            for (int i = 0; i < 3; i++)
            {
                if (OtherPlayers[i].PlayerId != -1)
                {
                    gameStartData.characters.Add(new CharacterData
                    {
                        id = OtherPlayers[i].PlayerId,
                        type = OtherPlayers[i].CharacterType,
                        isPlayer = false,
                        spawnPosition = Vector3.zero // TODO: Set proper spawn position
                    });
                }
            }
            gameStartData.characters.Add(new CharacterData
            {
                id = PlayerLobbyState.PlayerId,
                type = PlayerLobbyState.CharacterType,
                isPlayer = true,
                spawnPosition = Vector3.zero // TODO: Set proper spawn position
            });

            gameStartData.mapInfoContainer = new MapInfoContainer
            {
                soulSpawnPositions = new List<Vector3>(),
                gatePositions = new List<Vector3>(),
                heavenGateIndex = 0,
                mapString = mapString,
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