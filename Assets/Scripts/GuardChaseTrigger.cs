using UnityEngine;

// Attach this script to the object with the collider (set as isTrigger)
// Assign the GuardNPC reference in the inspector
public class GuardChaseTrigger : MonoBehaviour
{
    public GuardNPC guardNPC;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Ghost"))
        {
            if (guardNPC != null)
            {
                guardNPC.OnDetectionTriggerEnter(other);
            }
        }
    }
}
