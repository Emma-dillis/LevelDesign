using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BallSocketTriggerRelay : MonoBehaviour
{
    public BallSocket socket;

    private void Reset()
    {
        TrySetupTriggerCollider();
        socket = GetComponentInParent<BallSocket>();
    }

    private void Awake()
    {
        TrySetupTriggerCollider();

        if (!socket)
            socket = GetComponentInParent<BallSocket>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (socket)
            socket.TriggerEntered(other);
    }

    private void OnTriggerExit(Collider other)
    {
        if (socket)
            socket.TriggerExited(other);
    }

    private void TrySetupTriggerCollider()
    {
        Collider triggerCollider = GetComponent<Collider>();
        MeshCollider meshCollider = triggerCollider as MeshCollider;

        if (meshCollider && !meshCollider.convex)
        {
            Debug.LogWarning("BallSocketTriggerRelay wymaga prostego trigger collidera. Concave MeshCollider z ProBuildera zostaw jako wizual/kolizje i dodaj child BoxCollider trigger.", this);
            return;
        }

        triggerCollider.isTrigger = true;
    }
}
