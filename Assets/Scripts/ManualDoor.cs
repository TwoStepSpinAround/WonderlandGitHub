using UnityEngine;

public class ManualDoor : MonoBehaviour
{
    [Header("Interaction")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;

    [Header("Door Rotation")]
    [SerializeField] private float openAngle = 70f;
    [SerializeField] private float rotateSpeed = 120f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen;
    private string replayId;
    private bool hasWarnedMissingReplayId;

    private void Awake()
    {
        closedRotation = transform.localRotation;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle, rotationAxis.normalized);
        RefreshReplayId();
    }

    private void Start()
    {
        TryAutoAssignPlayer();
    }

    private void Update()
    {
        if (player == null)
        {
            TryAutoAssignPlayer();
        }

        if (player != null && Input.GetKeyDown(interactKey))
        {
            float distance = Vector3.Distance(player.position, transform.position);
            if (distance <= interactDistance)
            {
                isOpen = !isOpen;

                if (string.IsNullOrWhiteSpace(replayId))
                    RefreshReplayId();

                GhostReplayController.RecordManualDoorState(replayId, isOpen);
            }
        }

        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        transform.localRotation = Quaternion.RotateTowards(
            transform.localRotation,
            targetRotation,
            rotateSpeed * Time.deltaTime);
    }

    public void SetDoorOpenStateFromReplay(bool open)
    {
        isOpen = open;
    }

    private void TryAutoAssignPlayer()
    {
        if (player != null || string.IsNullOrWhiteSpace(playerTag))
            return;

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void RefreshReplayId()
    {
        ReplayIdentity identity = GetComponent<ReplayIdentity>();
        if (identity == null)
            identity = GetComponentInParent<ReplayIdentity>();
        if (identity == null)
            identity = GetComponentInChildren<ReplayIdentity>(true);

        replayId = identity != null ? identity.Id : string.Empty;

        if (!hasWarnedMissingReplayId && string.IsNullOrWhiteSpace(replayId))
        {
            hasWarnedMissingReplayId = true;
            Debug.LogWarning($"ManualDoor '{name}' has no ReplayIdentity with a valid Id. Ghost replay for this door will be skipped.", this);
        }
    }
}
