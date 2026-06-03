using UnityEngine;
using TMPro;

public class BatteryManager : MonoBehaviour
{
    [Header("Battery")]
    [SerializeField, Range(0, 100)] private int initialBattery = 100;
    [SerializeField, Range(0, 100)] private int currentBattery;
    [Tooltip("Base battery drain speed. This is the initial speed before activity bonuses.")]
    [SerializeField, Min(1)] private int speed = 5;
    [Tooltip("Runtime speed after applying phone/flashlight bonuses.")]
    [SerializeField] private int currentSpeed;

    [Header("UI")]
    [SerializeField] private TMP_Text batteryText;

    [Header("Threshold Events")]

    [SerializeField] private GameObject mainScreen;
    [SerializeField] private GameObject enableAt20Battery;
    [SerializeField] private GameObject enableAt10Battery;

    [Header("Phone Activity Sources")]
    [SerializeField] private GameObject phoneCanvas;
    [SerializeField] private GameObject flashLight;
    [SerializeField] private PhoneManager phoneManager;
    [SerializeField] private MessageManager messageManager;


    private float currentBatteryPrecise;

    private void Awake()
    {
        DisablePhoneCanvasChildren();

        currentBattery = Mathf.Clamp(initialBattery, 0, 100);
        currentBatteryPrecise = currentBattery;
        currentSpeed = speed;
        UpdateBatteryText();
        UpdateThresholdEvents();
        UpdatePhoneAvailability();
    }

    private void FixedUpdate()
    {
        if (currentBattery <= 0)
        {
            return;
        }

        currentSpeed = GetCurrentDrainSpeed();
        float drainPerMinute = currentSpeed;
        float drainPerSecond = drainPerMinute / 60f;
        currentBatteryPrecise -= drainPerSecond * Time.fixedDeltaTime;
        currentBatteryPrecise = Mathf.Clamp(currentBatteryPrecise, 0f, 100f);

        int nextValue = Mathf.FloorToInt(currentBatteryPrecise);
        if (nextValue != currentBattery)
        {
            currentBattery = nextValue;
            UpdateBatteryText();
            UpdateThresholdEvents();
            UpdatePhoneAvailability();
        }
    }

    private int GetCurrentDrainSpeed()
    {
        int activeBonus = 0;

        if (phoneCanvas != null && phoneCanvas.activeInHierarchy)
        {
            activeBonus += 3;
        }

        if (flashLight != null && flashLight.activeInHierarchy)
        {
            activeBonus += 5;
        }

        return speed + activeBonus;
    }

    private void UpdateBatteryText()
    {
        if (batteryText != null)
        {
            batteryText.text = currentBattery + "%";
        }
    }

    private void UpdateThresholdEvents()
    {
        if (enableAt20Battery != null && currentBattery == 20)
        {
            phoneManager.OpenOnlyMain();
            messageManager.PlayMessageSound();
            enableAt20Battery.SetActive(true);
        }

        if (currentBattery == 10)
        {
            messageManager.PlayMessageSound();
            if (enableAt10Battery != null)
            {
                phoneManager.OpenOnlyMain();
                enableAt10Battery.SetActive(true);
            }

            if (enableAt20Battery != null)
            {
                enableAt20Battery.SetActive(false);
            }
        }
    }

    private void UpdatePhoneAvailability()
    {
        bool hasBattery = currentBattery > 0;

        if (phoneManager != null)
        {
            phoneManager.SetCanOpenPhone(hasBattery);
            phoneManager.enabled = hasBattery;
        }
        
        if (!hasBattery)
        {
            DisableAllPhoneCanvases();

            CursorController.panelOpen = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    private void DisableAllPhoneCanvases()
    {
        if (phoneCanvas != null)
        {
            phoneCanvas.SetActive(false);
        }

        if (flashLight != null)
        {
            flashLight.SetActive(false);
        }
    }

    private void DisablePhoneCanvasChildren()
    {
        if (phoneCanvas == null)
            return;

        Transform root = phoneCanvas.transform;
        for (int i = 0; i < root.childCount; i++)
        {
            root.GetChild(i).gameObject.SetActive(false);
        }
    }
}
