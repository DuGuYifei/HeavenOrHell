using MiniGames.Altar;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;
using System.Diagnostics.Eventing.Reader;
using AntMill.Liu.Scripts.networks;

public class AltarController : MonoBehaviour
{
    private AltarMiniGame miniGame;
    public GameObject miniGameController;

    private int weakSoulCount = 0;
    private int defaultSoulCount = 0;

    private Animator animator;

    private List<GameObject> currentCollisions = new List<GameObject> ();
    public InputActionAsset inputAction;
    InputActionMap minigameActivationMap;

    private bool isPlayerDefaultSoulInside = false;

    void Start()
    {
        minigameActivationMap = inputAction.FindActionMap("Main");
        minigameActivationMap.Disable();

        animator = GetComponent<Animator>();

        miniGame = miniGameController.GetComponent<AltarMiniGame>();
        miniGameController.SetActive(false);
    }

    void Update()
    {
        if (minigameActivationMap.FindAction("Activate").triggered)
        {
            StartMiniGame();
        }
        else if (minigameActivationMap.FindAction("Stop").triggered)
        {
            miniGame.StopTheMiniGame();
        }

        weakSoulCount = 0;
        defaultSoulCount = 0;
        foreach (GameObject gObject in currentCollisions)
        {
            SoulContainer soulCont = gObject.GetComponent<SoulContainer>();
            if (soulCont != null)
            {
                // TODO check if it is weak soul
                if (soulCont.isWeak)
                {
                    weakSoulCount += 1;
                }
                // TODO check if it is regular soul
                else
                {
                    defaultSoulCount += 1;

                    if (soulCont.id == GameManager.Instance.PlayerId)
                    {
                        isPlayerDefaultSoulInside = true;
                    }
                }

            }
        }
        if (weakSoulCount > 0 && defaultSoulCount > 0 && isPlayerDefaultSoulInside)
        {
            SwitchToActive();
            minigameActivationMap.Enable();
        }
        else
        {
            SwitchToIdle();
            minigameActivationMap.Disable();
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        currentCollisions.Add (collision.gameObject);
        
		foreach (GameObject gObject in currentCollisions)
        {
            print(gObject.name);
        }

    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        currentCollisions.Remove (collision.gameObject);

		foreach (GameObject gObject in currentCollisions)
        {
			print (gObject.name);
		}
    }

    public void StartMiniGame()
    {
        miniGameController.SetActive(true);
        miniGame.enabled = true;
        miniGame.BeginTheMiniGame();
        GameManager.Instance.TurnPlayerControl(true);
    }

    public void SwitchToIdle()
    {
        animator.SetBool("isAltarActive", false);
    }

    public void SwitchToActive()
    {
        animator.SetBool("isAltarActive", true);
    }

    public void RegenSoul()
    {
        // TODO
        foreach (GameObject gObject in currentCollisions)
        {
            SoulContainer soulCont = gObject.GetComponent<SoulContainer>();
            if (soulCont != null)
            {
                if (soulCont.isWeak)
                {
                    weakSoulCount += 1;
                    soulCont.isWeak = false;
                    KcpNetwork.Instance.SendAltarSuccessMessage(soulCont.id);
                    GameManager.Instance.TurnPlayerControl(false);
                }

            }
        }

        miniGameController.SetActive(false);
        miniGame.enabled = false;
    }
}
