using UnityEngine;

public static class SaveManager
{
    private const string SAVED_LEVEL_KEY = "HighestUnlockedLevel";

    /// <summary>
    /// Saves the name of the level the player has reached.
    /// </summary>
    public static void SaveLevel(string levelName)
    {
        PlayerPrefs.SetString(SAVED_LEVEL_KEY, levelName);
        PlayerPrefs.Save();
        Debug.Log($"[SaveManager] Progress saved. Next level: {levelName}");
    }

    /// <summary>
    /// Loads the saved level name.
    /// </summary>
    public static string LoadSavedLevel()
    {
        return PlayerPrefs.GetString(SAVED_LEVEL_KEY, "");
    }

    /// <summary>
    /// Checks if the player has any saved progress.
    /// </summary>
    public static bool HasSavedProgress()
    {
        return PlayerPrefs.HasKey(SAVED_LEVEL_KEY) && !string.IsNullOrEmpty(PlayerPrefs.GetString(SAVED_LEVEL_KEY));
    }

    /// <summary>
    /// Deletes the saved progress (used for New Game).
    /// </summary>
    public static void ClearSave()
    {
        PlayerPrefs.DeleteKey(SAVED_LEVEL_KEY);
        PlayerPrefs.Save();
        Debug.Log("[SaveManager] Save data cleared for New Game.");
    }
}
