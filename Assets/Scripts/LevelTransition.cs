using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider))]
public class LevelTransition : MonoBehaviour
{
    [Header("Level Settings")]
    [Tooltip("The exact name of the next scene to load.")]
    public string nextLevelName;
    
    [Header("Visual Feedback (Optional)")]
    public GameObject transitionEffect;
    
    private void Awake()
    {
        // Ensure the collider is a trigger so the player can walk into it
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player is the one entering the zone
        if (other.CompareTag("Player"))
        {
            Debug.Log($"[LevelTransition] Player entered transition zone. Moving to {nextLevelName}...");

            // 1. Save Progress
            if (!string.IsNullOrEmpty(nextLevelName))
            {
                SaveManager.SaveLevel(nextLevelName);
            }
            else
            {
                Debug.LogWarning("[LevelTransition] Next level name is empty! Can't save progress.");
            }

            // 2. Play Effects
            if (transitionEffect != null)
            {
                Instantiate(transitionEffect, transform.position, Quaternion.identity);
            }

            // 3. Load Level
            if (!string.IsNullOrEmpty(nextLevelName))
            {
                SceneManager.LoadScene(nextLevelName);
            }
        }
    }
}
