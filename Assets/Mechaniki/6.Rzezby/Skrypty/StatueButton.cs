using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StatueButton : MonoBehaviour
{
    [Header("Cel")]
    public StatueStation statue;

    [Header("Interakcja")]
    public bool requirePlayerTag = false;
    public string playerTag = "Player";
    public KeyCode interactionKey = KeyCode.E;
    public string prompt = "E - nacisnij przycisk";
    public Transform player;
    public Camera interactionCamera;
    public float interactionDistance = 2f;
    public bool allowMouseClick = true;

    [Header("Wizualne wcisniecie")]
    public Transform buttonVisual;
    public Vector3 pressedLocalOffset = new Vector3(0f, -0.05f, 0f);
    public float visualMoveSpeed = 10f;

    private bool playerInRange;
    private bool pressed;
    private bool promptVisible;
    private Vector3 visualStartLocalPosition;

    private void Reset()
    {
        Collider buttonCollider = GetComponent<Collider>();
        buttonCollider.isTrigger = true;

        statue = GetComponentInParent<StatueStation>();
        buttonVisual = transform;
    }

    private void Awake()
    {
        Collider buttonCollider = GetComponent<Collider>();
        buttonCollider.isTrigger = true;

        if (!statue)
            statue = GetComponentInParent<StatueStation>();

        if (!buttonVisual)
            buttonVisual = transform;

        FindInteractorIfNeeded();

        visualStartLocalPosition = buttonVisual.localPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (pressed || !IsAllowedInteractor(other))
            return;

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsAllowedInteractor(other))
            return;

        playerInRange = false;
    }

    private void Update()
    {
        bool canInteract = !pressed && IsInInteractionRange();

        SetPromptVisible(canInteract);

        if (canInteract && Input.GetKeyDown(interactionKey))
            Press();

        UpdateVisual();
    }

    private void OnMouseDown()
    {
        if (allowMouseClick)
            Press();
    }

    public void Press()
    {
        if (pressed)
            return;

        pressed = true;
        playerInRange = false;
        SetPromptVisible(false);

        if (statue)
            statue.PressButton();
    }

    private bool IsAllowedInteractor(Collider other)
    {
        return !requirePlayerTag || other.CompareTag(playerTag);
    }

    private bool IsInInteractionRange()
    {
        if (playerInRange)
            return true;

        Transform interactor = GetInteractorTransform();
        if (!interactor)
            return false;

        float maxDistance = Mathf.Max(0.01f, interactionDistance);
        return Vector3.SqrMagnitude(interactor.position - transform.position) <= maxDistance * maxDistance;
    }

    private Transform GetInteractorTransform()
    {
        if (player)
            return player;

        FindInteractorIfNeeded();

        if (player)
            return player;

        return null;
    }

    private void FindInteractorIfNeeded()
    {
        if (!player)
        {
            CharacterController character = FindObjectOfType<CharacterController>();
            if (character)
                player = character.transform;
        }

        if (!interactionCamera)
            interactionCamera = Camera.main;

        if (!player && interactionCamera)
        {
            player = interactionCamera.transform;
        }
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptVisible == visible)
            return;

        promptVisible = visible;

        if (visible)
            InteractionPromptUI.Instance?.Show(prompt);
        else
            InteractionPromptUI.Instance?.Hide();
    }

    private void UpdateVisual()
    {
        if (!buttonVisual)
            return;

        Vector3 targetPosition = visualStartLocalPosition + (pressed ? pressedLocalOffset : Vector3.zero);
        buttonVisual.localPosition = Vector3.Lerp(buttonVisual.localPosition, targetPosition, Time.deltaTime * visualMoveSpeed);
    }
}
