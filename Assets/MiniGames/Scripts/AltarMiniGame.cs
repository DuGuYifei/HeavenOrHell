using System.Collections;
using Unity.VisualScripting;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.InputSystem;


namespace MiniGames.Altar
{

    public class AltarMiniGame : MonoBehaviour
    {

        public GameObject arrowUp;
        public GameObject arrowDown;
        public GameObject arrowLeft;
        public GameObject arrowRight;

        public InputActionAsset inputAction;
        InputActionMap minigameInputMap;
        
        int[] expectedbuttons = new int[10];
        GameObject[] buttons = new GameObject[10];
        int buttonSequenceLength = 6;
        int currentButton = 0;
        bool readyForInput = false;

        void Awake()
        {
            // MinigameActionMap["Up"].performed += PressUp;
        }

        void OnEnable()
        {
            
        }

        void OnDisable()
        {
            minigameInputMap.Disable();
        }

        void Start()
        {
            minigameInputMap = inputAction.FindActionMap("Main");
            minigameInputMap.Enable();

            Generatebuttonsequence();
            PlaceButtons();
        }

        // Update is called once per frame
        void Update()
        {
            if (minigameInputMap.FindAction("Stop").triggered)
            {
                DeleteButtons();
                currentButton = 0;
                readyForInput = false;
            }
            else if (readyForInput)
            {
                // bool isMistaken = false;
                int chosenButton = -1;
                if (minigameInputMap.FindAction("Up").triggered)
                {
                    chosenButton = 0;
                }
                else if (minigameInputMap.FindAction("Right").triggered)
                {
                    chosenButton = 1;
                }
                else if (minigameInputMap.FindAction("Down").triggered)
                {
                    chosenButton = 2;
                }
                else if (minigameInputMap.FindAction("Left").triggered)
                {
                    chosenButton = 3;
                }

                if (chosenButton == expectedbuttons[currentButton])
                {
                    Debug.Log("Correct Input");
                    Destroy(buttons[currentButton]);

                    currentButton += 1;
                }
                else if (chosenButton > -1)
                {
                    Debug.Log("WRONG INPUT");
                    currentButton = 0;
                    DeleteButtons();
                    Generatebuttonsequence();
                    PlaceButtons();
                }
            }
            if (currentButton == buttonSequenceLength) {
                Debug.Log("Sequence entered correctly! You saved a soul");
                readyForInput = false;
            }
        }

        void Generatebuttonsequence()
        {
            for (int i = 0; i < buttonSequenceLength; ++i)
            {
                expectedbuttons[i] = Random.Range(0,4);
                // expectedbuttons[i] = i % 4;
                Debug.Log(i + " => " + expectedbuttons[i]);
            }
        }

        void PlaceButtons()
        {
            for (int i = 0; i < buttonSequenceLength; ++i)
            {
                switch (expectedbuttons[i]) {
                    case 0:
                    {
                        buttons[i] = Instantiate<GameObject>(arrowUp);
                        break;
                    }
                    case 1:
                    {
                        buttons[i] = Instantiate<GameObject>(arrowRight);
                        break;
                    }
                    case 2:
                    {
                        buttons[i] = Instantiate<GameObject>(arrowDown);
                        break;
                    }
                    case 3:
                    {
                        buttons[i] = Instantiate<GameObject>(arrowLeft);
                        break;
                    }
                    default:
                        Debug.LogError("Wrong Arrow id: " + expectedbuttons[i]);
                        return;
                }
                buttons[i].transform.position = transform.position;
                buttons[i].transform.position = new Vector2
                (
                    buttons[i].transform.position.x - 0.75f * (buttonSequenceLength / 2 - i),
                    buttons[i].transform.position.y                    
                );
            }
            readyForInput = true;
        }

        void DeleteButtons ()
        {
            for (int i = 0; i < buttonSequenceLength; ++i)
            {
                Destroy(buttons[i]);
            }
        }
    }

}