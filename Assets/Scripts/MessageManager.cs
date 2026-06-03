using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MessageManager : MonoBehaviour
{
    [System.Serializable]
    private class MessageData
    {
        [SerializeField, Min(0)] private int delaySeconds;
        [SerializeField] private GameObject[] activateObjects;
        [SerializeField] private GameObject[] deactivateObjects;
        [SerializeField, Tooltip("Build index of level to auto-deliver this message on. -1 = never auto-deliver")]
        private int levelDelivery = -1;
        [SerializeField] private bool notification = true;

        public int DelaySeconds => Mathf.Max(0, delaySeconds);
        public GameObject[] ActivateObjects => activateObjects;
        public GameObject[] DeactivateObjects => deactivateObjects;
        public int LevelDelivery => levelDelivery;
        public bool Notification => notification;
    }

    [Header("Message Sound")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip messageSound;

    [Header("Messages")]
    [SerializeField] private MessageData[] messages;

    private bool[] delivered;
    private bool[] delivering;

    [SerializeField] private GameObject NotificationsPanel;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        delivered = new bool[messages.Length];
        delivering = new bool[messages.Length];
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;

        for (int i = 0; i < delivering.Length; i++)
        {
            if (!delivered[i])
            {
                delivering[i] = false;
            }
        }

        CheckCurrentSceneForMessages();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        NotificationsPanel.SetActive(false);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckCurrentSceneForMessages();
    }

    private void CheckCurrentSceneForMessages()
    {
        int currentScene = SceneManager.GetActiveScene().buildIndex;

        for (int i = 0; i < messages.Length; i++)
        {
            if (delivered[i] || delivering[i])
                continue;

            if (messages[i].LevelDelivery == currentScene)
            {
                StartCoroutine(DeliverMessage(i));
            }
        }
    }

    private IEnumerator DeliverMessage(int index)
    {
        delivering[index] = true;

        MessageData message = messages[index];

        yield return new WaitForSeconds(message.DelaySeconds);

        SetObjectsActive(message.ActivateObjects, true);
        SetObjectsActive(message.DeactivateObjects, false);
        
        if (message.Notification)
        {
            PlayMessageSound();
            NotificationsPanel.SetActive(true);
            StartCoroutine(HideNotificationsPanelAfterDelay(2f));
        }
       
        Debug.Log($"Message {index + 1} delivered.");

        
        delivered[index] = true;
        delivering[index] = false;
    }

    private static void SetObjectsActive(GameObject[] objects, bool active)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                obj.SetActive(active);
            }
        }
    }

    public void PlayMessageSound()
    {
        if (audioSource == null || messageSound == null)
            return;

        audioSource.PlayOneShot(messageSound);
    }

    private IEnumerator HideNotificationsPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        NotificationsPanel.SetActive(false);
    }
}