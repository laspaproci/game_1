using UnityEngine;

[RequireComponent(typeof(Animator))]
public class IdleFlipper : MonoBehaviour
{
    private Animator animator;
    private Vector3 baseScale;

    void Awake()
    {
        animator = GetComponent<Animator>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        //flip
        if (Mathf.Approximately(animator.GetFloat("Speed"), 0f))
        {
            bool right = animator.GetBool("FacingRight");
            float dir = right ? 1f : -1f;
            transform.localScale = new Vector3(baseScale.x * dir, baseScale.y, baseScale.z);
        }
    }
}
