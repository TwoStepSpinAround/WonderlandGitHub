using TMPro;
using UnityEngine;

public class Highscore : MonoBehaviour
{
    [SerializeField] private TMP_Text yourTime;
    [SerializeField] private TMP_Text yourBest;
    [SerializeField] private TMP_Text developersBest;

    private void Start()
    {
        PlayerPrefs.SetFloat("YourTime", GameManager.Instance.timer);
        PlayerPrefs.SetFloat("YourBest", GameManager.Instance.highscoreTime);
        PlayerPrefs.SetFloat("DevelopersBest", 300f);
        yourTime.text = $"Your time: {PlayerPrefs.GetFloat("YourTime", 0f)}";
        yourBest.text = $"Your best time: {PlayerPrefs.GetFloat("YourBest", 0f)}";
        developersBest.text = $"Developer's best time: {PlayerPrefs.GetFloat("DevelopersBest", 300f)}";
        PlayerPrefs.Save();    
    }
}