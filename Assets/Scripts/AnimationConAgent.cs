using UnityEngine;

public class AnimationConAgent : MonoBehaviour
{
    public Animator animator; // Assign in inspector or via script
    private Vector3 lastPosition;
    private bool isWalking = false;

    [Header("Audio")]
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private AudioClip walkClip;

    void Start()
    {
        lastPosition = transform.position;
        if (animator == null)
        {
            animator = GetComponent<Animator>();
        }
        if (walkAudioSource == null)
            walkAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // Check if the object has moved since last frame
        bool currentlyWalking = (transform.position - lastPosition).sqrMagnitude > 0.0001f;
        if (currentlyWalking != isWalking)
        {
            isWalking = currentlyWalking;
            animator.SetBool("isWalking", isWalking);
            if (isWalking)
                PlayWalkSound();
            else
                StopWalkSound();
        }
        lastPosition = transform.position;
    }

    private void PlayWalkSound()
    {
        if (walkAudioSource != null && walkClip != null && !walkAudioSource.isPlaying)
        {
            walkAudioSource.clip = walkClip;
            walkAudioSource.loop = true;
            walkAudioSource.Play();
        }
    }

    private void StopWalkSound()
    {
        if (walkAudioSource != null && walkAudioSource.isPlaying)
        {
            walkAudioSource.Stop();
        }
    }
}
