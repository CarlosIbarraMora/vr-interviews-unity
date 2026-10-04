using UnityEngine;

public class OperatorCameraFollow : MonoBehaviour
{
    [SerializeField]
    private Transform xrCamera;

    private void LateUpdate()
    {
        if (xrCamera == null)
        {
            return;
        }

        transform.position = xrCamera.position;
        transform.rotation = xrCamera.rotation;
    }
}