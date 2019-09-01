using GONet;
using UnityEngine;

[RequireComponent(typeof(GONetParticipant))]
public class PendulumJalou : MonoBehaviour
{
    internal GONetParticipant gnp;
    Vector3 position1, position2;
    public float speed = 1.5f;

    private void Awake()
    {
        gnp = GetComponent<GONetParticipant>();
    }

    void Start()
    {
        position1 = transform.position - new Vector3(20, 0, 0);
        position2 = transform.position + new Vector3(20, 0, 0);
    }

    private void Update()
    {
        if (GONetMain.IsMine(gnp))
        {
            transform.position = Vector3.Lerp(position1, position2, (Mathf.Sin(speed * Time.time) + 1.0f) / 2.0f);
        }
    }
}
