using UnityEngine;

public class GhostReplayBindings : MonoBehaviour
{
    [SerializeField] private GameObject flashlightObject;

    public void SetFlashlightState(bool isOn)
    {
        if (flashlightObject == null)
            return;

        flashlightObject.SetActive(isOn);
    }
}
