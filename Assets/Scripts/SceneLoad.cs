using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoad : MonoBehaviour
{
    [Tooltip("Optional player object to persist across scenes. If null, this GameObject is used.")]
    public GameObject player;

    [Tooltip("Tag used to find the player on scene load.")]
    public string playerTag = "Player";

    [Tooltip("Tag used to find the elevator on scene load.")]
    public string elevatorTag = "Elevator";

    [Tooltip("If true, player's world scale is restored relative to elevator scale across scene loads.")]
    public bool persistScaleRelativeToElevator = true;

    private static GameObject persistedPlayer;

    public static GameObject PersistedPlayer
    {
        get { return persistedPlayer; }
    }
    private static Vector3 persistedLocalPositionToElevator;
    private static Quaternion persistedLocalRotationToElevator = Quaternion.identity;
    private static Vector3 persistedScaleRatioToElevator = Vector3.one;
    private static bool hasPersistedLocalPose;

    public static Transform PersistedPlayerTransform
    {
        get
        {
            return persistedPlayer != null ? persistedPlayer.transform : null;
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Awake()
    {
        GameObject target = player != null ? player : gameObject;

        if (persistedPlayer == null)
        {
            persistedPlayer = target;
            DontDestroyOnLoad(persistedPlayer);
        }

        if (persistedPlayer != target)
        {
            Destroy(target);
            return;
        }

    }

    private void LateUpdate()
    {
        CachePlayerPoseRelativeToElevator();
    }

    public void HandleSceneLoaded(Scene scene, LoadSceneMode mode)

    {
        // Reset persisted pose on new scene load to avoid using previous scene's elevator
        hasPersistedLocalPose = false;
        Transform playerTransform = ResolvePlayerTransform();
        if (playerTransform == null)
        {
            Debug.LogWarning($"SceneLoad: no GameObject with tag '{playerTag}' found in scene '{scene.name}'.");
            return;
        }

        // Disable the entire player GameObject before moving
        GameObject playerGO = playerTransform.gameObject;
        bool wasActive = playerGO.activeSelf;
        if (wasActive)
        {
            Debug.Log("[SceneLoad] Disabling player GameObject before moving");
            playerGO.SetActive(false);
        }

        GameObject elevator = ResolveElevatorObject();
        if (elevator == null)
        {
            Debug.LogWarning($"SceneLoad: no GameObject with tag '{elevatorTag}' found in scene '{scene.name}'.");
            return;
        }

        Collider col = elevator.GetComponent<Collider>();

        CharacterController controller = playerTransform.GetComponent<CharacterController>();
        if (controller != null)
            controller.enabled = false;

        Rigidbody body = playerTransform.GetComponent<Rigidbody>();
        bool hadRigidbody = body != null;
        bool previousIsKinematic = false;
        bool previousUseGravity = false;
        if (hadRigidbody)
        {
            previousIsKinematic = body.isKinematic;
            previousUseGravity = body.useGravity;
            body.isKinematic = true;
            body.useGravity = false;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
        }

        Transform elevatorTransform = elevator.transform;
        // Try to find a child with tag 'ElevatorTarget'
        Transform spawnPoint = null;
        foreach (Transform child in elevatorTransform)
        {
            if (child.CompareTag("ElevatorTarget"))
            {
                spawnPoint = child;
                break;
            }
        }

        if (spawnPoint != null)
        {
            Debug.Log($"[SceneLoad] Moving player to elevator spawn point: {spawnPoint.position}, rotation: {spawnPoint.rotation.eulerAngles}");
            playerTransform.position = spawnPoint.position;
            playerTransform.rotation = spawnPoint.rotation;
        }
        else if (hasPersistedLocalPose)
        {
            Vector3 targetPos = elevatorTransform.TransformPoint(persistedLocalPositionToElevator);
            Quaternion targetRot = elevatorTransform.rotation * persistedLocalRotationToElevator;
            Debug.Log($"[SceneLoad] Moving player to persisted local pose: {targetPos}, rotation: {targetRot.eulerAngles}");
            playerTransform.position = targetPos;
            playerTransform.rotation = targetRot;

            if (persistScaleRelativeToElevator)
            {
                Vector3 targetWorldScale = Vector3.Scale(elevatorTransform.lossyScale, persistedScaleRatioToElevator);
                ApplyWorldScale(playerTransform, targetWorldScale);
            }
        }
        else
        {
            // Use collider center if available, else fallback to transform.position
            Vector3 elevatorCenter = elevatorTransform.position;
            if (col != null)
                elevatorCenter = col.bounds.center;
            Debug.Log($"[SceneLoad] Moving player to elevator center: {elevatorCenter}, rotation: {elevatorTransform.rotation.eulerAngles}");
            playerTransform.position = elevatorCenter;
            playerTransform.rotation = elevatorTransform.rotation;
        }

        if (hadRigidbody)
        {
            body.position = playerTransform.position;
            body.rotation = playerTransform.rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.useGravity = previousUseGravity;
            body.isKinematic = previousIsKinematic;
        }

        if (controller != null)
            controller.enabled = true;

        // Re-enable the player GameObject after moving
        if (!playerGO.activeSelf && wasActive)
        {
            Debug.Log("[SceneLoad] Re-enabling player GameObject after moving");
            playerGO.SetActive(true);
        }

        CachePlayerPoseRelativeToElevator();
    }

    public void CaptureRelativePoseNow()
    {
        CachePlayerPoseRelativeToElevator();
    }

    private Transform ResolvePlayerTransform()
    {
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject taggedPlayer = GameObject.FindGameObjectWithTag(playerTag);
            if (taggedPlayer != null)
                return taggedPlayer.transform;
        }

        return PersistedPlayerTransform;
    }

    private GameObject ResolveElevatorObject()
    {
        if (!string.IsNullOrEmpty(elevatorTag))
        {
            GameObject elevator = GameObject.FindGameObjectWithTag(elevatorTag);
            if (elevator != null)
                return elevator;
        }

        return GameObject.FindGameObjectWithTag("elevator");
    }

    private void CachePlayerPoseRelativeToElevator()
    {
        Transform playerTransform = ResolvePlayerTransform();
        if (playerTransform == null)
            return;

        GameObject elevator = ResolveElevatorObject();
        if (elevator == null)
            return;

        Transform elevatorTransform = elevator.transform;
        persistedLocalPositionToElevator = elevatorTransform.InverseTransformPoint(playerTransform.position);
        persistedLocalRotationToElevator = Quaternion.Inverse(elevatorTransform.rotation) * playerTransform.rotation;
        persistedScaleRatioToElevator = DivideComponentsSafe(playerTransform.lossyScale, elevatorTransform.lossyScale);
        hasPersistedLocalPose = true;
    }

    private static Vector3 DivideComponentsSafe(Vector3 numerator, Vector3 denominator)
    {
        return new Vector3(
            Mathf.Approximately(denominator.x, 0f) ? 1f : numerator.x / denominator.x,
            Mathf.Approximately(denominator.y, 0f) ? 1f : numerator.y / denominator.y,
            Mathf.Approximately(denominator.z, 0f) ? 1f : numerator.z / denominator.z);
    }

    private static void ApplyWorldScale(Transform target, Vector3 targetWorldScale)
    {
        if (target == null)
            return;

        Transform parent = target.parent;
        if (parent == null)
        {
            target.localScale = targetWorldScale;
            return;
        }

        Vector3 parentScale = parent.lossyScale;
        target.localScale = DivideComponentsSafe(targetWorldScale, parentScale);
    }
}
