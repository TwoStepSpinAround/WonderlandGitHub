using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChaosManager : MonoBehaviour
{
    [System.Serializable]
    public class TimedGroup
    {
        public List<GameObject> targets = new List<GameObject>();
        [Min(0f)] public float waitBeforeEnable = 0f;
        [Min(0f)] public float waitBeforeDisable = 1f;
    }

    [SerializeField] private List<TimedGroup> timedGroups = new List<TimedGroup>();

    private readonly List<Coroutine> activeRoutines = new List<Coroutine>();

    private void OnEnable()
    {
        StartSequence();
    }

    private void OnDisable()
    {
        StopSequence();
    }

    public void StartSequence()
    {
        StopSequence();

        for (int i = 0; i < timedGroups.Count; i++)
        {
            TimedGroup group = timedGroups[i];
            if (group != null)
            {
                activeRoutines.Add(StartCoroutine(RunGroup(group)));
            }
        }
    }

    public void StopSequence()
    {
        for (int i = 0; i < activeRoutines.Count; i++)
        {
            if (activeRoutines[i] != null)
            {
                StopCoroutine(activeRoutines[i]);
            }
        }

        activeRoutines.Clear();

        for (int i = 0; i < timedGroups.Count; i++)
        {
            SetGroupState(timedGroups[i], false);
        }
    }

    private IEnumerator RunGroup(TimedGroup group)
    {
        if (group == null)
        {
            yield break;
        }

        if (group.waitBeforeEnable > 0f)
        {
            yield return new WaitForSeconds(group.waitBeforeEnable);
        }

        SetGroupState(group, true);

        if (group.waitBeforeDisable > 0f)
        {
            yield return new WaitForSeconds(group.waitBeforeDisable);
        }

        SetGroupState(group, false);
    }

    private static void SetGroupState(TimedGroup group, bool enabled)
    {
        if (group == null || group.targets == null)
        {
            return;
        }

        for (int i = 0; i < group.targets.Count; i++)
        {
            GameObject target = group.targets[i];
            if (target != null)
            {
                target.SetActive(enabled);
            }
        }
    }
}
