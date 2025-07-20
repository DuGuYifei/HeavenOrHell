using UnityEngine;
using Message;
using AntMill.Liu.Scripts.networks;
using TMPro;

public class LobbyOtherPlayerController : MonoBehaviour
{

    public GameObject ReadyFlag;
    public GameObject RoleText;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    public void SetReady(bool newReady)
    {
        ReadyFlag.SetActive(newReady);
    }

    public void UpdateRole(CharacterType newRole)
    {
        switch (newRole)
        {
            case CharacterType.Reaper:
                {
                    RoleText.GetComponent<TMP_Text>().text = "Reaper";
                    break;
                }
            case CharacterType.SoulDog:
                {
                    RoleText.GetComponent<TMP_Text>().text = "Soul Dog";
                    break;
                }
            case CharacterType.SoulPsychologist:
                {
                    RoleText.GetComponent<TMP_Text>().text = "Soul Psychologist";
                    break;
                }
            case CharacterType.SoulDetective:
                {
                    RoleText.GetComponent<TMP_Text>().text = "Soul Detective";
                    break;
                }
            default:
                break;

        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
