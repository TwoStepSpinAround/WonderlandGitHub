using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections;


public class ElevatorFloorController : MonoBehaviour
{
    [SerializeField] private TMP_Text floorText;

    private void Awake()
    {
        UpdateFloorTextAfterDelay();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateFloorTextAfterDelay();
    }

    private void UpdateFloorTextAfterDelay()
    {
        ResetFloorText();
        StartCoroutine(AfterSeconds(1f));
    }

    private void ResetFloorText()
    {
        floorText.text = "";
    }

    private IEnumerator AfterSeconds(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        int sceneIndex = SceneManager.GetActiveScene().buildIndex;

        if (sceneIndex == 0)
            floorText.text = "0";
        else if (sceneIndex == 3)
            floorText.text = "3";
        else if (sceneIndex == 4)
            floorText.text = "-3";
        else
            floorText.text = (-sceneIndex).ToString();
        
    }
}