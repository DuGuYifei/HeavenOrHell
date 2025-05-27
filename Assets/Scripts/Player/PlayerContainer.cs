using UnityEngine;

namespace Player
{
    public class PlayerContainer : MonoBehaviour
    {
        
        #region Singleton
        private static PlayerContainer _instance;

        public static PlayerContainer Instance
        {
            get => _instance;
            set => _instance = value;
        }

        private void Awake()
        {
            if (!_instance)
            {
                _instance = this;
            }
        }
        #endregion
    }
}