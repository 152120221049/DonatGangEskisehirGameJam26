using UnityEngine;
using UnityEngine.Events;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health Settings")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float invincibilityDuration = 1f;
    private float invincibilityTimer;

    [Header("Damage Effects")]
    public float damageSlowdownMultiplier = 0.5f;
    public float damageSlowdownDuration = 0.3f;

    [Header("Events")]
    public UnityEvent<float> OnHealthChanged;
    public UnityEvent OnDeath;

    private PlayerController playerController;

    private void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void Update()
    {
        if (invincibilityTimer > 0)
            invincibilityTimer -= Time.deltaTime;
    }

    public void TakeDamage(float amount)
    {
        if (invincibilityTimer > 0 || currentHealth <= 0) return;

        currentHealth -= amount;
        invincibilityTimer = invincibilityDuration;

        Debug.Log($"[PlayerHealth] Took damage: {amount}. Current health: {currentHealth}");
        
        OnHealthChanged?.Invoke(currentHealth / maxHealth);

        // Slow down the player
        if (playerController != null)
        {
            playerController.ApplySlowdown(damageSlowdownDuration, damageSlowdownMultiplier);
        }

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Player Died!");
        OnDeath?.Invoke();
        if (playerController != null)
        {
            playerController.Die();
            // Reset health on respawn
            currentHealth = maxHealth;
            OnHealthChanged?.Invoke(1f);
        }
    }
}
