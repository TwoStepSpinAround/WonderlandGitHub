using UnityEngine;

public class MenuManager : MonoBehaviour
{
    private void Awake()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Time.timeScale = 1f;
    }

    public void RestartButton()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL");
            return;
        }
        GameManager.Instance.RestartGame();
    }
    public void PlayButton()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL");
            return;
        }

        GameManager.Instance.StartGame();
    }
}