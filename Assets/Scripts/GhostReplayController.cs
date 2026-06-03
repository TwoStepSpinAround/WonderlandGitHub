using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GhostReplayController : MonoBehaviour
{
    public enum ReplayActionType
    {
        FlashlightSet,
        DoorSetOpen,
        ManualDoorSetOpen
    }

    private struct ReplayFrame
    {
        public float time;
        public Vector3 position;
        public Quaternion rotation;
    }

    private struct ReplayAction
    {
        public float time;
        public ReplayActionType type;
        public string targetId;
        public bool boolValue;
    }

    private static readonly List<ReplayFrame> RecordedFrames = new List<ReplayFrame>(2048);
    private static readonly List<ReplayAction> RecordedActions = new List<ReplayAction>(256);
    private static bool hasRecording;
    private static GhostReplayController activeRecorder;
    private static bool isPlaybackExecuting;

    [Header("References")]
    [SerializeField] private Transform rootToRecord;
    [SerializeField] private GameObject ghostModelPrefab;

    [Header("Recording")]
    [SerializeField] private bool persistAcrossScenes = true;
    [SerializeField] private string recordSceneName = "Level 2";
    [SerializeField, Min(1f)] private float samplesPerSecond = 15f;
    [SerializeField] private string playerTag = "Player";

    [Header("Playback")]
    [SerializeField] private string playbackSceneName = "Level 6";
    [SerializeField, Min(0f)] private float playbackSpawnDelaySeconds = 10f;
    [SerializeField] private string ghostTagOnSpawn = "Ghost";
    [SerializeField, Min(0.1f)] private float playbackSpeed = 1f;
    [SerializeField] private bool loopPlayback;

    [Header("Action Playback")]
    [SerializeField] private string ghostFlashlightChildName = "Flashlight";

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private bool isRecording;
    private float recordElapsed;
    private float nextSampleTime;
    private bool missingPlayerLogged;
    private Coroutine playbackRoutine;
    private Coroutine delayedSpawnRoutine;
    private GameObject activeGhost;
    private readonly Dictionary<string, AutomaticDoors> doorTargetsById = new Dictionary<string, AutomaticDoors>();
    private readonly Dictionary<string, ManualDoor> manualDoorTargetsById = new Dictionary<string, ManualDoor>();

    private void Awake()
    {
        Debug.Log("GhostReplayController: Awake called");
        if (activeRecorder != null && activeRecorder != this)
        {
            Debug.LogWarning("GhostReplayController: Duplicate instance detected. Destroying newer instance to preserve recording state.", this);
            Destroy(gameObject);
            return;
        }

        activeRecorder = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(gameObject);
        }

        RefreshRecordedRoot(true);
    }

    private void OnDestroy()
    {
        Debug.Log("GhostReplayController: OnDestroy called");
        if (activeRecorder == this)
        {
            activeRecorder = null;
        }
    }

    private void OnEnable()
    {
        Debug.Log("GhostReplayController: OnEnable called");
        SceneManager.sceneLoaded += OnSceneLoaded;
        RefreshRecordedRoot(true);

        if (SceneManager.GetActiveScene().name == recordSceneName)
        {
            float existingDuration = 0f;
            if (RecordedFrames.Count > 0)
            {
                existingDuration = RecordedFrames[RecordedFrames.Count - 1].time;
            }

            if (!hasRecording || existingDuration < 2f)
            {
                Log($"OnEnable: starting new recording (existingDuration={existingDuration:F2}s)");
                BeginRecording();
            }
            else
            {
                Log($"OnEnable: preserving existing recording (duration={existingDuration:F2}s)");
            }
        }
    }

    private void OnDisable()
    {
        Debug.Log("GhostReplayController: OnDisable called");
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        if (rootToRecord == null)
        {
            RefreshRecordedRoot(false);
        }

        if (!isRecording || rootToRecord == null)
        {
            return;
        }

        recordElapsed += Time.deltaTime;
        while (recordElapsed >= nextSampleTime)
        {
            AddFrame(nextSampleTime, rootToRecord.position, rootToRecord.rotation);
            nextSampleTime += 1f / samplesPerSecond;
        }
    }

    public void BeginRecording()
    {
        if (isRecording)
        {
            Debug.Log("BeginRecording called but already recording; ignoring.");
            return;
        }

        Debug.Log("GhostReplayController: BeginRecording()");
        RefreshRecordedRoot(true);
        RecordedFrames.Clear();
        RecordedActions.Clear();
        hasRecording = false;

        isRecording = true;
        recordElapsed = 0f;
        nextSampleTime = 0f;

        if (rootToRecord != null)
        {
            AddFrame(0f, rootToRecord.position, rootToRecord.rotation);
            nextSampleTime = 1f / samplesPerSecond;
        }

        Log($"Recording started in '{SceneManager.GetActiveScene().name}' at {samplesPerSecond} samples/sec.");
    }

    public void StopRecording()
    {
        isRecording = false;
        hasRecording = RecordedFrames.Count > 0 || RecordedActions.Count > 0;

        int flashlightActions = GetRecordedActionCount(ReplayActionType.FlashlightSet);
        int autoDoorActions = GetRecordedActionCount(ReplayActionType.DoorSetOpen);
        int manualDoorActions = GetRecordedActionCount(ReplayActionType.ManualDoorSetOpen);

        Log(
            $"Recording stopped. Frames: {RecordedFrames.Count}, Actions: {RecordedActions.Count} " +
            $"(Flashlight: {flashlightActions}, AutoDoors: {autoDoorActions}, ManualDoors: {manualDoorActions}). " +
            $"Has recording: {hasRecording}."
        );
        

        if (RecordedActions.Count == 0)
        {
            Log(
                "No replay actions were captured. Check: (1) the door actually changed state while recording, " +
                "(2) AutomaticDoors initialAutoOpenDelay did not block opening, and " +
                "(3) replayed doors have ReplayIdentity with a non-empty Id."
            );
        }
        
        if (activeGhost != null && SceneManager.GetActiveScene().name != playbackSceneName)
        {
            Destroy(activeGhost);
            activeGhost = null;
        }

    }

    public void SpawnGhostNow()
    {
        if (isRecording)
        {
            Log("Spawn deferred: recording still in progress.");
            return;
        }

        if (activeGhost != null)
        {
            Log("Spawn skipped: ghost is already active.");
            return;
        }

        try
        {
            if (!string.IsNullOrEmpty(ghostTagOnSpawn) && GameObject.FindGameObjectWithTag(ghostTagOnSpawn) != null)
            {
                Log($"Spawn skipped: a GameObject with tag '{ghostTagOnSpawn}' already exists in the scene.");
                return;
            }
        }
        catch (UnityException)
        {
            // Tag may not exist yet; ignore and continue.
        }

        if (!hasRecording || RecordedFrames.Count == 0)
        {
            Log("Spawn skipped: no recording available.");
            return;
        }

        if (ghostModelPrefab == null)
        {
            Log("Spawn skipped: ghostModelPrefab is not assigned.");
            return;
        }

        if (playbackRoutine != null)
        {
            Debug.Log("GhostReplayController: Stopping existing playbackRoutine before spawning new ghost");
            StopCoroutine(playbackRoutine);
            playbackRoutine = null;
        }

        if (activeGhost != null)
        {
            Destroy(activeGhost);
            activeGhost = null;
        }

        ReplayFrame first = RecordedFrames[0];
        Log($"Attempting to instantiate ghost at position: {first.position}, rotation: {first.rotation.eulerAngles}");
        activeGhost = Instantiate(ghostModelPrefab, first.position, first.rotation);

        if (activeGhost == null)
        {
            Log("Instantiate returned null! Ghost was not created.");
            return;
        }
        else
        {
            Log($"Ghost instantiated: {activeGhost.name}, active: {activeGhost.activeSelf}, layer: {activeGhost.layer}");
        }

        if (!activeGhost.activeSelf)
        {
            activeGhost.SetActive(true);
            Log("Spawned ghost was inactive and has been enabled.");
        }

        if (!string.IsNullOrEmpty(ghostTagOnSpawn))
        {
            try
            {
                activeGhost.tag = ghostTagOnSpawn;
            }
            catch (UnityException)
            {
                Debug.LogWarning($"GhostReplayController: Tag '{ghostTagOnSpawn}' does not exist. Create it in Project Settings > Tags and Layers.", this);
            }
        }

        EnsureGhostTriggerPhysics(activeGhost);
        BuildDoorTargetMap();
        SetGhostFlashlightState(GetInitialFlashlightStateFromRecording());
        ApplyInitialActionStates();

        playbackRoutine = StartCoroutine(PlayGhostRoutine(activeGhost.transform));
        Log($"Ghost spawned. Playback started with {RecordedFrames.Count} frames and {RecordedActions.Count} actions at speed {playbackSpeed}.");
    }

    private void Start()
    {
        HandleSceneRules(SceneManager.GetActiveScene());

        // Force ghost spawn and playback immediately in Level 6
        if (SceneManager.GetActiveScene().name == "Level 6")
        {
            SpawnGhostNow();
        }
    }

    public void ClearRecording()
    {
        isRecording = false;
        hasRecording = false;
        RecordedFrames.Clear();
        RecordedActions.Clear();
        Log("Recording cleared.");
    }

    public static void RecordFlashlightState(bool isOn)
    {
        TryRecordAction(ReplayActionType.FlashlightSet, string.Empty, isOn);
    }

    public static void RecordDoorState(string targetId, bool isOpen)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            if (activeRecorder != null && activeRecorder.enableDebugLogs)
            {
                Debug.Log("[GhostReplay] Door action skipped while recording: missing ReplayIdentity Id.");
            }
            return;
        }

        TryRecordAction(ReplayActionType.DoorSetOpen, targetId, isOpen);
    }

    public static void RecordManualDoorState(string targetId, bool isOpen)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            if (activeRecorder != null && activeRecorder.enableDebugLogs)
            {
                Debug.Log("[GhostReplay] Manual door action skipped while recording: missing ReplayIdentity Id.");
            }
            return;
        }

        TryRecordAction(ReplayActionType.ManualDoorSetOpen, targetId, isOpen);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Log($"Scene loaded: {scene.name}");
        RefreshRecordedRoot(true);
        HandleSceneRules(scene);
    }

    private void HandleSceneRules(Scene scene)
    {
        if (delayedSpawnRoutine != null)
        {
            StopCoroutine(delayedSpawnRoutine);
            delayedSpawnRoutine = null;
        }

        bool isRecordScene = !string.IsNullOrEmpty(recordSceneName) && scene.name == recordSceneName;
        bool isPlaybackScene = !string.IsNullOrEmpty(playbackSceneName) && scene.name == playbackSceneName;

        if (isRecordScene)
        {
            Log($"Entered record scene '{recordSceneName}'.");
            BeginRecording();
            return;
        }

        if (isRecording)
        {
            Log($"Left record scene '{recordSceneName}'.");
            StopRecording();
        }

        if (isPlaybackScene && SceneManager.GetActiveScene().buildIndex == 6)
        {
            SpawnGhostNow();
            return;
            Log($"Entered playback scene '{playbackSceneName}'. Starting {playbackSpawnDelaySeconds:0.##}s countdown.");
            delayedSpawnRoutine = StartCoroutine(SpawnGhostAfterDelay(playbackSpawnDelaySeconds));
        }
    }

    private IEnumerator SpawnGhostAfterDelay(float delaySeconds)
    {
        float remaining = Mathf.Max(0f, delaySeconds);
        int previousShownSecond = -1;

        while (remaining > 0f)
        {
            int shownSecond = Mathf.CeilToInt(remaining);
            if (shownSecond != previousShownSecond)
            {
                previousShownSecond = shownSecond;
                Log($"Playback countdown: {shownSecond}s");
            }

            remaining -= Time.deltaTime;
            yield return null;
        }

        SpawnGhostNow();
        delayedSpawnRoutine = null;
    }

    private IEnumerator PlayGhostRoutine(Transform ghostTransform)
    {
        Debug.Log($"PlayGhostRoutine started with ghostTransform: {ghostTransform}, RecordedFrames: {RecordedFrames.Count}, RecordedActions: {RecordedActions.Count}");
        if (ghostTransform == null || RecordedFrames.Count == 0)
        {
            yield break;
        }

        // Diagnostic: log the final recorded time so we can tell if playback has any duration
        float _diagnosticFinalTime = RecordedFrames[RecordedFrames.Count - 1].time;
        Debug.Log($"PlayGhostRoutine diagnostic: finalTime={_diagnosticFinalTime:F3}, frames={RecordedFrames.Count}");

        isPlaybackExecuting = true;

        do
        {
            int _diagnosticFrameCounter = 0;
            float replayTime = 0f;
            int segmentIndex = 0;
            int actionIndex = 0;
            float finalTime = RecordedFrames[RecordedFrames.Count - 1].time;

            while (replayTime <= finalTime)
            {
                replayTime += Time.deltaTime * playbackSpeed;

                while (actionIndex < RecordedActions.Count && replayTime >= RecordedActions[actionIndex].time)
                {
                    ExecuteAction(RecordedActions[actionIndex]);
                    actionIndex++;
                }

                while (segmentIndex < RecordedFrames.Count - 2 && replayTime > RecordedFrames[segmentIndex + 1].time)
                {
                    segmentIndex++;
                }

                ReplayFrame a = RecordedFrames[segmentIndex];
                ReplayFrame b = RecordedFrames[Mathf.Min(segmentIndex + 1, RecordedFrames.Count - 1)];

                float duration = Mathf.Max(0.0001f, b.time - a.time);
                float t = Mathf.Clamp01((replayTime - a.time) / duration);


                ghostTransform.SetPositionAndRotation(
                    Vector3.LerpUnclamped(a.position, b.position, t),
                    Quaternion.SlerpUnclamped(a.rotation, b.rotation, t)
                );

                _diagnosticFrameCounter++;
                Debug.Log($"Ghost transformed #{_diagnosticFrameCounter} t={replayTime:F3} seg={segmentIndex} frameA.time={a.time:F3} frameB.time={b.time:F3}");

                yield return null;
            }
            Debug.Log($"PlayGhostRoutine finished playback loop framesLogged={_diagnosticFrameCounter}");
        }
        while (loopPlayback && ghostTransform != null);

        isPlaybackExecuting = false;
    }

    private static void AddFrame(float time, Vector3 position, Quaternion rotation)
    {
        RecordedFrames.Add(new ReplayFrame
        {
            time = time,
            position = position,
            rotation = rotation
        });

        hasRecording = true;
        Debug.Log($"AddFrame: time={time:F3} totalFrames={RecordedFrames.Count}");
    }

    private static void TryRecordAction(ReplayActionType type, string targetId, bool boolValue)
    {
        if (activeRecorder == null || !activeRecorder.isRecording || isPlaybackExecuting)
        {
            return;
        }

        RecordedActions.Add(new ReplayAction
        {
            time = activeRecorder.recordElapsed,
            type = type,
            targetId = targetId,
            boolValue = boolValue
        });

        hasRecording = true;
        Debug.Log($"TryRecordAction: type={type} time={activeRecorder.recordElapsed:F3} targetId={targetId} value={boolValue}");
    }

    private static int GetRecordedActionCount(ReplayActionType type)
    {
        int count = 0;
        for (int i = 0; i < RecordedActions.Count; i++)
        {
            if (RecordedActions[i].type == type)
            {
                count++;
            }
        }

        return count;
    }

    private void ExecuteAction(ReplayAction action)
    {
        Debug.Log($"ExecuteAction: type={action.type} time={action.time:F3} target={action.targetId} value={action.boolValue}");
        switch (action.type)
        {
            case ReplayActionType.FlashlightSet:
                SetGhostFlashlightState(action.boolValue);
                break;

            case ReplayActionType.DoorSetOpen:
                if (doorTargetsById.TryGetValue(action.targetId, out AutomaticDoors doors) && doors != null)
                {
                    if (action.boolValue)
                    {
                        doors.Open();
                    }
                    else
                    {
                        doors.Close();
                    }
                }
                else
                {
                    Log($"Door action skipped: no AutomaticDoors found for replay id '{action.targetId}'.");
                }
                break;

            case ReplayActionType.ManualDoorSetOpen:
                if (manualDoorTargetsById.TryGetValue(action.targetId, out ManualDoor manualDoor) && manualDoor != null)
                {
                    manualDoor.SetDoorOpenStateFromReplay(action.boolValue);
                }
                else
                {
                    Log($"Manual door action skipped: no ManualDoor found for replay id '{action.targetId}'.");
                }
                break;
        }
    }

    private void SetGhostFlashlightState(bool isOn)
    {
        if (activeGhost == null)
        {
            return;
        }

        GhostReplayBindings bindings = activeGhost.GetComponentInChildren<GhostReplayBindings>(true);
        if (bindings != null)
        {
            bindings.SetFlashlightState(isOn);
            return;
        }

        if (string.IsNullOrWhiteSpace(ghostFlashlightChildName))
        {
            return;
        }

        Transform flashlight = activeGhost.transform.Find(ghostFlashlightChildName);
        if (flashlight != null)
        {
            flashlight.gameObject.SetActive(isOn);
        }
    }

    private void BuildDoorTargetMap()
    {
        Debug.Log("BuildDoorTargetMap: scanning ReplayIdentity instances");
        doorTargetsById.Clear();
        manualDoorTargetsById.Clear();

        ReplayIdentity[] identities = FindObjectsOfType<ReplayIdentity>(true);
        for (int i = 0; i < identities.Length; i++)
        {
            ReplayIdentity identity = identities[i];
            if (identity == null || string.IsNullOrWhiteSpace(identity.Id))
            {
                continue;
            }

            AutomaticDoors doors = identity.GetComponent<AutomaticDoors>();
            if (doors == null)
            {
                doors = identity.GetComponentInParent<AutomaticDoors>();
            }
            if (doors == null)
            {
                doors = identity.GetComponentInChildren<AutomaticDoors>(true);
            }
            if (doors == null)
            {
                ManualDoor manualDoor = identity.GetComponent<ManualDoor>();
                if (manualDoor == null)
                {
                    manualDoor = identity.GetComponentInParent<ManualDoor>();
                }
                if (manualDoor == null)
                {
                    manualDoor = identity.GetComponentInChildren<ManualDoor>(true);
                }

                if (manualDoor == null)
                {
                    continue;
                }

                if (!manualDoorTargetsById.ContainsKey(identity.Id))
                {
                    manualDoorTargetsById.Add(identity.Id, manualDoor);
                }
                else
                {
                    Log($"Duplicate ReplayIdentity id '{identity.Id}' found for ManualDoor. Using first match.");
                }

                continue;
            }

            if (!doorTargetsById.ContainsKey(identity.Id))
            {
                doorTargetsById.Add(identity.Id, doors);
            }
            else
            {
                Log($"Duplicate ReplayIdentity id '{identity.Id}' found. Using first match.");
            }
        }
        Debug.Log($"BuildDoorTargetMap: found autoDoors={doorTargetsById.Count} manualDoors={manualDoorTargetsById.Count}");
    }

    private void ApplyInitialActionStates()
    {
        for (int i = 0; i < RecordedActions.Count; i++)
        {
            if (RecordedActions[i].time <= 0f)
            {
                ExecuteAction(RecordedActions[i]);
            }
        }
    }

    private bool GetInitialFlashlightStateFromRecording()
    {
        for (int i = 0; i < RecordedActions.Count; i++)
        {
            if (RecordedActions[i].type == ReplayActionType.FlashlightSet)
            {
                return RecordedActions[i].boolValue;
            }
        }

        return false;
    }

    private void Log(string message)
    {
        if (!enableDebugLogs)
            return;

        Debug.Log($"[GhostReplay] {message}");
    }

    private void RefreshRecordedRoot(bool forceSearch)
    {
        Transform playerTransform = FindPlayerTransformByTag();

        if (playerTransform != null && rootToRecord != playerTransform)
        {
            rootToRecord = playerTransform;
            missingPlayerLogged = false;
            Log($"Resolved player root from tag '{playerTag}': {rootToRecord.name}");
            return;
        }

        if (rootToRecord != null)
        {
            missingPlayerLogged = false;
            return;
        }

        if (forceSearch && !missingPlayerLogged)
        {
            Log($"Player with tag '{playerTag}' was not found yet. Will retry.");
            missingPlayerLogged = true;
        }
    }

    private Transform FindPlayerTransformByTag()
    {
        if (string.IsNullOrWhiteSpace(playerTag))
        {
            return null;
        }

        Transform[] allTransforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate != null && candidate.CompareTag(playerTag))
            {
                return candidate;
            }
        }

        return null;
    }

    private void EnsureGhostTriggerPhysics(GameObject ghostInstance)
    {
        if (ghostInstance == null)
            return;

        Rigidbody rb = ghostInstance.GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = ghostInstance.AddComponent<Rigidbody>();
            Log("Added Rigidbody to ghost for trigger interactions.");
        }

        rb.isKinematic = true;
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        Collider col = ghostInstance.GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"EnsureGhostTriggerPhysics: found Collider (isTrigger={col.isTrigger})");
        }
        else
        {
            Debug.Log("EnsureGhostTriggerPhysics: no Collider found on ghost instance");
        }
    }
}
