using UnityEngine;

public class PhoneManager : MonoBehaviour
{
    [SerializeField] private GameObject phoneUIRoot;
    [SerializeField] private bool startPhoneOpen;
    [SerializeField] private bool lockCursorWhenPhoneCloses = true;

    [Header("Phone Pages")]
    [SerializeField] private GameObject flashlight;
    [SerializeField] private GameObject messages;
    [SerializeField] private GameObject mainMenu;
    [SerializeField] private GameObject pagesRoot;
    [SerializeField] private bool canOpenPhone = true;

    private bool isPhoneOpen;

    private void Awake()
    {
        if (phoneUIRoot == null)
        {
            phoneUIRoot = gameObject;
        }
    }

    private void Start()
    {
        SetPhoneState(startPhoneOpen && canOpenPhone, true);
    }

    public void SetCanOpenPhone(bool canOpen)
    {
        canOpenPhone = canOpen;

        if (!canOpenPhone)
        {
            SetPhoneState(false, true);
        }
    }

    public void TogglePhone()
    {
        SetPhoneState(!isPhoneOpen);
    }

    public void OpenPhone()
    {
        SetPhoneState(true);
    }

    public void ClosePhone()
    {
        SetPhoneState(false);
    }

    public void SetPhoneState(bool open, bool force = false)
    {
        if (open && !canOpenPhone)
            return;

        if (!force && isPhoneOpen == open)
            return;

        isPhoneOpen = open;

        if (open)
        {
            OpenOnlyMain();
            
            CursorController.panelOpen = true;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            return;
        }

        if (phoneUIRoot != null)
            phoneUIRoot.SetActive(false);

        CursorController.panelOpen = false;

        if (lockCursorWhenPhoneCloses)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void OpenOnlyMain()
    {
        if (phoneUIRoot != null)
            phoneUIRoot.SetActive(true);

        DisableDirectChildren(phoneUIRoot);

        if (mainMenu != null)
            mainMenu.SetActive(true);
    }

    public void Flashlight()
    {
        if (flashlight == null)
        {
            return;
        }

        flashlight.SetActive(!flashlight.activeSelf);
        GhostReplayController.RecordFlashlightState(flashlight.activeSelf);
    }

    public void Messages()
    {
        SetActivePage(messages);
    }

    public void Back()
    {
        if (pagesRoot != null)
        {
            Transform root = pagesRoot.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                root.GetChild(i).gameObject.SetActive(false);
            }
        }

        if (mainMenu != null)
        {
            mainMenu.SetActive(true);
        }
    }
/*
    public void ActivatePersonByNumber(int personNumber)
    {
        switch (personNumber)
        {
            case 1:
                SetActivePage(person1);
                break;
            case 2:
                SetActivePage(person2);
                break;
            case 3:
                SetActivePage(person3);
                break;
        }
    }*/

    private void SetActivePage(GameObject pageToEnable)
    {
        if (mainMenu != null)
            mainMenu.SetActive(false);

        if (messages != null)
            messages.SetActive(false);

        if (pageToEnable != null)
            pageToEnable.SetActive(true);
    }

    private void DisableDirectChildren(GameObject parent)
    {
        if (parent == null)
            return;

        Transform root = parent.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            root.GetChild(i).gameObject.SetActive(false);
        }
    }
}
