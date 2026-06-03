using UnityEngine;

public class DestroyAfterSeconds : MonoBehaviour
{
    [SerializeField, Min(0)] private float seconds = 2f;

    void Start()
    {
        Destroy(gameObject, seconds);
    }

}
