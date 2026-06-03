using UnityEngine;

public class ReplayIdentity : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

    private void OnValidate()
    {
        if (id == null)
            return;

        id = id.Trim();
    }
}
