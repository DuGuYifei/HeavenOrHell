using UnityEngine;

namespace Character
{
    public class DogContainer : SoulContainer
    {
        public override void OnInit()
        {
            GameManager.Instance.dogPathManager.TurnOnPathChecking();
        }

        public override void SkillPerformed()
        {
            // No skills
        }
        
        
    }
}