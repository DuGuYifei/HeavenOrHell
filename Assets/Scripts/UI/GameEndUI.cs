using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UI
{
    public class GameEndUI : MonoBehaviour
    {
        [Header("Soul End Panel")]
        [SerializeField] private GameObject soulEndPanel;
        [SerializeField] private TextMeshProUGUI soulEndText;
        [SerializeField] private string soulEnterGateText = "You entered heaven!";
        [SerializeField] private string soulEnterHellText = "You entered hell!";
        [SerializeField] private string soulDiedOfWeaknessText = "You died because you were weak!";
        
        [Header("Game End Panel")]
        [SerializeField] private GameObject gameEndPanel;
        [SerializeField] private TextMeshProUGUI gameEndText;
        [SerializeField] private Image gameEndBackground;
        [SerializeField] private Color gameEndWinBackground;
        [SerializeField] private Color gameEndLoseBackground;
        [SerializeField] private Color GameEndTieBackground;
        [SerializeField] private float gameEndPanelDelay = 3.0f;
        
        private bool _delayStarted = false;
        private float _delayTime = 0f;
        
        #region Singleton

        private static GameEndUI instance;

        public static GameEndUI Instance => instance;
        
        private void Awake()
        {
            if (instance) return;
            instance = this;
        }

        #endregion

        private void Update()
        {
            if (!_delayStarted) return;
            _delayTime += Time.deltaTime;
            if (_delayTime >= gameEndPanelDelay)
            {
                _delayStarted = false;
                SceneManager.LoadScene(0);
            }
        }
        
        
        public void TurnOnSoulEndPanel(bool isWin, bool isHitByReaper = false)
        {
            soulEndPanel.SetActive(true);
            if (isHitByReaper)
            {
                soulEndText.text = soulDiedOfWeaknessText;
            }
            else
            {
                soulEndText.text = isWin ? soulEnterGateText : soulEnterHellText;
            }
        }

        public void TurnOnGameEndPanel(bool soulWin, bool playerSoul, bool tie)
        {
            gameEndPanel.SetActive(true);
            if (tie)
            {
                gameEndText.text = "It's a tie!";
                gameEndBackground.color = GameEndTieBackground;
            }
            else if (soulWin && playerSoul)
            {
                gameEndText.text = "You won!";
                gameEndBackground.color = gameEndWinBackground;
            }
            else if (!soulWin && playerSoul)
            {
                gameEndText.text = "You lost!";
                gameEndBackground.color = gameEndLoseBackground;
            }
            else if (soulWin)
            {
                gameEndText.text = "The souls got out!";
                gameEndBackground.color = gameEndLoseBackground;
            }
            else
            {
                gameEndText.text = "You Won!";
                gameEndBackground.color = gameEndWinBackground;
            }

            _delayStarted = true;
        }
    }
    
    
}