using Player;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;

    private Transform _cameraTransform;
    private Transform _playerTransform;
    private bool _foundPlayerTransform;

    private void Start()
    {
    }

    private void Update()
    {
        if (!GameManager.Instance || GameManager.Instance.State == GameManager.GameState.BeforeMap) return;
        if (!_foundPlayerTransform)
        {
            _cameraTransform = playerCamera.transform;
            _playerTransform = PlayerContainer.Instance.transform;
            _foundPlayerTransform = true;
        }
        var cameraPosition = _cameraTransform.position;
        var playerPosition = _playerTransform.position;
        cameraPosition.x = playerPosition.x;
        cameraPosition.y = playerPosition.y;
        _cameraTransform.position = cameraPosition;
    }
}
