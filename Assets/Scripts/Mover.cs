using UnityEngine;
using System.Collections;

public class Mover : MonoBehaviour
{
    public GameObject fence;
    public float moveAmount = 3f;
    public float speed = 2f;

    private Vector3 startPosition;
    private Vector3 targetPosition;
    private bool moveFence = false;

    void Start()
    {
        startPosition = fence.transform.localPosition;
        targetPosition = startPosition;
    }

    void Update()
    {
        if (moveFence)
        {
            fence.transform.localPosition = Vector3.MoveTowards(
                fence.transform.localPosition,
                targetPosition,
                speed * Time.deltaTime
            );

            // stop moving once we've reached the desired position
            if (fence.transform.localPosition == targetPosition)
            {
                moveFence = false;
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {

        if (other.CompareTag("Player"))
        {
            StopAllCoroutines();
            StartCoroutine(MoveFence());
        }
    }

    private IEnumerator MoveFence()
    {
        targetPosition = startPosition + Vector3.down * moveAmount;
        moveFence = true;
        yield return new WaitForSeconds(3f);
        targetPosition = startPosition;
        moveFence = true;
    }
}