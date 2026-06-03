using UnityEngine;
using System.Collections.Generic;

public class ChaosFlashingLights : MonoBehaviour
{
    [Header("Flicker")]
    [SerializeField] private bool flickeringEnabled = true;
    [SerializeField, Min(0.1f)] private float flickerSpeed = 12f;
    [SerializeField, Range(0f, 1f)] private float toggleProbability = 0.8f;

    [Header("Rotation")]
    [SerializeField] private bool randomChildRotationEnabled = true;
    [SerializeField, Min(0f)] private float minRotationSpeed = 20f;
    [SerializeField, Min(0f)] private float maxRotationSpeed = 120f;
    [SerializeField, Min(0f)] private float directionChangeRate = 0.25f;

    private readonly List<GameObject> childTargets = new List<GameObject>();
    private readonly List<Vector3> childRotationAxes = new List<Vector3>();
    private readonly List<float> childRotationSpeeds = new List<float>();
    private Coroutine flickerRoutine;
    private bool previousFlickeringState;

    private void Awake()
    {
        CacheChildren();
        previousFlickeringState = flickeringEnabled;
    }

    private void OnEnable()
    {
        flickerRoutine = StartCoroutine(FlickerLoop());
    }

    private void OnDisable()
    {
        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            flickerRoutine = null;
        }

        SetAllChildrenActive(true);
    }

    private void Update()
    {
        if (previousFlickeringState != flickeringEnabled)
        {
            previousFlickeringState = flickeringEnabled;

            if (!flickeringEnabled)
            {
                SetAllChildrenActive(true);
            }
        }

        UpdateChildrenRotation();
    }

    private System.Collections.IEnumerator FlickerLoop()
    {
        float toggleAccumulator = 0f;

        while (true)
        {
            if (!flickeringEnabled)
            {
                toggleAccumulator = 0f;
                yield return null;
                continue;
            }

            if (childTargets.Count == 0)
            {
                CacheChildren();
                toggleAccumulator = 0f;
                yield return null;
                continue;
            }

            // Treat flickerSpeed as toggle attempts per second for immediate tuning feedback.
            float attemptsPerSecond = Mathf.Max(0.1f, flickerSpeed);
            toggleAccumulator += Time.deltaTime * attemptsPerSecond;
            int attemptsThisFrame = Mathf.FloorToInt(toggleAccumulator);

            if (attemptsThisFrame > 0)
            {
                toggleAccumulator -= attemptsThisFrame;
                attemptsThisFrame = Mathf.Min(attemptsThisFrame, 8);
            }

            for (int attempt = 0; attempt < attemptsThisFrame; attempt++)
            {
                if (Random.value > toggleProbability)
                {
                    continue;
                }

                int index = Random.Range(0, childTargets.Count);
                GameObject target = childTargets[index];
                if (target != null)
                {
                    target.SetActive(!target.activeSelf);
                }
            }

            yield return null;
        }
    }

    private void CacheChildren()
    {
        childTargets.Clear();
        childRotationAxes.Clear();
        childRotationSpeeds.Clear();

        Transform[] allTransforms = GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform t = allTransforms[i];
            if (t == transform)
            {
                continue;
            }

            childTargets.Add(t.gameObject);
            AssignRandomRotation(i: childTargets.Count - 1);
        }
    }

    private void UpdateChildrenRotation()
    {
        if (!randomChildRotationEnabled)
        {
            return;
        }

        if (childTargets.Count == 0)
        {
            CacheChildren();
            return;
        }

        if (childRotationAxes.Count != childTargets.Count || childRotationSpeeds.Count != childTargets.Count)
        {
            CacheChildren();
            return;
        }

        float deltaTime = Time.deltaTime;
        float changeChance = directionChangeRate * deltaTime;

        for (int i = 0; i < childTargets.Count; i++)
        {
            GameObject child = childTargets[i];
            if (child == null)
            {
                continue;
            }

            child.transform.Rotate(childRotationAxes[i], childRotationSpeeds[i] * deltaTime, Space.Self);

            if (Random.value < changeChance)
            {
                AssignRandomRotation(i);
            }
        }
    }

    private void AssignRandomRotation(int i)
    {
        if (i < 0 || i >= childTargets.Count)
        {
            return;
        }

        while (childRotationAxes.Count <= i)
        {
            childRotationAxes.Add(Vector3.up);
        }

        while (childRotationSpeeds.Count <= i)
        {
            childRotationSpeeds.Add(0f);
        }

        childRotationAxes[i] = Random.onUnitSphere;

        float minSpeed = Mathf.Min(minRotationSpeed, maxRotationSpeed);
        float maxSpeed = Mathf.Max(minRotationSpeed, maxRotationSpeed);
        float speed = Random.Range(minSpeed, maxSpeed);
        childRotationSpeeds[i] = Random.value < 0.5f ? speed : -speed;
    }

    private void SetAllChildrenActive(bool isActive)
    {
        for (int i = 0; i < childTargets.Count; i++)
        {
            if (childTargets[i] != null)
            {
                childTargets[i].SetActive(isActive);
            }
        }
    }
}
