using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Animator))]
public class Health : MonoBehaviour
{
    public int maxHealth = 100;
    public int damagePerHit = 20;
    public HealthBar healthBar;     

    // informacja o śmierci 
    public delegate void DeathHandler();
    public event DeathHandler OnDeath;

    private int     currentHealth;
    private Animator animator;
    private bool    isDead = false;

    void Awake()
    {
        currentHealth = maxHealth;
        animator      = GetComponent<Animator>();
        if (healthBar != null)
            healthBar.SetMaxHealth(maxHealth);
    }

    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        animator.SetTrigger("Hit");

        if (currentHealth == 0)
            Die();
    }

    private void Die()
    {
        isDead = true;
        animator.SetTrigger("Die");
        OnDeath?.Invoke();
        StartCoroutine(DisableAfterDeath());
    }

    private IEnumerator DisableAfterDeath()
    {
      
        yield return new WaitForSeconds(1.5f);
    
    }

    // Przywraca życie do maxHealth i resetuje Animatora.
    
    public void ResetHealth()
    {
        isDead        = false;
        currentHealth = maxHealth;
        if (healthBar != null)
            healthBar.SetHealth(currentHealth);

        animator.ResetTrigger("Hit");
        animator.ResetTrigger("Die");
        animator.Rebind();
        animator.Update(0f);

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }
}
