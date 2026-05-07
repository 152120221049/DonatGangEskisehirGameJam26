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

    [Header("Audio")]
    public AudioSource healthAudioSource;
    public AudioClip damageClip;
    public AudioClip agonyClip;
    public AudioClip deathClip;
    
    private PlayerController playerController;
    private float agonyTimer;

    private void Start()
    {
        currentHealth = maxHealth;
        playerController = GetComponent<PlayerController>();
        
        if (healthAudioSource == null) healthAudioSource = GetComponent<AudioSource>();
        if (healthAudioSource == null) healthAudioSource = gameObject.AddComponent<AudioSource>();
        
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void Update()
    {
        if (invincibilityTimer > 0)
            invincibilityTimer -= Time.deltaTime;

        // Agony sound when low health
        if (currentHealth > 0 && currentHealth < maxHealth * 0.3f)
        {
            agonyTimer -= Time.deltaTime;
            if (agonyTimer <= 0)
            {
                if (agonyClip != null) healthAudioSource.PlayOneShot(agonyClip, 0.6f);
                agonyTimer = 2.5f; // Play every 2.5s
            }
        }
    }

    public bool TakeDamage(float amount)
    {
        if (invincibilityTimer > 0 || currentHealth <= 0) return false;

        currentHealth -= amount;
        invincibilityTimer = invincibilityDuration;

        Debug.Log($"[PlayerHealth] Took damage: {amount}. Current health: {currentHealth}");
        
        if (damageClip != null) healthAudioSource.PlayOneShot(damageClip, 0.5f);

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

        return true;
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
        OnHealthChanged?.Invoke(currentHealth / maxHealth);
    }

    private void Die()
    {
        Debug.Log("[PlayerHealth] Player Died!");
        if (deathClip != null) healthAudioSource.PlayOneShot(deathClip, 0.8f);

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
