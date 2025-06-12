using UnityEngine;

namespace Character
{
    public class DogPathContainer : MonoBehaviour
    {
        [SerializeField] private Renderer pathRenderer;

        public void SetColor(Color color)
        {
            pathRenderer.material.color = color;
        }
    }
}