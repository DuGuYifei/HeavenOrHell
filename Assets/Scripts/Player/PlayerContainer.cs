using UnityEngine;

namespace Player
{
    public class PlayerContainer : MonoBehaviour
    {
        public float hp = 100;
        public float maxHp = 100;
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