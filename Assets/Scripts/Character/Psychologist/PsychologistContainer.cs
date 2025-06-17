using UnityEngine;

namespace Character.Psychologist
{
    public class PsychologistContainer : SoulContainer
    {
        [SerializeField] private float skillRange = 5f;
        private bool _usedSkill = false;
        public override void OnInit()
        {
            throw new System.NotImplementedException();
        }

        public override void SkillPerformed()
        {
            if (_usedSkill) return;
            var nearestGate = -1;
            var minDistance = float.MaxValue;
            foreach (var gate in GameManager.Instance.mapInfoContainer.gatePositions)
            {
                var distance = Vector3.Distance(transform.position, gate);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearestGate = GameManager.Instance.mapInfoContainer.gatePositions.IndexOf(gate);
                }
            }

            if (minDistance > skillRange) return;
            _usedSkill = true;
            if (nearestGate != GameManager.Instance.mapInfoContainer.heavenGateIndex)
            {
                print("gate not heaven");
                //TODO: gate is not heaven gate. Change gate color?
            }
            else
            {
                print("gate is heaven");
                //TODO: gate is heaven gate. Change gate color?
            }
        }
    }
}