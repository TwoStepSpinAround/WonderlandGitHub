using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Audio for Death/Reload")] 
    public AudioSource reloadAudioSource;
    public AudioClip reloadClip;
    public bool Cheats = false;

    [Header("Scene Load Audio")] 
    public AudioSource sceneAudioSource;
    public AudioClip[] sceneAudioClips;

    public float timer = 0f;

    public float highscoreTime;

    [SerializeField] private int startScene = 0; // Default to first playable scene

    private int lastScene = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Cheats)
        CheckForStart();
    }

    void CheckForStart()
    {
        if (lastScene == -1) // No valid last scene, start new game
        {
            lastScene = startScene; // Set to first playable scene
            SceneManager.LoadScene(8);
        }
    }
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }


    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex < 7 && scene.buildIndex >= 0) // Only update lastScene for valid playable scenes
        {
            lastScene = scene.buildIndex;
        }

        // Removed PlayRandomSceneAudio from here
        Debug.Log($"[GameManager] Entered scene: {scene.name}");
        StartCoroutine(DelayedSceneLoadLogic(scene, mode));
    }

    //used after introscene!
    public void BeginNewGame()
    {
        SceneManager.LoadScene(7);
    }

    public void PlayRandomSceneAudio()
    {
        if (sceneAudioSource != null && sceneAudioClips != null && sceneAudioClips.Length > 0)
        {
            foreach (AudioClip clip in sceneAudioClips)
            {
                sceneAudioSource.PlayOneShot(clip);
            }
        }
    }

    private IEnumerator DelayedSceneLoadLogic(Scene scene, LoadSceneMode mode)
    {
        yield return null; // Wait one frame to ensure all objects are initialized
        SceneLoad sceneLoad = FindObjectOfType<SceneLoad>();
        if (sceneLoad != null)
        {
            sceneLoad.HandleSceneLoaded(scene, mode);
        }
        else
        {
            Debug.LogWarning("[GameManager] No SceneLoad instance found to handle scene load.");
        }
    }

    public void StartGame()
    {
        PlayRandomSceneAudio();
        // Find player tag
        string playerTagToFind = "Player";

        SceneLoad sceneLoad = FindObjectOfType<SceneLoad>();
        if (sceneLoad != null)
            playerTagToFind = sceneLoad.playerTag;

        // Find active OR inactive player, including DontDestroyOnLoad objects
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);

        GameObject playerObj = null;

        foreach (Transform t in allTransforms)
        {
            if (t.CompareTag(playerTagToFind))
            {
                playerObj = t.gameObject;
                break;
            }
        }


        if (playerObj != null)
        {
            playerObj.SetActive(true);
            Debug.Log($"Enabled player: {playerObj.name}");
            SceneManager.LoadScene(lastScene);
        }
        else
        {
            SceneManager.LoadScene(startScene); // Always load first playable scene if lastScene is invalid
            Debug.LogWarning($"No player object found with tag '{playerTagToFind}'");
        }
    }

    public void Death()
    {
        StartCoroutine(DeathEffects());
    }

    private IEnumerator DeathEffects()
    {
        // Play death sound
        if (reloadAudioSource != null)
        {
            if (reloadClip != null)
                reloadAudioSource.PlayOneShot(reloadClip);
            else
                reloadAudioSource.Play();
        }
        // Wait for the sound to finish (or a fixed time if no clip)
        float waitTime = reloadClip != null ? reloadClip.length : 1f;
        yield return new WaitForSeconds(waitTime);
        // Load death scene
        SceneManager.LoadScene(7);
    }

    public void RestartGame()
    {
        if (SceneLoad.PersistedPlayer != null)
        {
            Destroy(SceneLoad.PersistedPlayer);
        }

        timer = 0f;

        SceneManager.LoadScene(0);
    }

    void Update()
    {
        if(SceneManager.GetActiveScene().buildIndex != 4 && SceneManager.GetActiveScene().buildIndex < 7)
            timer += Time.deltaTime;
        else
        {
            if (timer > highscoreTime)
                highscoreTime = timer;
        }

        if (Input.GetKeyDown(KeyCode.P))
        {
            SceneManager.LoadScene(7);
        }
    }
}