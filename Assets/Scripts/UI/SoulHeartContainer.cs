using System.Collections.Generic;
using UnityEngine;

namespace DefaultNamespace.UI
{
    public class SoulHeartContainer : MonoBehaviour
    {
        [SerializeField] private List<GameObject> hearts;

        public void SetHearts(float heartRatio)
        {
            int fullHearts = Mathf.RoundToInt(heartRatio * hearts.Count);
            for (var i = 0; i < hearts.Count; i++)
            {
                hearts[i].SetActive(fullHearts > i);
            }
        }
    }
}