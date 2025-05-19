using UnityEngine;

namespace network
{
    public class MessageParser : MonoBehaviour
    {
        private void Start()
        {
            NetworkManager.Instance.onNetworkEvent.AddListener(MessageReceived);
        }


        private void MessageReceived(string message)
        {
            
        }
    }
}