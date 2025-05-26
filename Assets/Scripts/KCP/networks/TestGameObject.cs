using UnityEngine;

namespace AntMill.Liu.Scripts.networks
{
    public class TestGameObject : MonoBehaviour
    {
        public KcpNetwork kcpNetwork;
        
        private void Start()
        {
            kcpNetwork.StartConnect();
        }
    }
}