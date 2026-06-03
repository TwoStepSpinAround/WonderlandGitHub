using UnityEngine;

using UnityEngine.AI;

public class PlayerControls : MonoBehaviour
{
    public float moveSpeed = 3.5f;

    private NavMeshAgent agent;

    [Header("Audio")]
    [SerializeField] private AudioSource walkAudioSource;
    [SerializeField] private AudioClip walkClip;
    private bool isWalking = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updatePosition = true;
        agent.updateRotation = true;
        if (walkAudioSource == null)
            walkAudioSource = GetComponent<AudioSource>();
    }

    void Update()
    {
        // WASD movement
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = new Vector3(h, 0, v).normalized;
        if (inputDir.magnitude > 0.1f)
        {
            Vector3 move = transform.TransformDirection(inputDir) * moveSpeed;
            agent.Move(move * Time.deltaTime);
            if (!isWalking)
            {
                isWalking = true;
                PlayWalkSound();
            }
        }
        else
        {
            if (isWalking)
            {
                isWalking = false;
                StopWalkSound();
            }
        }
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
