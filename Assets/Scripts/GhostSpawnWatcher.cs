using UnityEngine;
using UnityEngine.SceneManagement;

public class GhostSpawnWatcher : MonoBehaviour
{
    [SerializeField] private string playbackSceneName = "Level 6";
    [SerializeField] private float checkInterval = 1f;

    private float timer;

    private void Update()
    {
        if (SceneManager.GetActiveScene().name != playbackSceneName)
            return;

        timer += Time.deltaTime;

        if (timer < checkInterval)
            return;

        timer = 0f;

        GameObject ghost = GameObject.FindGameObjectWithTag("Ghost");

        if (ghost != null)
            return;

        Debug.Log("[GhostSpawnWatcher] No ghost found. Attempting spawn.");

        GhostReplayController controller =
            FindObjectOfType<GhostReplayController>();

        if (controller == null)
        {
            Debug.LogError(
                "[GhostSpawnWatcher] Could not find GhostReplayController."
            );
            return;
        }

        controller.SpawnGhostNow();
    }
}