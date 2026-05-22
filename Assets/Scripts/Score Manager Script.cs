using TMPro;
using UnityEngine;

public class ScoreManagerScript : MonoBehaviour
{
    public static ScoreManagerScript Instance { get; private set; }

    [Header("HUD Text Elements")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI healthText;
    public TextMeshProUGUI ammoText;

    [Header("Records Display")]
    public TextMeshProUGUI highScoreText;
    public TextMeshProUGUI bestTimeText;

    [Header("Timer Settings")]
    public TextMeshProUGUI timerText;

    [Header("Game Over Settings")]
    public GameObject gameOverPanel;

    private int currentScore = 0;
    private int highScore = 0;
    private float elapsedTime = 0f;
    private float bestTime = 0f;
    private bool canRunTimer = true;

    void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); return; }

        highScore = PlayerPrefs.GetInt("HighScore", 0);
        bestTime = PlayerPrefs.GetFloat("BestTime", 0f);
    }

    void Start()
    {
        Time.timeScale = 1f;
        UpdateScoreDisplay();
        UpdateTimerDisplay();

        if (highScoreText != null) highScoreText.text = "";
        if (bestTimeText != null) bestTimeText.text = "";
    }

    void Update()
    {
        if (canRunTimer)
        {
            elapsedTime += Time.deltaTime;
            UpdateTimerDisplay();
        }
    }

    public void StartTimer() => canRunTimer = true;
    public void StopTimer() => canRunTimer = false;

    public void AddScore(int points)
    {
        currentScore += points;
        UpdateScoreDisplay();

        if (AudioManagerScript.Instance != null)
        {
            AudioManagerScript.Instance.PlayScorePointSound();
        }

        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }
    }

    public void UpdateHUD(int lives, int ammo, int maxAmmo, bool reloading)
    {
        healthText.text = "LIVES: " + lives;
        ammoText.text = reloading ? "RELOADING..." : $"AMMO: {ammo}/{maxAmmo}";
    }

    public void StopTimerPermanently()
    {
        canRunTimer = false;

        // Check Points Record
        if (currentScore > highScore)
        {
            highScore = currentScore;
            PlayerPrefs.SetInt("HighScore", highScore);
        }

        // Check Time Record (Best Time)
        if (elapsedTime > bestTime)
        {
            bestTime = elapsedTime;
            PlayerPrefs.SetFloat("BestTime", bestTime);
        }

        if (highScoreText != null)
        {
            highScoreText.text = "HIGH SCORE: " + highScore.ToString("0000");
        }

        UpdateBestTimeDisplay();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            Time.timeScale = 0f;
        }

        PlayerPrefs.Save();
    }

    void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = "SCORE: " + currentScore.ToString("0000");
        }
    }

    void UpdateTimerDisplay()
    {
        if (timerText == null) return;
        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        timerText.text = $"{minutes:00}:{seconds:00}";
    }

    void UpdateBestTimeDisplay()
    {
        if (bestTimeText == null) return;
        int minutes = Mathf.FloorToInt(bestTime / 60);
        int seconds = Mathf.FloorToInt(bestTime % 60);
        bestTimeText.text = $"BEST TIME: {minutes:00}:{seconds:00}";
    }
}
