using UnityEngine;

namespace Character.Detective
{
    public class DetectiveContainer : SoulContainer
    {
        // [SerializeField] private float skillDuration = 30f;
        [SerializeField] private DetectiveArrowContainer arrowPrefab;
        [SerializeField] private Canvas arrowCanvas;


        

        // private void Update()
        // {
        //     
        // }
        //
        public override void OnInit()
        {
           arrowCanvas.gameObject.SetActive(true);
           //TODO: iterate over all gates. Instantiate arrow for each gate, set target position
        }

        public override void SkillPerformed()
        {
        }
    }
}