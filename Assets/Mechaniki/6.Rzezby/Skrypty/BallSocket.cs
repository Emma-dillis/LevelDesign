using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class BallSocket : MonoBehaviour
{
    [Header("Kontroler")]
    public BallSocketPuzzleController controller;

    [Header("Trigger wykrywania")]
    public Collider detectionCollider;
    public Vector3 defaultTriggerSize = new Vector3(1.2f, 0.35f, 1.2f);
    public Vector3 defaultTriggerLocalOffset = new Vector3(0f, 0.15f, 0f);

    [Header("Kule")]
    public Rigidbody[] acceptedSpheres;

    [Header("Reakcja na ciezar")]
    public Transform socketVisual;
    public Vector3 pressedLocalOffset = new Vector3(0f, -0.05f, 0f);
    public float visualMoveSpeed = 8f;

    [Header("Zdarzenia")]
    public UnityEvent onWeightPressed;
    public UnityEvent onWeightReleased;
    public UnityEvent onSpherePlaced;
    public UnityEvent onSphereRemoved;

    private readonly HashSet<Collider> weightColliders = new HashSet<Collider>();
    private readonly HashSet<Rigidbody> spheresInSocket = new HashSet<Rigidbody>();
    private Vector3 visualStartLocalPosition;
    private bool hadWeight;
    private bool hadSphere;

    public bool HasWeight => weightColliders.Count > 0;
    public bool HasSphere => spheresInSocket.Count > 0;

    public Rigidbody CurrentSphere
    {
        get
        {
            foreach (Rigidbody sphere in spheresInSocket)
                return sphere;

            return null;
        }
    }

    private void Reset()
    {
        socketVisual = transform;
        controller = GetComponentInParent<BallSocketPuzzleController>();
        EnsureDetectionCollider();
    }

    private void Awake()
    {
        EnsureDetectionCollider();

        if (!controller)
            controller = GetComponentInParent<BallSocketPuzzleController>();

        if (!socketVisual)
            socketVisual = transform;

        visualStartLocalPosition = socketVisual.localPosition;
    }

    private void Update()
    {
        UpdateVisual();
    }

    public void TriggerEntered(Collider other)
    {
        if (!other || other.transform == transform)
            return;

        weightColliders.Add(other);

        Rigidbody sphere = GetSphereRigidbody(other);
        if (sphere)
            spheresInSocket.Add(sphere);

        RefreshState();
    }

    public void TriggerExited(Collider other)
    {
        if (!other)
            return;

        weightColliders.Remove(other);

        Rigidbody sphere = GetSphereRigidbody(other);
        if (sphere)
            spheresInSocket.Remove(sphere);

        RefreshState();
    }

    public void SetController(BallSocketPuzzleController newController)
    {
        controller = newController;
    }

    private void EnsureDetectionCollider()
    {
        if (detectionCollider && !CanBeTrigger(detectionCollider))
            detectionCollider = null;

        if (!detectionCollider)
            detectionCollider = FindUsableTriggerCollider();

        if (!detectionCollider)
            detectionCollider = CreateDefaultTriggerCollider();

        if (!detectionCollider)
            return;

        detectionCollider.isTrigger = true;

        BallSocketTriggerRelay relay = detectionCollider.GetComponent<BallSocketTriggerRelay>();
        if (!relay)
            relay = detectionCollider.gameObject.AddComponent<BallSocketTriggerRelay>();

        relay.socket = this;
    }

    private Collider FindUsableTriggerCollider()
    {
        Collider[] colliders = GetComponentsInChildren<Collider>(true);

        foreach (Collider candidate in colliders)
        {
            if (!candidate || candidate.transform == transform)
                continue;

            if (CanBeTrigger(candidate))
                return candidate;
        }

        Collider ownCollider = GetComponent<Collider>();
        if (CanBeTrigger(ownCollider))
            return ownCollider;

        return null;
    }

    private Collider CreateDefaultTriggerCollider()
    {
        GameObject triggerObject = new GameObject("BallSocketTrigger");
        triggerObject.transform.SetParent(transform, false);
        triggerObject.transform.localPosition = defaultTriggerLocalOffset;
        triggerObject.transform.localRotation = Quaternion.identity;
        triggerObject.transform.localScale = Vector3.one;

        BoxCollider triggerCollider = triggerObject.AddComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.size = defaultTriggerSize;

        return triggerCollider;
    }

    private bool CanBeTrigger(Collider candidate)
    {
        if (!candidate)
            return false;

        MeshCollider meshCollider = candidate as MeshCollider;
        return !meshCollider || meshCollider.convex;
    }

    private Rigidbody GetSphereRigidbody(Collider other)
    {
        Rigidbody body = other.attachedRigidbody;
        if (!body || !IsAcceptedSphere(body))
            return null;

        return body;
    }

    private bool IsAcceptedSphere(Rigidbody body)
    {
        if (!body)
            return false;

        if (acceptedSpheres != null && acceptedSpheres.Length > 0)
        {
            foreach (Rigidbody acceptedSphere in acceptedSpheres)
            {
                if (acceptedSphere == body)
                    return true;
            }

            return false;
        }

        if (controller)
            return controller.IsAcceptedSphere(body);

        return true;
    }

    private void RefreshState()
    {
        bool hasWeight = HasWeight;
        bool hasSphere = HasSphere;

        if (hasWeight != hadWeight)
        {
            if (hasWeight)
                onWeightPressed?.Invoke();
            else
                onWeightReleased?.Invoke();
        }

        if (hasSphere != hadSphere)
        {
            if (hasSphere)
                onSpherePlaced?.Invoke();
            else
                onSphereRemoved?.Invoke();
        }

        hadWeight = hasWeight;
        hadSphere = hasSphere;

        if (controller)
            controller.SocketStateChanged(this);
    }

    private void UpdateVisual()
    {
        if (!socketVisual)
            return;

        Vector3 targetPosition = visualStartLocalPosition + (HasWeight ? pressedLocalOffset : Vector3.zero);
        socketVisual.localPosition = Vector3.Lerp(socketVisual.localPosition, targetPosition, Time.deltaTime * visualMoveSpeed);
    }
}
