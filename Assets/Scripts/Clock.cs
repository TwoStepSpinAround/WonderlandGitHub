using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Clock : MonoBehaviour
{
    [System.Serializable]
    private struct SceneSpeedEntry
    {
        public int sceneBuildIndex;
        public float speedPercent;
    }

    [Header("UI")]
    [SerializeField] private TMP_Text timeText;

    [Header("Start Time")]
    [SerializeField, Range(0, 23)] private int defaultHour = 8;
    [SerializeField, Range(0, 59)] private int defaultMinute = 0;

    [Header("Speed")]
    [Tooltip("Fallback speed in percent when the active scene is not in the scene speed list.")]
    [SerializeField] private float initialSpeedPercent = 100f;
    [Tooltip("Per-scene speed overrides in percent. 100 = normal, -100 = normal backwards.")]
    [SerializeField] private SceneSpeedEntry[] sceneSpeeds;

    private const float SecondsPerMinute = 60f;
    private const float SecondsPerDay = 24f * 60f * 60f;

    private float currentTimeSeconds;
    private float timeSpeedPercent;

    private void Awake()
    {
        if (timeText == null)
        {
            timeText = GetComponent<TMP_Text>();
        }

        SetTime(defaultHour, defaultMinute);
        ApplySpeedForScene(SceneManager.GetActiveScene().buildIndex);
        RefreshText();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void Update()
    {
        float speedMultiplier = timeSpeedPercent / 100f;
        currentTimeSeconds = Mathf.Repeat(currentTimeSeconds + (Time.deltaTime * speedMultiplier), SecondsPerDay);

        RefreshText();
    }

    public void SetTimeSpeedPercent(float newSpeedPercent)
    {
        timeSpeedPercent = newSpeedPercent;
    }

    public void AddTimeSpeedPercent(float deltaPercent)
    {
        timeSpeedPercent += deltaPercent;
    }

    public void SetTime(int hour, int minute)
    {
        hour = Mathf.Clamp(hour, 0, 23);
        minute = Mathf.Clamp(minute, 0, 59);
        currentTimeSeconds = (hour * 60f + minute) * SecondsPerMinute;
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySpeedForScene(scene.buildIndex);
    }

    private void ApplySpeedForScene(int sceneBuildIndex)
    {
        timeSpeedPercent = initialSpeedPercent;

        if (sceneSpeeds == null || sceneSpeeds.Length == 0)
        {
            return;
        }

        for (int i = 0; i < sceneSpeeds.Length; i++)
        {
            if (sceneSpeeds[i].sceneBuildIndex == sceneBuildIndex)
            {
                timeSpeedPercent = sceneSpeeds[i].speedPercent;
                return;
            }
        }
    }

    private void RefreshText()
    {
        if (timeText == null)
        {
            return;
        }

        int totalMinutes = Mathf.FloorToInt(currentTimeSeconds / SecondsPerMinute);
        int hours = (totalMinutes / 60) % 24;
        int minutes = totalMinutes % 60;

        timeText.text = string.Format("{0:00}:{1:00}", hours, minutes);
    }
}
