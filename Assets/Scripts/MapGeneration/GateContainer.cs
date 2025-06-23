using UnityEngine;

namespace MapGeneration
{
    public class GateContainer : MonoBehaviour
    {
        [SerializeField] private Color hellColor = Color.red;
        [SerializeField] private Color heavenColor = Color.blue;
        
        public void ChangeColor(bool isHeaven)
        {
            // Change the color of the gate based on whether it's heaven or hell
            var renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = isHeaven ? heavenColor : hellColor;
            }
        }
        
    }
}