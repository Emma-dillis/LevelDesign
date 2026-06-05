using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BallSocketPuzzleController : MonoBehaviour
{
    [Header("Otwory")]
    public BallSocket[] sockets;

    [Header("Kule wymagane do pelnej aktywacji")]
    public Rigidbody[] acceptedSpheres;
    public bool acceptAnyRigidbodyWhenListIsEmpty = false;

    [Header("Zachowanie")]
    public bool triggerOnlyOnce = true;

    [Header("Zdarzenia do podpiecia pozniej")]
    public UnityEvent onAnyWeightChanged;
    public UnityEvent onAllSocketsWeighted;
    public UnityEvent onAllSpheresPlaced;
    public UnityEvent onSpheresNoLongerPlaced;

    private bool allWeighted;
    private bool solved;
    private bool triggeredOnce;

    public bool IsSolved => solved;

    private void Awake()
    {
        if (sockets == null || sockets.Length == 0)
            sockets = GetComponentsInChildren<BallSocket>();

        foreach (BallSocket socket in sockets)
        {
            if (socket)
                socket.SetController(this);
        }
    }

    public bool IsAcceptedSphere(Rigidbody body)
    {
        if (!body)
            return false;

        if (acceptedSpheres == null || acceptedSpheres.Length == 0)
            return acceptAnyRigidbodyWhenListIsEmpty;

        foreach (Rigidbody acceptedSphere in acceptedSpheres)
        {
            if (acceptedSphere == body)
                return true;
        }

        return false;
    }

    public void SocketStateChanged(BallSocket changedSocket)
    {
        bool currentAllWeighted = AreAllSocketsWeighted();
        bool currentSolved = AreAllSocketsFilledWithUniqueSpheres();

        onAnyWeightChanged?.Invoke();

        if (currentAllWeighted != allWeighted)
        {
            allWeighted = currentAllWeighted;

            if (allWeighted)
                onAllSocketsWeighted?.Invoke();
        }

        if (currentSolved == solved)
            return;

        solved = currentSolved;

        if (solved)
        {
            if (!triggerOnlyOnce || !triggeredOnce)
            {
                triggeredOnce = true;
                onAllSpheresPlaced?.Invoke();
            }
        }
        else
        {
            onSpheresNoLongerPlaced?.Invoke();
        }
    }

    private bool AreAllSocketsWeighted()
    {
        if (sockets == null || sockets.Length == 0)
            return false;

        foreach (BallSocket socket in sockets)
        {
            if (!socket || !socket.HasWeight)
                return false;
        }

        return true;
    }

    private bool AreAllSocketsFilledWithUniqueSpheres()
    {
        if (sockets == null || sockets.Length == 0)
            return false;

        HashSet<Rigidbody> uniqueSpheres = new HashSet<Rigidbody>();

        foreach (BallSocket socket in sockets)
        {
            if (!socket || !socket.HasSphere || !socket.CurrentSphere)
                return false;

            uniqueSpheres.Add(socket.CurrentSphere);
        }

        return uniqueSpheres.Count == sockets.Length;
    }
}
