using UnityEngine;

namespace Character.Detective
{
    public class DetectiveContainer : SoulContainer
    {
        // [SerializeField] private float skillDuration = 30f;
        [SerializeField] private DetectiveArrowContainer arrowPrefab;
        [SerializeField] private Canvas arrowCanvas;

        
        public override void OnInit()
        {
            base.OnInit();
            if (GameManager.Instance.PlayerId != id) return;
            arrowCanvas.gameObject.SetActive(true);
            foreach(var gatePosition in GameManager.Instance.mapInfoContainer.gatePositions)
            {
                // Instantiate arrow for each gate
                var arrow = Instantiate(arrowPrefab, arrowCanvas.transform);
                arrow.SetArrowTarget(gatePosition);
            }
        }

        public override void SkillPerformed()
        {
        }
    }
}