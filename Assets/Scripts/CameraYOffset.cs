using UnityEngine;

public class CameraYOffset : MonoBehaviour
{
    public float yOffset = 1.7f;

    void LateUpdate()
    {
        Vector3 localPos = transform.localPosition;
        localPos.y = yOffset;
        transform.localPosition = localPos;
    }
}
