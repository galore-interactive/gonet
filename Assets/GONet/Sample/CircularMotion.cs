using GONet;
using UnityEngine;

public class CircularMotion : GONetParticipantCompanionBehaviour
{
    [Header("Movement Settings")]
    public float radius = 5f; // The radius of the circular path
    public float angularSpeed = 30f; // The speed of the circular motion (degrees per second)

    [Header("Rotation Settings")]
    public float rotationSpeed = 90f; // The speed of the rotation around the object's own axis (degrees per second)

    private float angle = 0f; // Current angle in radians

    [GONetAutoMagicalSync]
    public float NettyWorkedFloat { get; set; }

    public override void OnGONetClientVsServerStatusKnown(bool isClient, bool isServer, ushort myAuthorityId)
    {
        base.OnGONetClientVsServerStatusKnown(isClient, isServer, myAuthorityId);

        if (GONetParticipant.IsMine)
        {
            angle = UnityEngine.Random.Range(0, Mathf.PI) * 2f;
            radius *= Random.Range(0.25f, 1.5f);
            rotationSpeed *= Random.Range(0.25f, 1.5f);
            angularSpeed *= Random.Range(0.25f, 1.5f);
        }
    }

    void Update()
    {
        if (GONetParticipant.IsMine)
        {
            // Calculate the new position in the circular path
            angle += angularSpeed * Time.deltaTime; // Update the angle based on angular speed
            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * radius;
            float z = Mathf.Sin(angle * Mathf.Deg2Rad) * radius;
            transform.position = new Vector3(x, transform.position.y, z);

            // Rotate the object around its own axis
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

            // just testing some network stuff
            NettyWorkedFloat += Time.deltaTime;
        }
    }
}
