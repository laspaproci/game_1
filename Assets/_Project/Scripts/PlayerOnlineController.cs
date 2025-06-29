using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerOnlineController : NetworkBehaviour
{
    [Header("Ruch i skok")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 7f;

    [Header("Atak")]
    [SerializeField] private float attackRange = 0.5f;
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private int attackDamage = 20;
    [SerializeField] private LayerMask targetLayer;

    [Header("Respawn")]
    [SerializeField] private float respawnDelay = 2f;

    private Transform[] spawnPoints;
    private NetworkVariable<int> hp = new NetworkVariable<int>(100);

    // Input System
    private PlayerInputActions inputActions;
    private Vector2            moveInput;

    // Komponenty
    private Rigidbody2D rb;
    private Animator    animator;
    private Vector3     baseScale;
    private bool        isGrounded = true;

    #region Unity lifecycle

    private void Awake()
    {
        // Komponenty
        rb        = GetComponent<Rigidbody2D>();
        animator  = GetComponent<Animator>();
        baseScale = transform.localScale;

        // InputSystem – inicjalizujemy od razu
        inputActions = new PlayerInputActions();

        // Punkty startowe
        var gos = GameObject.FindGameObjectsWithTag("SpawnPoint");
        spawnPoints = new Transform[gos.Length];
        for (int i = 0; i < gos.Length; i++)
            spawnPoints[i] = gos[i].transform;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Ustawienie pozycji startowej
        if (spawnPoints.Length > 0)
        {
            int idx = (int)OwnerClientId;
            if (idx < 0 || idx >= spawnPoints.Length) idx = 0;
            transform.position = spawnPoints[idx].position;
            Debug.Log($"[PlayerOnline] Client {OwnerClientId} spawned at point {idx}");
        }
        else
        {
            Debug.LogWarning("[PlayerOnline] Brak obiektów z tagiem 'SpawnPoint'!");
        }

        // Tylko właściciel steruje
        if (!IsOwner)
        {
            enabled = false;
            return;
        }

        UIManager.Instance.RegisterPlayer(OwnerClientId);
        hp.OnValueChanged += OnHpChanged;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        if (IsOwner)
            UIManager.Instance.UnregisterPlayer(OwnerClientId);
    }

    private void OnEnable()
    {
        //mapa
        if (inputActions.asset.FindActionMap("Gameplay") == null)
        {
            Debug.LogError("[PlayerOnline] Nie znaleziono ActionMap 'Gameplay' w PlayerInputActions.asset!");
            return;
        }

        var gm = inputActions.Gameplay;
        gm.Enable();

        gm.Move.performed   += HandleMove;
        gm.Move.canceled    += HandleMoveCanceled;
        gm.Jump.performed   += HandleJump;
        gm.Fall.performed   += HandleFall;
        gm.Attack.performed += HandleAttack;
    }

    private void OnDisable()
    {
        var gm = inputActions.Gameplay;
        if (gm.enabled)
        {
            gm.Move.performed   -= HandleMove;
            gm.Move.canceled    -= HandleMoveCanceled;
            gm.Jump.performed   -= HandleJump;
            gm.Fall.performed   -= HandleFall;
            gm.Attack.performed -= HandleAttack;
            gm.Disable();
        }
    }

    private void Update()
    {
        if (!IsOwner) return;
        HandleMovement();
    }

    #endregion

    #region Input callbacks

    private void HandleMove(InputAction.CallbackContext ctx)
    {
        moveInput = ctx.ReadValue<Vector2>();
    }

    private void HandleMoveCanceled(InputAction.CallbackContext ctx)
    {
        moveInput = Vector2.zero;
    }

    private void HandleJump(InputAction.CallbackContext _)
    {
        TryJump();
    }

    private void HandleFall(InputAction.CallbackContext _)
    {
        TryFall();
    }

    private void HandleAttack(InputAction.CallbackContext _)
    {
        SubmitAttackServerRpc();
    }

    #endregion

    #region Movement & attack

    private void HandleMovement()
    {
        float h = moveInput.x;
        
        rb.linearVelocity = new Vector2(h * moveSpeed, rb.linearVelocity.y);
        animator.SetFloat("Speed", Mathf.Abs(h));

        if (h > 0f)      transform.localScale = baseScale;
        else if (h < 0f) transform.localScale = new Vector3(-baseScale.x, baseScale.y, baseScale.z);
    }

    private void TryJump()
    {
        if (isGrounded)
        {
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            isGrounded = false;
            animator.SetBool("IsJumping", true);
        }
    }

    private void TryFall()
    {
        if (!isGrounded)
          
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -jumpForce);
    }

    [ServerRpc]
    private void SubmitAttackServerRpc(ServerRpcParams rpcParams = default)
    {
        Vector2 origin = (Vector2)transform.position + Vector2.right * attackRange * Mathf.Sign(transform.localScale.x);
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, attackRadius, targetLayer);
        foreach (var hit in hits)
        {
            if (hit.TryGetComponent(out PlayerOnlineController pc))
                pc.hp.Value = Mathf.Max(0, pc.hp.Value - attackDamage);
        }
        PlayAttackClientRpc();
    }

    [ClientRpc]
    private void PlayAttackClientRpc(ClientRpcParams rpcParams = default)
    {
        animator.SetTrigger("Attack");
    }

    #endregion

    #region HP & Respawn

    private void OnHpChanged(int oldHp, int newHp)
    {
        if (IsOwner)
            UIManager.Instance.UpdateHpDisplay(OwnerClientId, newHp);

        if (newHp <= 0)
            StartCoroutine(HandleDeathAndRespawn());
    }

    private IEnumerator HandleDeathAndRespawn()
    {
        animator.SetTrigger("Die");
        GetComponent<Collider2D>().enabled     = false;
        GetComponent<SpriteRenderer>().enabled = false;

        yield return new WaitForSeconds(respawnDelay);

        if (IsServer)
            hp.Value = 100;

        if (spawnPoints.Length > 0)
        {
            int idx = (int)OwnerClientId;
            if (idx < 0 || idx >= spawnPoints.Length) idx = 0;
            transform.position = spawnPoints[idx].position;
        }

        GetComponent<Collider2D>().enabled     = true;
        GetComponent<SpriteRenderer>().enabled = true;
        animator.ResetTrigger("Die");
    }

    private void OnCollisionEnter2D(Collision2D c)
    {
        if (c.contacts.Length > 0 && c.contacts[0].normal.y > 0.5f)
        {
            isGrounded = true;
            animator.SetBool("IsJumping", false);
        }
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (!Application.isPlaying) return;
        Gizmos.color = Color.red;
        Vector2 origin = (Vector2)transform.position + Vector2.right * attackRange * Mathf.Sign(transform.localScale.x);
        Gizmos.DrawWireSphere(origin, attackRadius);
    }
}
