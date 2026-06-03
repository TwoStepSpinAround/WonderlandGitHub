using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class AutomaticDoors : MonoBehaviour
{
    [System.Serializable]
    public class DoorEntry
    {
        public Transform doorTransform;
        public Transform openPosition;
        public Transform closedPosition;

        // world-space positions cached at start to avoid chasing moving targets
        [HideInInspector] public Vector3 openPosWorld;
        [HideInInspector] public Vector3 closedPosWorld;
    }

    public List<DoorEntry> doors = new List<DoorEntry>();

    public float moveSpeed = 1.2f;

    [Tooltip("How long doors should wait after scene load before they are allowed to auto-open from player proximity.")]
    public float initialAutoOpenDelay = 6f;

    public string playerTag = "Player";
    public string ghostTag = "Ghost";

    [Tooltip("When disabled, colliders tagged as ghost will not auto-open these doors. Useful when door state is driven by replay actions.")]
    [SerializeField] private bool allowGhostTriggerActivation = true;

    // internal state
    private bool shouldBeOpen = false;
    private float autoOpenAllowedAt;
    private string replayId;
    private bool hasWarnedMissingReplayId;

    /// <summary>
    /// True when all doors are currently at their closed positions and not
    /// actively moving; false while opening or closing.
    /// </summary>
    public bool IsClosed { get; private set; } = true;

    private void SetOpenState(bool open)
    {
        if (shouldBeOpen == open)
            return;

        shouldBeOpen = open;

        if (string.IsNullOrWhiteSpace(replayId))
            RefreshReplayId();

        GhostReplayController.RecordDoorState(replayId, shouldBeOpen);
    }

    // ------------------------------------------------------------------
    // public helpers called by other scripts (e.g. elevator buttons)
    // ------------------------------------------------------------------

    /// <summary>
    /// Force the doors open or closed immediately and ignore any trigger/collision
    /// events.  This can be used by external systems (like an elevator) to close
    /// the doors regardless of the player's position.
    /// </summary>
    /// <param name="open">true to open, false to close</param>
    public void ForceOpenState(bool open)
    {
        SetOpenState(open);
    }

    /// <summary>Shortcut for <see cref="ForceOpenState"/> with <c>false</c>.</summary>
    public void Close()
    {
        SetOpenState(false);
    }

    /// <summary>Shortcut for <see cref="ForceOpenState"/> with <c>true</c>.</summary>
    public void Open()
    {
        SetOpenState(true);
    }

    /// <summary>
    /// Prevents automatic player-triggered opening for the given duration.
    /// Useful when a transition sequence needs doors to stay shut temporarily.
    /// </summary>
    public void DelayAutoOpenFor(float seconds)
    {
        autoOpenAllowedAt = Mathf.Max(autoOpenAllowedAt, Time.time + Mathf.Max(0f, seconds));
    }

    private void Reset()
    {
        // ensure the collider is a trigger so OnTrigger events fire
        Collider col = GetComponent<Collider>();
        if (col != null)
            col.isTrigger = true;
    }

    private void Start()
    {
        RefreshReplayId();
        autoOpenAllowedAt = Time.time + Mathf.Max(0f, initialAutoOpenDelay);

        // sanity check references and cache the absolute positions at startup;
        // if the open/closed objects are children of the door they’ll move with it
        // otherwise we chase a moving world-space target and the door can overshoot.
        foreach (var entry in doors)
        {
            if (entry.doorTransform == null)
            {
                Debug.LogWarning("AutomaticDoors: an entry has no doorTransform assigned", this);
                continue;
            }
            if (entry.openPosition == null)
                Debug.LogWarning($"Door '{entry.doorTransform.name}' missing openPosition", entry.doorTransform);
            else
                entry.openPosWorld = entry.openPosition.position;

            if (entry.closedPosition == null)
                Debug.LogWarning($"Door '{entry.doorTransform.name}' missing closedPosition", entry.doorTransform);
            else
                entry.closedPosWorld = entry.closedPosition.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsActivator(other))
        {
            RequestOpenFromPlayer();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (IsActivator(other))
            RequestOpenFromPlayer();
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsActivator(other))
        {
            SetOpenState(false);
        }
    }

    // in case you want to use normal (non-trigger) physics collisions
    private void OnCollisionEnter(Collision collision)
    {
        if (IsActivator(collision.collider))
        {
            RequestOpenFromPlayer();
        }
    }

    private void OnCollisionStay(Collision collision)
    {
        if (IsActivator(collision.collider))
            RequestOpenFromPlayer();
    }

    private void OnCollisionExit(Collision collision)
    {
        if (IsActivator(collision.collider))
        {
            SetOpenState(false);
        }
    }

    private bool IsActivator(Collider other)
    {
        if (other == null)
            return false;

        Transform root = other.transform.root;

        if (!string.IsNullOrEmpty(playerTag) && root.CompareTag(playerTag))
            return true;

        if (allowGhostTriggerActivation && !string.IsNullOrEmpty(ghostTag) && root.CompareTag(ghostTag))
            return true;

        return false;
    }

    private void RequestOpenFromPlayer()
    {
        if (Time.time < autoOpenAllowedAt)
            return;

        SetOpenState(true);
    }

    private void Update()
    {
        bool allClosed = !shouldBeOpen;
        foreach (var entry in doors)
        {
            if (entry.doorTransform == null)
                continue;

            Vector3 targetPos = shouldBeOpen ? entry.openPosWorld : entry.closedPosWorld;
            entry.doorTransform.position = Vector3.MoveTowards(
                entry.doorTransform.position,
                targetPos,
                moveSpeed * Time.deltaTime);

            if (!shouldBeOpen && Vector3.Distance(entry.doorTransform.position, entry.closedPosWorld) > 0.001f)
                allClosed = false;
        }
        IsClosed = allClosed;
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
            Debug.LogWarning($"AutomaticDoors '{name}' has no ReplayIdentity with a valid Id. Ghost replay for this door will be skipped.", this);
        }
    }
}
