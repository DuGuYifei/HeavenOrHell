using UnityEngine;

namespace Character
{
    public class DogContainer : SoulContainer
    {
        public override void OnInit()
        {
            if (GameManager.Instance.PlayerId != id) return;
            GameManager.Instance.dogPathManager.TurnOnPathChecking();
        }

        public override void SkillPerformed()
        {
            // No skills
        }
        
        
    }
}