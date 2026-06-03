using UnityEngine;

public class KillPlayer : MonoBehaviour
{
    void Start()
    {
        string playerTagToFind = "Player";
        Transform[] allTransforms = FindObjectsOfType<Transform>(true);
        
        foreach (Transform t in allTransforms)
        {
            if (t.CompareTag(playerTagToFind))
            {
                Destroy(t.gameObject);
            }
        }    
    }
}
