using UnityEngine;
using UnityEngine.UI;
using TMPro; // Assuming you are using TextMeshPro for text

public class PlayerUI : MonoBehaviour
{
    [Header("UI References")]
    public Slider healthSlider;
    public Image livesImage;
    
    [Header("Life Sprites (Size 3)")]
    [Tooltip("Element 0 = 1 life left, Element 1 = 2 lives left, Element 2 = 3 lives left")]
    public Sprite[] lifeSprites;
    
    [Header("Player Reference")]
    public PlayerHealth playerHealth;

    private void Start()
    {
        // If playerHealth isn't assigned, try to find the player in the scene
        if (playerHealth == null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                playerHealth = player.GetComponent<PlayerHealth>();
            }
        }

        // Subscribe to the events
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.AddListener(UpdateHealthBar);
            playerHealth.OnLivesChanged.AddListener(UpdateLivesDisplay);
            
            // Initial UI Update
            UpdateHealthBar(playerHealth.currentHealth / playerHealth.maxHealth);
            UpdateLivesDisplay(playerHealth.currentLives);
        }
        else
        {
            Debug.LogWarning("[PlayerUI] PlayerHealth reference not found!");
        }
    }

    /// <summary>
    /// Updates the slider value (expects a float between 0 and 1).
    /// </summary>
    public void UpdateHealthBar(float healthPercentage)
    {
        if (healthSlider != null)
        {
            healthSlider.value = healthPercentage;
        }
    }

    /// <summary>
    /// Updates the image displaying current lives.
    /// </summary>
    public void UpdateLivesDisplay(int lives)
    {
        if (livesImage != null && lifeSprites != null && lifeSprites.Length > 0)
        {
            // Since arrays start at 0, 1 life = index 0, 2 lives = index 1, 3 lives = index 2.
            int index = Mathf.Clamp(lives - 1, 0, lifeSprites.Length - 1);
            
            if (lifeSprites[index] != null)
            {
                livesImage.sprite = lifeSprites[index];
            }
        }
    }

    private void OnDestroy()
    {
        // Always good practice to unsubscribe when the UI is destroyed
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged.RemoveListener(UpdateHealthBar);
            playerHealth.OnLivesChanged.RemoveListener(UpdateLivesDisplay);
        }
    }
}
