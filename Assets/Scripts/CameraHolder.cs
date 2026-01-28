using UnityEngine;

public class CameraHolder : MonoBehaviour
{
    public Transform holder;

    // Update is called once per frame
    void Update()
    {
        transform.position = holder.position;
    }
}
