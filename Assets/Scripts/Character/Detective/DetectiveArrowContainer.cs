using System;
using UnityEngine;
using utils;

namespace Character.Detective
{
    public class DetectiveArrowContainer : MonoBehaviour
    {
        [SerializeField] private Vector3 targetPosition;
        [SerializeField] private float distanceToPlayer = 0.3f;

        private RectTransform _arrowRectTransform;
        private float _screenRes;
        private Camera _camera;
        private float _distanceToPlayerPixels;

        private void Start()
        {
            _arrowRectTransform = (RectTransform) transform;
            _camera = GameManager.Instance.mainCamera;
            _screenRes = Screen.height < Single.Epsilon? Screen.height : Screen.width;
            _distanceToPlayerPixels = _screenRes * distanceToPlayer;
        }
        

        private void Update()
        {
            var targetScreenPos = _camera.WorldToScreenPoint(targetPosition);
            var screenMid = new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
            var direction = (targetScreenPos.XY() - screenMid).normalized;
            var angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            _arrowRectTransform.rotation = Quaternion.Euler(new Vector3(0, 0, angle));
            _arrowRectTransform.position = screenMid + direction * _distanceToPlayerPixels;
        }
        
        public void SetArrowTarget(Vector3 targetPosition)
        {
            this.targetPosition = targetPosition;
        }
    }
}