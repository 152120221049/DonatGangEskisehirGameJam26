using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Menu Panels")]
    public GameObject mainMenuPanel;
    public GameObject settingsPanel;
    public GameObject continueButton;

    [Header("Level to Load")]
    public string firstLevelName = "Level1";

    private void Start()
    {
        // Ensure cursor is visible and unlocked in the menu
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Show main menu by default
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // Check for save data and toggle Continue button
        if (continueButton != null)
        {
            continueButton.SetActive(SaveManager.HasSavedProgress());
        }
    }

    public void PlayGame()
    {
        // This is now "New Game"
        SaveManager.ClearSave();
        SceneManager.LoadScene(firstLevelName);
    }

    public void ContinueGame()
    {
        if (SaveManager.HasSavedProgress())
        {
            string savedLevel = SaveManager.LoadSavedLevel();
            Debug.Log($"[MainMenu] Continuing game from: {savedLevel}");
            SceneManager.LoadScene(savedLevel);
        }
        else
        {
            Debug.LogWarning("[MainMenu] No saved progress found!");
        }
    }

    public void OpenSettings()
    {
        mainMenuPanel.SetActive(false);
        settingsPanel.SetActive(true);
    }

    public void CloseSettings()
    {
        settingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }



    public void QuitGame()
    {
        Debug.Log("[MainMenu] Quitting Game...");
        Application.Quit();
    }
}
