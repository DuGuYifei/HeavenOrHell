using TMPro;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class GameEndUI : MonoBehaviour
    {
        [SerializeField] private GameObject gameEndPanel;
        [SerializeField] private TextMeshProUGUI gameEndText;

        [Header("Texts")] [SerializeField] private string winText = "You Entered Heaven!";
        [SerializeField] private string loseText = "You Entered Hell!";
        [SerializeField] private string hitByReaperText = "You were hit by the Reaper!";
        
        
        
        #region Singleton

        private static GameEndUI instance;

        public static GameEndUI Instance => instance;
        
        private void Awake()
        {
            if (instance) return;
            instance = this;
        }

        #endregion
        
        public void TurnOnGameEndPanel(bool isWin, bool isHitByReaper = false)
        {
            gameEndPanel.SetActive(true);
            if (isHitByReaper)
            {
                gameEndText.text = hitByReaperText;
            }
            else
            {
                gameEndText.text = isWin ? winText : loseText;
            }
        }
    }
}