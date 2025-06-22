using MiniGames.Altar;
using UnityEngine;

public class AltarController : MonoBehaviour
{
    private AltarMiniGame miniGame;
    public GameObject miniGameController;

    private int WeakSoulCount = 0;
    private int DefaultSoulCount = 0;

    private Animator animator;


    void Start()
    {
        animator = GetComponent<Animator>();

        miniGame = miniGameController.GetComponent<AltarMiniGame>();
        miniGameController.SetActive(false);
    }

    void Update()
    {
        // TODO DELETE AFTER DONE IMPLEMENTING
        if (Input.GetKey("e"))
        {
            StartMiniGame();
        }
    }

    public void OnTriggerEnter2D(Collider2D collision)
    {
        // TODO: soul is weak
        if (true)
        {
            WeakSoulCount += 1;

        }
        // TODO: soul is ok
        if (true)
        {
            DefaultSoulCount += 1;
        }
    }

    public void OnTriggerExit2D(Collider2D collision)
    {
        // TODO: soul is weak
        if (true)
        {
            WeakSoulCount += 1;

        }
        // TODO: soul is ok
        if (true)
        {
            DefaultSoulCount += 1;
        }
    }

    public void StartMiniGame()
    {
        miniGameController.SetActive(true);
        miniGame.enabled = true;
    }

    public void SwitchToIdle()
    {
        animator.SetBool("isAltarActive", false);
    }

    public void SwitchToActive()
    {
        animator.SetBool("isAltarActive", true);
    }

    public void RegenSoul(int id)
    {
        Debug.Log("change a soul back to its good state");
        miniGameController.SetActive(false);
        miniGame.enabled = false;
    }
}
