using UnityEngine;
using System.Collections;

public class RandomMoveToTarget : GONet.GONetParticipantCompanionBehaviour
{
    public Transform target; // The target to move towards
    public float minWaitTime = 1f; // Minimum wait time in seconds
    public float maxWaitTime = 2f; // Maximum wait time in seconds
    public float minSpeed = 2f; // Minimum movement speed
    public float maxSpeed = 5f; // Maximum movement speed

    protected override void Start()
    {
        base.Start();
        
        if (target == null)
        {
            Debug.LogError("Target not set!");
            return;
        }
    }

    public override void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId)
    {
        base.OnGONetClientVsServerStatusKnown(isClient, isServer, myAuthorityId);

        if (gonetParticipant.IsMine)
        {
            StartCoroutine(MoveCycle());
        }
    }

    private IEnumerator MoveCycle()
    {
        while (true)
        {
            // Wait for a random time between minWaitTime and maxWaitTime
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // Capture the target's position at this moment
            Vector3 targetPosition = target.position;

            // Calculate distance to the captured target position and random speed
            Vector3 startPosition = transform.position;
            float distance = Vector3.Distance(startPosition, targetPosition);
            float speed = Random.Range(minSpeed, maxSpeed);
            float duration = distance / speed;
            float elapsed = 0f;

            // Move towards the captured target position over time
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                transform.position = Vector3.Lerp(startPosition, targetPosition, t);
                yield return null;
            }

            // Ensure the object reaches the exact target position
            transform.position = targetPosition;
        }
    }
}