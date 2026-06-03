using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelDecider : MonoBehaviour
{
    [Tooltip("Delay after camera switch before door closing starts.")]
    public float preTransitionDelay = 1f;

    [Tooltip("How long to run flicker+shake (with breaks) after loading the new scene.")]
    public float postLoadEffectDuration = 6f;

    [Tooltip("Maximum time to wait for doors to reach closed state before forcing scene load.")]
    public float maxDoorCloseWait = 4f;

    [Tooltip("Extra safety time to keep auto-open blocked during transition.")]
    public float transitionAutoOpenBuffer = 1f;

    [Tooltip("Reference to the automatic doors controller that should be closed when a button is pressed.")]
    public AutomaticDoors doorsController;

    [Tooltip("Optional panel that should be notified when the camera is reset.")]
    public LookAtPanel panelToNotify;

    [Tooltip("Optional flicker controller to play just before scene switch.")]
    public FlickeringLights flickeringLights;

    private static bool pendingPostLoadSequence;
    private bool runPostLoadSequenceInThisScene;

    private void Awake()
    {
        runPostLoadSequenceInThisScene = pendingPostLoadSequence;
        pendingPostLoadSequence = false;
    }

    private void Start()
    {
        if (runPostLoadSequenceInThisScene)
            StartCoroutine(PostLoadSequence());
    }

    // NOTE: the original OnElevatorButton methods are retained for
    // backwards-compatibility but are no longer used by the current panel-based
    // UI.  Only the OnPanelButton handlers are actively referenced in the
    // elevator panel.

    /// <summary>
    /// Called by panel UI buttons.  Whenever a floor is chosen we close the
    /// doors, switch the camera, and defer scene loading until the doors have
    /// finished closing.
    /// </summary>
    public void OnPanelButton(int sceneBuildIndex)
    {
        //debug
        Debug.Log("OnPanelButton called with sceneBuildIndex: " + sceneBuildIndex);
        if (sceneBuildIndex >= 0 && sceneBuildIndex < SceneManager.sceneCountInBuildSettings)
            StartCoroutine(HandlePanelButtonAndLoad(() => SceneManager.LoadScene(sceneBuildIndex)));
    }

    public void OnPanelButton(string sceneName)
    {
        //debug
        Debug.Log("OnPanelButton called with sceneName: " + sceneName);
        if (!string.IsNullOrEmpty(sceneName))
            StartCoroutine(HandlePanelButtonAndLoad(() => SceneManager.LoadScene(sceneName)));
    }




    // public helpers for external callers (panels, other scripts)
    public void CloseDoors()
    {
        if (doorsController != null)
            doorsController.Close();

        // Play scene audio from GameManager when doors close
        if (GameManager.Instance != null)
            GameManager.Instance.PlayRandomSceneAudio();
    }

    public void OpenDoors()
    {
        if (doorsController != null)
            doorsController.Open();
    }

    private void ReturnToMainCamera()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null)
            return;

        // make sure only the tagged main camera is enabled
        foreach (var cam in Camera.allCameras)
            cam.enabled = (cam == mainCam);
    }

    private IEnumerator HandlePanelButtonAndLoad(System.Action loadSceneAction)
    {
        if (panelToNotify != null)
            panelToNotify.ResetView();

        ReturnToMainCamera();

        if (preTransitionDelay > 0f)
            yield return new WaitForSeconds(preTransitionDelay);

        SceneLoad sceneLoad = FindObjectOfType<SceneLoad>();
        if (sceneLoad != null)
            sceneLoad.CaptureRelativePoseNow();

        if (doorsController != null)
        {
            doorsController.DelayAutoOpenFor(preTransitionDelay + maxDoorCloseWait + postLoadEffectDuration + transitionAutoOpenBuffer);
            CloseDoors();
        }

        if (doorsController != null)
            yield return WaitForDoorsClosed();

        pendingPostLoadSequence = true;
        loadSceneAction?.Invoke();
    }

    private IEnumerator WaitForDoorsClosed()
    {
        float timeoutAt = Time.time + Mathf.Max(0f, maxDoorCloseWait);

        while (doorsController != null && !doorsController.IsClosed && Time.time < timeoutAt)
            yield return null;
    }

    private IEnumerator PostLoadSequence()
    {
        if (doorsController != null)
        {
            CloseDoors();
            doorsController.DelayAutoOpenFor(postLoadEffectDuration + 0.25f);
        }

        if (flickeringLights != null)
            flickeringLights.PlayForDurationInIntervals(postLoadEffectDuration);

        yield return new WaitForSeconds(Mathf.Max(0f, postLoadEffectDuration));

        if (doorsController != null)
            OpenDoors();
    }

}
