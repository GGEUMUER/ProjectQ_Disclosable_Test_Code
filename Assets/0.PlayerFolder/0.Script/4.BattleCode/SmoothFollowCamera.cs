using System.Collections;
using UnityEngine;

public class SmoothFollowCamera : MonoBehaviour
{
    public float smoothSpeed = 5f; // 초당 얼마나 따라갈지 (값을 키우면 더 빠르게 따라감)
    public Vector3 offset = Vector3.zero;
    public float stopThreshold = 0.01f;
    private Coroutine followRoutine;

    public void CoroutineCamera(Vector3 targetPosition)
    {
        if (followRoutine != null)
            StopCoroutine(followRoutine);

        followRoutine = StartCoroutine(SmoothFollow(targetPosition));
    }

    IEnumerator SmoothFollow(Vector3 targetPosition)
    {
        Vector3 destination = targetPosition + offset;

        while (true)
        {
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, destination, smoothSpeed * Time.deltaTime);
            transform.position = smoothedPosition;

            if (Vector3.Distance(transform.position, destination) < stopThreshold)
            {
                transform.position = destination;
                followRoutine = null;
                yield break;
            }

            yield return null;
        }
    }
}