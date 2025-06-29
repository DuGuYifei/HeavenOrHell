using System.Collections.Generic;
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
        [Header("Soul Death Indicator")]
        [SerializeField] private GameObject soulDeathImage;
        [SerializeField] private RectTransform soulDeathFillRectTransform;
        [Header("Heart Container")]
        [SerializeField] private SoulHeartContainer heartContainer;
        
        
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
            heartContainer.gameObject.SetActive(isSoul);
        }
        
        public void SetHearts(float heartRatio)
        {
            heartContainer.SetHearts(heartRatio);
        }
        
        public void TurnDeathIndicatorOn(bool isOn)
        {
            soulDeathImage.SetActive(isOn);
            if (isOn)
            {
                soulDeathFillRectTransform.anchorMax = new Vector2(0.5f, 0f);
            }
        }
        
        public void UpdateSoulWeakTimer(float fillAmount)
        {
            if (soulDeathImage.activeSelf) soulDeathFillRectTransform.anchorMax = new Vector2(0.5f, fillAmount);
        }

        public void UpdateSoulDashCooldown(float fillAmount )
        {
            if (soulDashImage.activeSelf) _souldashFillRectTransform.anchorMax = new Vector2(0.5f, fillAmount);
            soulDashFill.color = fillAmount >= 1? soulDashFillEndColor : soulDashFillingColor;
        }
    }
}