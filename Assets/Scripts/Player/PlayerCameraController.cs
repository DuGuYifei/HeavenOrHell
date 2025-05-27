using Player;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;

    private Transform _cameraTransform;
    private Transform _playerTransform;

    private void Start()
    {
        _cameraTransform = playerCamera.transform;
        _playerTransform = PlayerContainer.Instance.transform;
    }

    private void Update()
    {
        var cameraPosition = _cameraTransform.position;
        var playerPosition = _playerTransform.position;
        cameraPosition.x = playerPosition.x;
        cameraPosition.y = playerPosition.y;
        _cameraTransform.position = cameraPosition;
    }
}
