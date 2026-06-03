using UnityEngine;

public class PhoneToggle : MonoBehaviour
{
    [SerializeField] private PhoneManager phoneManager;
    [SerializeField] private KeyCode toggleKey = KeyCode.R;

    private void Awake()
    {
        ResolvePhoneManager();
    }

    private void ResolvePhoneManager()
    {
        if (phoneManager != null)
            return;

        phoneManager = FindFirstObjectByType<PhoneManager>(FindObjectsInactive.Include);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ResolvePhoneManager();

            if (phoneManager != null)
            {
                phoneManager.TogglePhone();
            }
        }
    }
}
