using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI
{
    public class SubmitPageContainer : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private InputActionAsset playerActionAsset;
        
        private InputAction _submitAction;

        private void Awake()
        {
            var actionMap = playerActionAsset.FindActionMap("UI", true);
            _submitAction = actionMap.FindAction("Submit", true);
        }


        void OnEnable()
        {
            _submitAction.Enable();
            _submitAction.performed += OnSubmitPerformed;
        }

        void OnDisable()
        {
            _submitAction.performed -= OnSubmitPerformed;
            _submitAction.Disable();
        }

        private void OnSubmitPerformed(InputAction.CallbackContext context)
        {
            if (button != null)
            {
                button.onClick.Invoke();
            }
        }
    }
}