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

        [Header("Reaper Attack Indicator")] 
        [SerializeField] private GameObject reaperAttackImage;
        [SerializeField] private Image reaperAttackFill;
        
        [Header("Heart Container")]
        [SerializeField] private SoulHeartContainer heartContainer;
        
        
        private RectTransform _soulDashFillRectTransform;
        private RectTransform _reaperAttackFillRectTransform;
        
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
            _soulDashFillRectTransform = soulDashFill.GetComponent<RectTransform>();
            _reaperAttackFillRectTransform = reaperAttackFill.GetComponent<RectTransform>();
        }

        public void InitializeUI(bool isSoul)
        {
            soulDashImage.SetActive(isSoul);
            heartContainer.gameObject.SetActive(isSoul);
            reaperAttackImage.SetActive(!isSoul);
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
            if (soulDashImage.activeSelf) _soulDashFillRectTransform.anchorMax = new Vector2(0.5f, fillAmount);
            soulDashFill.color = fillAmount >= 1? soulDashFillEndColor : soulDashFillingColor;
        }
        
        public void UpdateReaperAttackCooldown(float fillAmount)
        {
            if (reaperAttackImage.activeSelf) _reaperAttackFillRectTransform.anchorMax = new Vector2(0.5f, fillAmount);
            reaperAttackFill.color = fillAmount >= 1 ? soulDashFillEndColor : soulDashFillingColor;
        }
    }
}