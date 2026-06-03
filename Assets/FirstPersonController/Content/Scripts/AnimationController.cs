using UnityEngine;

public class AnimationController : MonoBehaviour {

    [SerializeField] private Animator animator;

    [Header("Blend Tree Parameter")]
    [SerializeField] private string phoneParameterName = "Phone";

    [Header("Blend Tree Values")]
    [SerializeField] private float idleValue = 0f;
    [SerializeField] private float lookAtPhoneValue = 1f;

    [Header("Blend Smoothing")]
    [SerializeField] private float parameterDampTime = 0.15f;

    private bool isLookingAtPhone;
    private bool ready;
    private float targetBlendValue;

    void Start () {
        if (animator == null)
            animator = GetComponent<Animator>();

        if (animator == null)
        {
            Debug.LogError("AnimationController: Animator is missing.");
            return;
        }

        if (!ResolveParameterName())
        {
            Debug.LogError("AnimationController: No float parameter found on Animator.");
            return;
        }

        ready = true;
        SetIdleState();
        animator.SetFloat(phoneParameterName, targetBlendValue);
    }

    void Update () {
        if (!ready)
            return;

        if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleLookAtPhone();
        }

        animator.SetFloat(phoneParameterName, targetBlendValue, parameterDampTime, Time.deltaTime);
    }

    private void ToggleLookAtPhone()
    {
        isLookingAtPhone = !isLookingAtPhone;

        if (isLookingAtPhone)
        {
            targetBlendValue = lookAtPhoneValue;
            return;
        }

        SetIdleState();
    }

    private void SetIdleState()
    {
        isLookingAtPhone = false;
        targetBlendValue = idleValue;
    }

    private bool ResolveParameterName()
    {
        if (HasFloatParameter(phoneParameterName))
            return true;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float)
            {
                phoneParameterName = parameter.name;
                Debug.LogWarning("AnimationController: 'Phone' was not found. Using float parameter '" + phoneParameterName + "' instead.");
                return true;
            }
        }

        return false;
    }

    private bool HasFloatParameter(string parameterName)
    {
        if (animator == null || string.IsNullOrEmpty(parameterName))
            return false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == AnimatorControllerParameterType.Float && parameter.name == parameterName)
                return true;
        }

        return false;
    }
}
