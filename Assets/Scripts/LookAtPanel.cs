using UnityEngine;

public class LookAtPanel : MonoBehaviour
{
    [Tooltip("Panel interaction camera (normal Unity Camera).")]
    public Camera interactCamera;

    [Tooltip("The default gameplay render camera (standard Unity Camera).")]
    public Camera mainCamera;

    [Tooltip("How far the player must be to interact with the panel.")]
    public float interactionDistance = 2f;

    [Tooltip("Optional reference to the player transform. If null, uses GameObject tagged 'Player'.")]
    public Transform player;

    [Tooltip("Optional LevelDecider used for door control.")]
    public LevelDecider levelDecider;

    [Tooltip("If true, close doors when panel camera is activated.")]
    public bool closeDoorsOnActivate = true;

    [Tooltip("If true, force the main camera to be live when play starts.")]
    public bool forceMainCameraOnStart = true;

    [Tooltip("If true, temporarily disable player controls while panel camera is active.")]
    public bool disablePlayerControlsInPanelMode = true;

    [Tooltip("If true, disable player colliders while panel mode is active.")]
    public bool disablePlayerColliderInPanelMode = true;

    [Tooltip("If true, disable the entire player GameObject while panel mode is active.")]
    public bool deactivatePlayerInPanelMode = false;

    private bool isActive = false;
    private FPMovementController movementController;
    private CameraController lookController;
    private Rigidbody playerRigidbody;
    private Collider[] playerColliders;
    private bool[] originalColliderStates;
    private Canvas[] panelCanvases;
    private bool playerWasActiveBeforePanel;

    private void Start()
    {
        if (interactCamera == null)
            Debug.LogWarning("LookAtPanel: interactCamera is not assigned", this);

        ResolveMainCameraReference();

        if (mainCamera == null)
            Debug.LogWarning("LookAtPanel: mainCamera is not assigned and no camera tagged 'MainCamera' was found", this);

        if (forceMainCameraOnStart)
            SetCameraMode(false);

        panelCanvases = GetComponentsInChildren<Canvas>(true);
        ResolvePlayerReference();

        SetCursorForPanelMode(false);
    }

    private void Update()
    {
        if (player == null)
            ResolvePlayerReference();

        if (mainCamera == null)
            ResolveMainCameraReference();

        if (player == null || interactCamera == null || mainCamera == null)
            return;

        float dist = Vector3.Distance(transform.position, player.position);
        if (dist <= interactionDistance && Input.GetKeyDown(KeyCode.E))
            ToggleCamera();
    }

    private void ToggleCamera()
    {
        isActive = !isActive;
        SetCameraMode(isActive);

        if (isActive)
        {
            SetCursorForPanelMode(true);

            if (disablePlayerControlsInPanelMode && !deactivatePlayerInPanelMode)
                SetPlayerControlsEnabled(false);

            if (deactivatePlayerInPanelMode)
                SetPlayerObjectActive(false);
            else
                SetPlayerColliderEnabled(false);

            AlignPlayerYawToPanelCamera();

            if (closeDoorsOnActivate && levelDecider != null)
                levelDecider.CloseDoors();
        }
        else
        {
            SetCursorForPanelMode(false);

            if (deactivatePlayerInPanelMode)
                SetPlayerObjectActive(true);
            else
                SetPlayerColliderEnabled(true);

            AlignPlayerYawToPanelCamera();

            if (disablePlayerControlsInPanelMode && !deactivatePlayerInPanelMode)
                SetPlayerControlsEnabled(true);
        }
    }

    private void SetCameraMode(bool panelMode)
    {
        if (mainCamera == null)
            ResolveMainCameraReference();

        if (panelMode)
            EnsurePanelCanvasEventCamera();

        if (mainCamera != null)
            mainCamera.enabled = !panelMode;

        if (interactCamera != null)
            interactCamera.enabled = panelMode;

        SetAudioListenerEnabled(mainCamera, !panelMode);
        SetAudioListenerEnabled(interactCamera, panelMode);
    }

    private void EnsurePanelCanvasEventCamera()
    {
        if (interactCamera == null || panelCanvases == null)
            return;

        for (int i = 0; i < panelCanvases.Length; i++)
        {
            var canvas = panelCanvases[i];
            if (canvas == null)
                continue;

            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                canvas.worldCamera = interactCamera;
            }
        }
    }

    private void SetAudioListenerEnabled(Camera cam, bool enabled)
    {
        if (cam == null)
            return;

        var listener = cam.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = enabled;
    }

    private void AlignPlayerYawToPanelCamera()
    {
        if (player == null || interactCamera == null)
            return;

        float targetYaw = interactCamera.transform.eulerAngles.y;

        Vector3 playerEuler = player.eulerAngles;
        playerEuler.y = targetYaw;
        player.eulerAngles = playerEuler;

        if (lookController != null)
            lookController.SetYaw(targetYaw);
    }

    private void ResolveMainCameraReference()
    {
        if (mainCamera != null)
            return;

        mainCamera = Camera.main;
        if (mainCamera != null)
            return;

        Camera[] cameras = FindObjectsOfType<Camera>(true);
        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].CompareTag("MainCamera"))
            {
                mainCamera = cameras[i];
                return;
            }
        }
    }

    private void ResolvePlayerReference()
    {
        if (player == null)
            player = SceneLoad.PersistedPlayerTransform;

        if (player == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go != null)
                player = go.transform;
        }

        if (player == null)
            return;

        movementController = player.GetComponent<FPMovementController>();
        lookController = player.GetComponentInChildren<CameraController>(true);
        playerRigidbody = player.GetComponent<Rigidbody>();

        if (disablePlayerColliderInPanelMode)
        {
            playerColliders = player.GetComponentsInChildren<Collider>(true);
            originalColliderStates = new bool[playerColliders.Length];
            for (int i = 0; i < playerColliders.Length; i++)
                originalColliderStates[i] = playerColliders[i] != null && playerColliders[i].enabled;
        }
    }

    private void SetPlayerControlsEnabled(bool enabled)
    {
        if (movementController != null)
            movementController.enabled = enabled;

        if (!enabled && playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }
    }

    private void SetPlayerColliderEnabled(bool enabled)
    {
        if (!disablePlayerColliderInPanelMode || playerColliders == null || originalColliderStates == null)
            return;

        for (int i = 0; i < playerColliders.Length; i++)
        {
            var col = playerColliders[i];
            if (col == null)
                continue;

            col.enabled = enabled ? originalColliderStates[i] : false;
        }
    }

    private void SetPlayerObjectActive(bool active)
    {
        if (player == null)
            return;

        if (!active)
            playerWasActiveBeforePanel = player.gameObject.activeSelf;

        if (active && !playerWasActiveBeforePanel)
            return;

        player.gameObject.SetActive(active);
    }

    private void SetCursorForPanelMode(bool panelMode)
    {
        CursorController.panelOpen = panelMode;
        Cursor.visible = panelMode;
        Cursor.lockState = panelMode ? CursorLockMode.None : CursorLockMode.Locked;
    }

    /// <summary>
    /// Called by other managers (e.g. <see cref="LevelDecider"/>) when the
    /// system should return to the normal main camera, regardless of the
    /// current toggle state.  This simply ensures the view is reset and the
    /// cursor hidden.
    /// </summary>
    public void ResetView()
    {
        if (isActive)
            ToggleCamera();
    }

    private void OnDisable()
    {
        SetCursorForPanelMode(false);

        if (deactivatePlayerInPanelMode)
            SetPlayerObjectActive(true);
        else
            SetPlayerColliderEnabled(true);

        if (disablePlayerControlsInPanelMode && !deactivatePlayerInPanelMode)
            SetPlayerControlsEnabled(true);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}
