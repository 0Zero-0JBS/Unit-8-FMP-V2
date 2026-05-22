using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using TMPro;

public class SceneManagerScript : MonoBehaviour
{
    public enum MenuState
    {
        MainMenu,
        Settings,
        QuitConfirmation,
        ReturnToMenuConfirmation
    }

    [Header("Menu Panels")]
    public GameObject mainMenu;
    public GameObject settingsMenu;
    public GameObject quitMenu;
    public GameObject returnToMenuPanel;

    [Header("Controller Focus Buttons")]
    public GameObject menuFirstSelectedButton;
    public GameObject settingsFirstSelectedButton;
    public GameObject quitFirstSelectedButton;
    public GameObject returnFirstSelectedButton;

    private MenuState currentMenuState = MenuState.MainMenu;

    [Header("Records Display (In Main Menu)")]
    public TextMeshProUGUI mainMenuHighScoreText;
    public TextMeshProUGUI mainMenuBestTimeText;

    public void SwitchMenuState(MenuState newState)
    {
        // Turns off every panel to clear the screen layer entirely
        if (mainMenu != null) mainMenu.SetActive(false);
        if (settingsMenu != null) settingsMenu.SetActive(false);
        if (quitMenu != null) quitMenu.SetActive(false);
        if (returnToMenuPanel != null) returnToMenuPanel.SetActive(false);

        currentMenuState = newState;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        // Turns on ONLY the requested display layout panels
        switch (currentMenuState)
        {
            case MenuState.MainMenu:
                if (mainMenu != null) mainMenu.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(menuFirstSelectedButton);
                break;

            case MenuState.Settings:
                if (settingsMenu != null) settingsMenu.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(settingsFirstSelectedButton);
                break;

            case MenuState.QuitConfirmation:
                if (quitMenu != null) quitMenu.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(quitFirstSelectedButton);
                break;

            case MenuState.ReturnToMenuConfirmation:
                if (returnToMenuPanel != null) returnToMenuPanel.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(returnFirstSelectedButton);
                break;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Time.timeScale = 1f;

        // Get the name of the scene that just loaded
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (AudioManagerScript.Instance != null)
        {
            if (currentSceneName == "Main Menu" || currentSceneName == "Tutorial")
            {
                if (AudioManagerScript.Instance.mainMenuMusic != null)
                {
                    // TransitionToMusic checks internally if the song is already playing. 
                    // Because of this, transitioning from Main Menu to Tutorial will NOT restart the song!
                    AudioManagerScript.Instance.TransitionToMusic(AudioManagerScript.Instance.mainMenuMusic, 0.5f);
                }
            }
            else if (currentSceneName == "Level")
            {
                if (AudioManagerScript.Instance.battleMusic != null)
                {
                    AudioManagerScript.Instance.TransitionToMusic(AudioManagerScript.Instance.battleMusic, 0.5f);
                }
            }
        }

        int savedHigh = PlayerPrefs.GetInt("HighScore", 0);
        if (mainMenuHighScoreText != null) mainMenuHighScoreText.text = "HIGH SCORE: " + savedHigh.ToString("0000");

        float savedBestTime = PlayerPrefs.GetFloat("BestTime", 0f);
        if (mainMenuBestTimeText != null)
        {
            int minutes = Mathf.FloorToInt(savedBestTime / 60);
            int seconds = Mathf.FloorToInt(savedBestTime % 60);
            mainMenuBestTimeText.text = $"BEST TIME: {minutes:00}:{seconds:00}";
        }

        SwitchMenuState(MenuState.MainMenu);
    }

    public void OpenSettingsMenu() => SwitchMenuState(MenuState.Settings);
    public void CloseSettingsMenu() => SwitchMenuState(MenuState.MainMenu);

    public void OpenQuitMenu() => SwitchMenuState(MenuState.QuitConfirmation);
    public void CloseQuitMenu() => SwitchMenuState(MenuState.MainMenu);

    public void OpenReturnToMenuPanel() => SwitchMenuState(MenuState.ReturnToMenuConfirmation);
    public void CloseReturnToMenuPanel() => SwitchMenuState(MenuState.MainMenu);

    public void PlayGame() => SceneManager.LoadScene("Level");
    public void OpenTutorial() => SceneManager.LoadScene("Tutorial");
    public void ToMainMenu() => SceneManager.LoadScene("Main Menu");
    public void RestartLevel() => SceneManager.LoadScene("Level");
    public void QuitGame() => Application.Quit();

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ResetHighScore()
    {
        PlayerPrefs.DeleteKey("HighScore");
        PlayerPrefs.DeleteKey("BestTime");

        if (mainMenuHighScoreText != null) mainMenuHighScoreText.text = "HIGH SCORE: 0000";
        if (mainMenuBestTimeText != null) mainMenuBestTimeText.text = "BEST TIME: 00:00";
    }
}
