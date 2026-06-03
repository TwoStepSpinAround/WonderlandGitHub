using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class FlickeringLights : MonoBehaviour
{
    [Tooltip("Lights to flicker. If empty, this GameObject's Light is used if present.")]
    public Light[] lights;

    [Tooltip("Tag used to auto-find lights when none are assigned.")]
    public string autoLightTag = "ElevatorLamp";

    [Tooltip("Flicker when the scene starts.")]
    public bool flickerOnSceneStart = true;

    [Tooltip("How long the start flicker lasts.")]
    public float sceneStartFlickerDuration = 1.2f;

    [Tooltip("If true, start the scene in darkness first, then flicker in.")]
    public bool startSceneDark = true;

    [Tooltip("How long to stay fully dark at scene start before flickering.")]
    public float sceneStartDarkHold = 0.25f;

    [Tooltip("Duration of each pre-scene-change flicker/shake burst.")]
    public float preChangeBurstDuration = 1f;

    [Tooltip("Minimum number of pre-scene-change bursts.")]
    public int minPreChangeBursts = 2;

    [Tooltip("Maximum number of pre-scene-change bursts.")]
    public int maxPreChangeBursts = 3;

    [Tooltip("Minimum interval between bursts.")]
    public float minBurstInterval = 0.35f;

    [Tooltip("Maximum interval between bursts.")]
    public float maxBurstInterval = 0.8f;

    [Tooltip("Optional camera shake component to mimic elevator movement while lights flicker.")]
    public CameraShake cameraShake;

    [Tooltip("Camera tag used when auto-finding CameraShake.")]
    public string cameraTag = "MainCamera";

    [Tooltip("If true, use CameraShake default values (duration input + default magnitude).")]
    public bool useCameraShakeDefaults = true;

    [Tooltip("Shake strength during scene-start flicker.")]
    public float startShakeMagnitude = 0.035f;

    [Tooltip("Shake strength during pre-scene-change flicker.")]
    public float preChangeShakeMagnitude = 0.05f;

    [Tooltip("How long before scene load lights are forced fully off to hide the cut.")]
    public float preLoadBlackoutDuration = 0.2f;

    [Tooltip("Minimum delay between flicker toggles.")]
    public float minToggleInterval = 0.03f;

    [Tooltip("Maximum delay between flicker toggles.")]
    public float maxToggleInterval = 0.12f;

    private Coroutine sequenceRoutine;
    private bool[] initialEnabledStates;
    private bool warnedMissingShake = false;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureLightsResolved();
    }

    private void Start()
    {
        if (flickerOnSceneStart)
            StartFlickerSequence(SceneStartFlickerRoutine());
    }

    public void PlayBeforeSceneChange(float secondsUntilSceneLoad)
    {
        EnsureLightsResolved(false);

        float blackout = Mathf.Max(0f, preLoadBlackoutDuration);
        float burstDuration = Mathf.Max(0.01f, preChangeBurstDuration);
        int minBursts = Mathf.Max(1, minPreChangeBursts);
        int maxBursts = Mathf.Max(minBursts, maxPreChangeBursts);
        int burstCount = Random.Range(minBursts, maxBursts + 1);

        float availableBeforeBlackout = Mathf.Max(0f, secondsUntilSceneLoad - blackout);
        int maxBurstsThatFit = Mathf.Max(1, Mathf.FloorToInt(availableBeforeBlackout / burstDuration));
        burstCount = Mathf.Min(burstCount, maxBurstsThatFit);

        float intervalsTotal = Mathf.Max(0f, availableBeforeBlackout - (burstCount * burstDuration));
        float waitBeforeSequence = 0f;

        if (burstCount > 1)
        {
            float minTotalIntervals = (burstCount - 1) * Mathf.Max(0f, minBurstInterval);
            float maxTotalIntervals = (burstCount - 1) * Mathf.Max(minBurstInterval, maxBurstInterval);
            float targetIntervals = Mathf.Clamp(intervalsTotal, minTotalIntervals, maxTotalIntervals);
            waitBeforeSequence = Mathf.Max(0f, availableBeforeBlackout - (burstCount * burstDuration) - targetIntervals);
        }
        else
        {
            waitBeforeSequence = Mathf.Max(0f, availableBeforeBlackout - burstDuration);
        }

        StartFlickerSequence(PlayBeforeSceneChangeRoutine(waitBeforeSequence, burstCount, burstDuration, blackout));
    }

    public void PlayFlicker(float duration)
    {
        EnsureLightsResolved(false);
        StartFlickerSequence(FlickerRoutine(Mathf.Max(0f, duration)));
    }

    public void PlayForDurationInIntervals(float totalDuration)
    {
        EnsureLightsResolved(false);
        StartFlickerSequence(IntervalEffectRoutine(Mathf.Max(0f, totalDuration)));
    }

    private void StartFlickerSequence(IEnumerator routine)
    {
        if (sequenceRoutine != null)
            StopCoroutine(sequenceRoutine);

        sequenceRoutine = StartCoroutine(RunSequence(routine));
    }

    private IEnumerator RunSequence(IEnumerator routine)
    {
        if (routine != null)
            yield return routine;

        sequenceRoutine = null;
    }

    private IEnumerator SceneStartFlickerRoutine()
    {
        if (startSceneDark)
            SetAllLights(false);

        if (sceneStartDarkHold > 0f)
            yield return new WaitForSeconds(sceneStartDarkHold);

        TriggerShake(sceneStartFlickerDuration, startShakeMagnitude);
        yield return FlickerRoutine(sceneStartFlickerDuration);
    }

    private IEnumerator PlayBeforeSceneChangeRoutine(float delay, int burstCount, float burstDuration, float blackoutDuration)
    {
        yield return new WaitForSeconds(delay);

        for (int burstIndex = 0; burstIndex < burstCount; burstIndex++)
        {
            TriggerShake(burstDuration, preChangeShakeMagnitude);
            yield return FlickerRoutine(burstDuration);

            if (burstIndex < burstCount - 1)
                yield return new WaitForSeconds(Random.Range(minBurstInterval, maxBurstInterval));
        }

        SetAllLights(false);

        if (blackoutDuration > 0f)
            yield return new WaitForSeconds(blackoutDuration);
    }

    private IEnumerator IntervalEffectRoutine(float totalDuration)
    {
        float remaining = totalDuration;
        float burstDuration = Mathf.Max(0.01f, preChangeBurstDuration);

        while (remaining > 0f)
        {
            float thisBurst = Mathf.Min(burstDuration, remaining);

            TriggerShake(thisBurst, preChangeShakeMagnitude);
            yield return FlickerRoutine(thisBurst);
            remaining -= thisBurst;

            if (remaining <= 0f)
                break;

            float pause = Random.Range(minBurstInterval, maxBurstInterval);
            pause = Mathf.Min(Mathf.Max(0f, pause), remaining);
            if (pause > 0f)
                yield return new WaitForSeconds(pause);

            remaining -= pause;
        }
    }

    private IEnumerator FlickerRoutine(float duration)
    {
        if (lights == null || lights.Length == 0)
            yield break;

        float endTime = Time.time + duration;

        while (Time.time < endTime)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                    lights[i].enabled = !lights[i].enabled;
            }

            yield return new WaitForSeconds(Random.Range(minToggleInterval, maxToggleInterval));
        }

        RestoreInitialStates();
    }

    private void ResolveLights()
    {
        if (HasValidLights())
            return;

        if (!string.IsNullOrEmpty(autoLightTag))
        {
            Light[] allLights = FindObjectsOfType<Light>(true);
            if (allLights != null && allLights.Length > 0)
            {
                List<Light> foundLights = new List<Light>();
                for (int i = 0; i < allLights.Length; i++)
                {
                    Light currentLight = allLights[i];
                    if (currentLight == null)
                        continue;

                    GameObject lightObject = currentLight.gameObject;
                    if (lightObject == null)
                        continue;

                    if (lightObject.tag == autoLightTag)
                        foundLights.Add(currentLight);
                }

                if (foundLights.Count > 0)
                {
                    lights = foundLights.ToArray();
                    return;
                }
            }
        }

        Light single = GetComponent<Light>();
        if (single != null)
            lights = new[] { single };
    }

    private void EnsureLightsResolved()
    {
        EnsureLightsResolved(true);
    }

    private void EnsureLightsResolved(bool refreshInitialStates)
    {
        int previousLength = lights != null ? lights.Length : 0;
        ResolveLights();

        if (refreshInitialStates || initialEnabledStates == null || (lights != null && initialEnabledStates.Length != lights.Length) || previousLength != (lights != null ? lights.Length : 0))
            CacheInitialStates();
    }

    private bool HasValidLights()
    {
        if (lights == null || lights.Length == 0)
            return false;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                return true;
        }

        return false;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        lights = null;
        EnsureLightsResolved();
    }

    private void CacheInitialStates()
    {
        if (lights == null)
            return;

        initialEnabledStates = new bool[lights.Length];
        for (int i = 0; i < lights.Length; i++)
            initialEnabledStates[i] = lights[i] != null && lights[i].enabled;
    }

    private void RestoreInitialStates()
    {
        if (lights == null || initialEnabledStates == null)
            return;

        for (int i = 0; i < lights.Length && i < initialEnabledStates.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = initialEnabledStates[i];
        }
    }

    private void SetAllLights(bool enabled)
    {
        if (lights == null)
            return;

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
                lights[i].enabled = enabled;
        }
    }

    private void TriggerShake(float duration, float customMagnitude)
    {
        CameraShake shake = ResolveCameraShake();
        if (shake == null)
            return;

        if (useCameraShakeDefaults)
            shake.PlayShake(duration);
        else
            shake.PlayShake(duration, customMagnitude);
    }

    private CameraShake ResolveCameraShake()
    {
        if (cameraShake != null)
            return cameraShake;

        Camera mainCam = Camera.main;
        if (mainCam != null)
            cameraShake = mainCam.GetComponent<CameraShake>();

        if (cameraShake == null)
        {
            Camera[] cameras = FindObjectsOfType<Camera>(true);
            for (int i = 0; i < cameras.Length; i++)
            {
                if (cameras[i] != null && cameras[i].CompareTag(cameraTag))
                {
                    cameraShake = cameras[i].GetComponent<CameraShake>();
                    if (cameraShake != null)
                        break;
                }
            }
        }

        if (cameraShake == null)
            cameraShake = FindObjectOfType<CameraShake>(true);

        if (cameraShake == null && !warnedMissingShake)
        {
            warnedMissingShake = true;
            Debug.LogWarning("FlickeringLights: no CameraShake assigned/found on Camera.main. Assign CameraShake in inspector for flicker shake.", this);
        }

        return cameraShake;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;

        if (sequenceRoutine != null)
        {
            StopCoroutine(sequenceRoutine);
            sequenceRoutine = null;
        }

        RestoreInitialStates();
    }
}
