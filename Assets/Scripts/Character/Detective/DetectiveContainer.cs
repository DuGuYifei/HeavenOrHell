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
            // for( var i = 0 ; i < 4 ; i++ )
            // {
            //     var arrow = Instantiate(arrowPrefab, arrowCanvas.transform);
            // }
        }

        public override void SkillPerformed()
        {
        }
    }
}