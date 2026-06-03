using UnityEngine;

/// <summary>
/// Utility that keeps only one camera active at a time and ensures only the
/// active camera has enabled audio listeners.  Call <see cref="Activate"/>
/// whenever you want to switch cameras instead of toggling them manually.
/// </summary>
public static class CameraManager
{
    public static void DeactivateAll()
    {
        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            cam.enabled = false;
            SetAudioListenerEnabled(cam, false);
        }
    }

    /// <summary>
    /// Activates one Camera and disables all others.
    /// </summary>
    public static void Activate(Camera active)
    {
        if (active == null)
            return;

        if (!active.gameObject.activeSelf)
            active.gameObject.SetActive(true);

        foreach (var cam in Object.FindObjectsOfType<Camera>(true))
        {
            bool thisOne = (cam == active);
            cam.enabled = thisOne;
            SetAudioListenerEnabled(cam, thisOne);
        }
    }

    private static void SetAudioListenerEnabled(Camera cam, bool enabled)
    {
        var listener = cam.GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = enabled;
    }
}
