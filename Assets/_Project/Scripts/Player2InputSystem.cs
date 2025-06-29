using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
//sterowanie player 2 lokalnie

[RequireComponent(typeof(Rigidbody2D), typeof(Animator), typeof(Health))]
public class Player2InputSystem : MonoBehaviour
{
    [Header("Ruch i skok")]
    public float moveSpeed = 5f;
    public float jumpForce = 7f;

    [Header("Atak")]
    public float attackRange   = 0.02f;
    public float attackRadius  = 0.2f;
    public int   attackDamage  = 20;
    public LayerMask targetLayer;

    [Header("Fall Death")]
    public float deathY     = -5f;
    public int   fallDamage = 9999;

    [Header("Respawn")]
    public Transform spawnPoint;
    public float     respawnDelay = 2f;

    // stany
    private Vector2       moveInput;
    private bool          isGrounded    = true;
    private int           lastDirection = 1;
    private bool          isDead        = false;

    // referencje
    private Vector3             baseScale;
    private Rigidbody2D         rb;
    private Animator            animator;
    private Health              health;
    private Player2InputActions inputActions;
    private bool                eventsBound   = false;

   
    
private void Awake()
{
    inputActions = new Player2InputActions();

    rb        = GetComponent<Rigidbody2D>();
    animator  = GetComponent<Animator>();
    health    = GetComponent<Health>();

    Vector3 startScale = transform.localScale;
    startScale.x = Mathf.Abs(startScale.x);
  
    startScale.x *= -1f;
    
    transform.localScale = startScale;
    baseScale = transform.localScale;

    if (health != null)
        health.OnDeath += OnPlayerDied;
}

    
    private void OnEnable()
    {
        if (inputActions == null)
            inputActions = new Player2InputActions();

        var gm = inputActions.Gameplay;
        gm.Enable();

        if (!eventsBound)
        {
            gm.Move.performed   += OnMove;
            gm.Move.canceled    += OnMoveCanceled;
            gm.Jump.performed   += OnJump;
            gm.Fall.performed   += OnFall;
            gm.Attack.performed += OnAttack;
            eventsBound = true;
        }
    }

    private void OnDisable()
    {
        if (inputActions != null)
        {
            var gm = inputActions.Gameplay;
            if (eventsBound)
            {
                gm.Move.performed   -= OnMove;
                gm.Move.canceled    -= OnMoveCanceled;
                gm.Jump.performed   -= OnJump;
                gm.Fall.performed   -= OnFall;
                gm.Attack.performed -= OnAttack;
                eventsBound = false;
            }
            gm.Disable();
        }

        if (health != null)
            health.OnDeath -= OnPlayerDied;
    }

    private void Update()
    {
        if (isDead) return;

        Move();

        if (transform.position.y < deathY && health != null)
            health.TakeDamage(fallDamage);
    }

    private void OnMove(InputAction.CallbackContext ctx)        => moveInput = ctx.ReadValue<Vector2>();
    private void OnMoveCanceled(InputAction.CallbackContext _) => moveInput = Vector2.zero;

    private void Move()
    {
        float m = moveInput.x;

        // 1) Ruch fizyczny
        rb.linearVelocity = new Vector2(m * moveSpeed, rb.linearVelocity.y);

        // 2) Animacja prędkości (blend tree / przejścia)
        animator.SetFloat("Speed", Mathf.Abs(m));

        // 3) Flip tylko przy ruchu
        if (m > 0f)
        {
            lastDirection = 1;
            animator.SetBool("FacingRight", true);
            transform.localScale = new Vector3(
                Mathf.Abs(baseScale.x),
                baseScale.y,
                baseScale.z
            );
        }
        else if (m < 0f)
        {
            lastDirection = -1;
            animator.SetBool("FacingRight", false);
            transform.localScale = new Vector3(
                -Mathf.Abs(baseScale.x),
                baseScale.y,
                baseScale.z
            );
        }
        // **else** (m == 0): nic nie robimy — zostawiamy flip IdleFlipperowi
    }

    private void OnJump(InputAction.CallbackContext _) => Jump();
    private void Jump()
    {
        if (!isGrounded) return;
        rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        isGrounded = false;
        animator.SetBool("IsJumping", true);
    }

    private void OnFall(InputAction.CallbackContext _) => Fall();
    private void Fall()
    {
        if (!isGrounded)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -jumpForce);
    }

    private void OnAttack(InputAction.CallbackContext _) => Attack();
    private void Attack()
    {
        animator.SetTrigger("Attack");

        Vector2 origin = (Vector2)transform.position + Vector2.right * lastDirection * attackRange;
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRadius, targetLayer);
        foreach (var h in hits)
        {
            if (h.gameObject == gameObject) continue;
            h.GetComponent<Health>()?.TakeDamage(attackDamage);
        }
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (c.contacts.Length > 0 && c.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
            animator.SetBool("IsJumping", false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Vector2 origin = (Vector2)transform.position + Vector2.right * lastDirection * attackRange;
        Gizmos.DrawWireSphere(origin, attackRadius);
    }

    private void OnPlayerDied()
    {
        if (isDead) return;
        isDead = true;
        rb.simulated = false;
        enabled      = false;
        StartCoroutine(RespawnCoroutine());
    }

    private IEnumerator RespawnCoroutine()
    {
        yield return new WaitForSeconds(respawnDelay + 1.5f);

        // if (spawnPoint != null)
        //     transform.position = spawnPoint.position;
        // else
        //     Debug.LogWarning("[Player2InputSystem] SpawnPoint nie przypisany!");

        rb.simulated      = true;
        rb.linearVelocity = Vector2.zero;
        isGrounded        = true;

        health?.ResetHealth();
        animator.Rebind();
        animator.Update(0f);

        isDead  = false;
        enabled = true;
    }
}
