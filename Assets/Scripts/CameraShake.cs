using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    [Tooltip("Default shake duration in seconds.")]
    public float defaultDuration = 1f;

    [Tooltip("Default shake strength in degrees (Z-axis roll).")]
    public float defaultMagnitude = 1.5f;

    [Tooltip("How fast the roll shake oscillates.")]
    public float shakeSpeed = 35f;

    [Tooltip("How quickly shake fades out over its duration.")]
    public AnimationCurve fadeCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    private Coroutine shakeRoutine;
    private float lastAppliedRoll;

    public void PlayShake()
    {
        PlayShake(defaultDuration, defaultMagnitude);
    }

    public void PlayShake(float duration)
    {
        PlayShake(duration, defaultMagnitude);
    }

    public void PlayShake(float duration, float magnitude)
    {
        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeRoutine(Mathf.Max(0f, duration), Mathf.Max(0f, magnitude)));
    }

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        if (duration <= 0f || magnitude <= 0f)
        {
            RemoveLastRoll();
            yield break;
        }

        float phase = Random.Range(0f, Mathf.PI * 2f);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float fade = fadeCurve != null ? fadeCurve.Evaluate(t) : 1f - t;

            float oscillation = Mathf.Sin((elapsed * shakeSpeed) + phase);
            float nextRoll = oscillation * magnitude * fade;
            ApplyRoll(nextRoll);

            yield return null;
        }

        RemoveLastRoll();
        shakeRoutine = null;
    }

    private void ApplyRoll(float nextRoll)
    {
        Vector3 euler = transform.localEulerAngles;
        euler.z -= lastAppliedRoll;
        euler.z += nextRoll;
        transform.localEulerAngles = euler;
        lastAppliedRoll = nextRoll;
    }

    private void RemoveLastRoll()
    {
        Vector3 euler = transform.localEulerAngles;
        euler.z -= lastAppliedRoll;
        transform.localEulerAngles = euler;
        lastAppliedRoll = 0f;
    }

    private void OnDisable()
    {
        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        RemoveLastRoll();
    }
}
