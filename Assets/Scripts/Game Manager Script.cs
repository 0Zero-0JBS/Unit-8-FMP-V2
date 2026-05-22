using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class GameManagerScript : MonoBehaviour
{
    public static GameManagerScript Instance;

    public enum MenuState
    {
        None,
        PauseMenu,
        Settings,
        QuitConfirmation,
        ReturnToMenuConfirmation,
        GameOver
    }

    [Header("UI Panels")]
    public GameObject pauseMenuUI;
    public GameObject settingsMenuUI;
    public GameObject gameOverUI;
    public GameObject quitGamePanel;
    public GameObject returnToMenuPanel;

    [Header("Controller Focus Options")]
    public GameObject pauseFirstSelectedButton;
    public GameObject settingsFirstSelectedButton;
    public GameObject quitFirstSelectedButton;
    public GameObject returnFirstSelectedButton;
    public GameObject gameOverFirstSelectedButton;

    private MenuState currentMenuState = MenuState.None;
    private MenuState previousMenuState = MenuState.None;

    [Header("Indicator Settings")]
    public GameObject[] indicatorPrefabs;
    public Transform canvasTransform;

    private Coroutine selectionCoroutine;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (currentMenuState == MenuState.GameOver) return;

        bool pressedPauseInput = Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetButtonDown("Pause");

        if (pressedPauseInput)
        {
            if (currentMenuState == MenuState.None)
            {
                Pause();
            }
            else if (currentMenuState == MenuState.PauseMenu)
            {
                Resume();
            }
            else
            {
                CloseSubMenu();
            }
        }
    }

    public void SwitchMenuState(MenuState newState)
    {
        pauseMenuUI.SetActive(false);
        settingsMenuUI.SetActive(false);
        gameOverUI.SetActive(false);
        quitGamePanel.SetActive(false);
        returnToMenuPanel.SetActive(false);

        if (currentMenuState == MenuState.PauseMenu || currentMenuState == MenuState.GameOver)
        {
            previousMenuState = currentMenuState;
        }

        currentMenuState = newState;

        if (selectionCoroutine != null)
        {
            StopCoroutine(selectionCoroutine);
        }

        switch (currentMenuState)
        {
            case MenuState.None:
                Time.timeScale = 1f;
                previousMenuState = MenuState.None;
                break;

            case MenuState.PauseMenu:
                pauseMenuUI.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(pauseFirstSelectedButton);
                Time.timeScale = 0f;
                break;

            case MenuState.Settings:
                settingsMenuUI.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(settingsFirstSelectedButton);
                break;

            case MenuState.QuitConfirmation:
                quitGamePanel.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(quitFirstSelectedButton);
                break;

            case MenuState.ReturnToMenuConfirmation:
                returnToMenuPanel.SetActive(true);
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(returnFirstSelectedButton);
                break;

            case MenuState.GameOver:
                gameOverUI.SetActive(true);
                Time.timeScale = 0f;
                if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(gameOverFirstSelectedButton);
                break;
        }
    }

    private IEnumerator SetFocusNextFrame(GameObject targetButton)
    {
        if (EventSystem.current == null) yield break;

        EventSystem.current.SetSelectedGameObject(null);

        yield return null;

        if (targetButton != null)
        {
            EventSystem.current.SetSelectedGameObject(targetButton);
        }
    }

    public GameObject SpawnIndicator(Transform asteroidTarget, int size)
    {
        int index = 3 - size;
        if (index < 0 || index >= indicatorPrefabs.Length) index = 0;

        GameObject newIndicator = Instantiate(indicatorPrefabs[index], canvasTransform);
        IndicatorScript script = newIndicator.GetComponent<IndicatorScript>();

        if (script != null) script.Setup(asteroidTarget, size);

        return newIndicator;
    }

    public void Resume()
    {
        if (AudioManagerScript.Instance != null && AudioManagerScript.Instance.buttonClick != null)
            AudioManagerScript.Instance.PlaySFX(AudioManagerScript.Instance.buttonClick);

        SwitchMenuState(MenuState.None);
    }

    void Pause() => SwitchMenuState(MenuState.PauseMenu);

    public void ShowGameOver()
    {
        if (AudioManagerScript.Instance != null && AudioManagerScript.Instance.playerDeathSound != null)
            AudioManagerScript.Instance.PlaySFX(AudioManagerScript.Instance.playerDeathSound);

        SwitchMenuState(MenuState.GameOver);
    }

    public void CloseSubMenu()
    {
        if (previousMenuState == MenuState.None) SwitchMenuState(MenuState.PauseMenu);
        else SwitchMenuState(previousMenuState);
    }

    public void OpenSettings() => SwitchMenuState(MenuState.Settings);
    public void CloseSettings() => CloseSubMenu();
    public void OpenReturnToMenuPanel() => SwitchMenuState(MenuState.ReturnToMenuConfirmation);
    public void CloseReturnToMenuPanel() => CloseSubMenu();
    public void OpenQuitConfirmation() => SwitchMenuState(MenuState.QuitConfirmation);
    public void CloseQuitConfirmation() => CloseSubMenu();

    public void RestartLevel()
    {
        PlayTransitionAudio();
        Time.timeScale = 1f;
        Instance = null;

        if (ScoreManagerScript.Instance != null) Destroy(ScoreManagerScript.Instance.gameObject);

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void QuitToMainMenu()
    {
        PlayTransitionAudio();
        Time.timeScale = 1f;
        Instance = null;

        if (AudioManagerScript.Instance != null && AudioManagerScript.Instance.mainMenuMusic != null)
        {
            AudioManagerScript.Instance.TransitionToMusic(AudioManagerScript.Instance.mainMenuMusic, 0.75f);
        }

        SceneManager.LoadScene("Main Menu");
    }

    private void PlayTransitionAudio()
    {
        if (AudioManagerScript.Instance != null && AudioManagerScript.Instance.buttonClick != null)
        {
            AudioManagerScript.Instance.PlaySFX(AudioManagerScript.Instance.buttonClick);
        }
    }

    public void SaveToDisk() => PlayerPrefs.Save();
    public void QuitGame() { PlayTransitionAudio(); Application.Quit(); }
}
