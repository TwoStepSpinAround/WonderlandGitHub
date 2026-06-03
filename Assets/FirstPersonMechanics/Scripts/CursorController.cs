using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems; // needed for UI checks
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[RequireComponent(typeof(InteractionRayCaster))]
public class CursorController : MonoBehaviour {
    // when a UI panel like the elevator is active we should ignore world-click hiding
    public static bool panelOpen = false;

    // Use this for initialization
    void Start () {
        EnsureEventSystem();

        // start in locked/hidden state as before
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void EnsureEventSystem()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
        {
            Debug.LogWarning("CursorController: no EventSystem found in scene; creating one automatically.");
            var esGO = new GameObject("EventSystem");
            es = esGO.AddComponent<EventSystem>();
        }

#if ENABLE_INPUT_SYSTEM
        if (es.GetComponent<InputSystemUIInputModule>() == null)
        {
            var inputModule = es.gameObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();
        }

        if (es.GetComponent<StandaloneInputModule>() == null)
            es.gameObject.AddComponent<StandaloneInputModule>();
#else
        if (es.GetComponent<StandaloneInputModule>() == null)
            es.gameObject.AddComponent<StandaloneInputModule>();
#endif
    }

    void Update () {
        // Unlock Cursor when the player hits escape
        if (Input.GetKey(KeyCode.Escape))
        {
            ShowCursor();
        }

        // only hide/lock the cursor if we're not clicking on UI
        if (Input.GetButtonDown("Fire1") && Cursor.visible == true)
        {
            bool overUI = IsPointerOverUI();

            // if the panel is open, we never hide the cursor (we just log and exit)
            if (panelOpen)
            {
                return;
            }

            if (overUI)
            {
                // let the UI handle it; don't change cursor state this frame
                return;
            }

            // otherwise defer hiding until after the current frame so UI events can fire
            StartCoroutine(HideNextFrame());
        }
    }

    // non‑UI click path: hide the cursor a frame later to avoid stealing events
    IEnumerator HideNextFrame()
    {
        yield return new WaitForEndOfFrame();
        HideCursor();
    }

    void HideCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void ShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ChangeCursorIcon(Texture2D texture)
    {
        Cursor.SetCursor(texture, Vector2.zero, CursorMode.ForceSoftware);
        ShowCursor();
    }

    /// <summary>
    /// Returns true if the current mouse position is over any UI element.
    /// This is a more reliable replacement for EventSystem.IsPointerOverGameObject()
    /// when the cursor might be locked or the event system isn't tracking the pointer.
    /// </summary>
    private bool IsPointerOverUI()
    {
        if (EventSystem.current == null)
            return false;

        var ped = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);
        return results.Count > 0;
    }
}
