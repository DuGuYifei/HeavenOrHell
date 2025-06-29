using UnityEngine;
using UnityEngine.UI;

namespace DefaultNamespace.UI
{
    public class InGameUIManager : MonoBehaviour
    {
        [Header("Soul Boost Indictaor")]
        [SerializeField] private GameObject soulDashImage;
        [SerializeField] private Image soulDashFill;
        [SerializeField] private Color soulDashFillingColor;
        [SerializeField] private Color soulDashFillEndColor;
        
        
        private RectTransform _souldashFillRectTransform;
        
        #region Singleton

        private static InGameUIManager instance;

        public static InGameUIManager Instance => instance;

        private void Awake()
        {
            if (instance) return;
            instance = this;
        }
        
        #endregion

        private void Start()
        {
            _souldashFillRectTransform = soulDashFill.GetComponent<RectTransform>();
        }

        public void InitializeUI(bool isSoul)
        {
            soulDashImage.SetActive(isSoul);
        }

        public void UpdateSoulDashCooldown(float fillAmount )
        {
            if (soulDashImage.activeSelf) _souldashFillRectTransform.anchorMax = new Vector2(0.5f, fillAmount);
            soulDashFill.color = fillAmount >= 1? soulDashFillEndColor : soulDashFillingColor;
        }
    }
}