using AntMill.Liu.Scripts.networks;
using Message;
using network;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ChatUI : MonoBehaviour
    {
        public TMP_Text historyText;
        public TMP_InputField inputField;
        private KcpNetwork _kcpNetwork;
        private string _playerType;

        void Start()
        {
            KcpRecvMessageParser.Instance?.onChatMessageReceived.AddListener(OnChatMessageReceived);
            _kcpNetwork = KcpNetwork.Instance;
            _playerType = GameStartData.Instance.characters.Find(c => c.isPlayer).type.ToString();
            // Enter button
            inputField.onSubmit.AddListener(OnSendClick);
            inputField.onSelect.AddListener(OnChatMessageSelected);
            inputField.onDeselect.AddListener(OnChatMessageDeselected);
        }

        void OnSendClick()
        {
            string msg = inputField.text.Trim();
            if (string.IsNullOrEmpty(msg))
                return;

            SendMessageToServer(msg);

            inputField.text = "";
            inputField.DeactivateInputField();
            GameManager.Instance.TurnPlayerControl(true);
            //Deselect the input field
            inputField.OnDeselect(null);
        }

        // Support TMP_InputField onSubmit(string)
        void OnSendClick(string text)
        {
            OnSendClick();
        }
        
        void OnChatMessageSelected(string text)
        {
            GameManager.Instance.TurnPlayerControl(false);
        }

        private void OnChatMessageDeselected(string arg0)
        {
            GameManager.Instance.TurnPlayerControl(true);
        }
        void SendMessageToServer(string msg)
        {
            _kcpNetwork.SendChatMessage(_playerType, msg);
        }

        void OnChatMessageReceived(ChatMessage message)
        {
            // Append the message to the history text
            historyText.text += $"<color=yellow>{message.FromPlayer}:</color> {message.Content}\n";
        }
    }
}