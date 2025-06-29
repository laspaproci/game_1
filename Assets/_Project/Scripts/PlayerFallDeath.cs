using UnityEngine;

[RequireComponent(typeof(Health))]
public class PlayerFallDeath : MonoBehaviour
{
    [Header("Fall Death")]
    public float deathY = -10f;          // dead line
    public int fallDamage = 9999;      // obrazenia z upadku

    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
        if (health == null)
            Debug.LogWarning($"[{name}] Brak komponentu Health!");
    }

    void Update()
    {
        if (transform.position.y < deathY && health != null)
        {
            health.TakeDamage(fallDamage);
            enabled = false;
        }
    }
}