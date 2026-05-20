using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera != null)
        {
            // face camera horizontally only
            Vector3 directionToCamera = mainCamera.transform.position - transform.position;
            directionToCamera.y = 0; // ignore vertical
            
            // look that way
            if (directionToCamera.magnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(directionToCamera);
                transform.Rotate(0, 180, 0); // flip if needed
            }
        }
    }
}
