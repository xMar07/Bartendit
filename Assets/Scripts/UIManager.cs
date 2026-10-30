using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Single entry point for all on-screen UI driven by GameEventManager.
/// Add new UI details (combo text, difficulty label, popups...) here as new
/// handlers + serialized references - no other gameplay script needs to change.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("Score UI")]
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private TextMeshProUGUI bestScoreText;
    [SerializeField] private string scorePrefix = "SCORE : ";
    [SerializeField] private string bestScorePrefix = "BEST RECORD : ";

    [Header("Health UI")]
    [SerializeField] private Slider healthSlider;

    [Header("Coins UI")]
    [SerializeField] private TextMeshProUGUI coinsText;
    [SerializeField] private string coinsPrefix = "COINS : ";

    [Header("New Record Flash")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color recordFlashColor = Color.yellow;
    [SerializeField] private float flashDuration = 0.5f;

    [Header("Coins Feedback")]
    [Tooltip("How much the coin counter grows for a moment whenever coins are gained.")]
    [SerializeField] private float coinsPunchScale = 1.25f;
    [SerializeField] private float coinsPunchDuration = 0.12f;

    [Header("Pause Menu")]
    [Tooltip("Bottom-left button that opens the pause panel.")]
    [SerializeField] private Button pauseButton;

    [Tooltip("Root of the pause UI. Hidden until the pause button is pressed.")]
    [SerializeField] private GameObject pausePanel;

    [SerializeField] private Button pauseRestartButton;
    [SerializeField] private Button pauseHomeButton;
    [SerializeField] private Button pausePotionBarButton;

    [Tooltip("Button selected when the pause panel opens, for keyboard / gamepad navigation.")]
    [SerializeField] private Button pauseDefaultSelectedButton;

    [Header("Pause Scene Names")]
    [SerializeField] private string restartSceneName = "SampleScene";
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string potionBarSceneName = "PotionBar";

    private int currentScore;
    private int bestScore;
    private int currentHealth;
    private int maxHealth;
    private int currentCoins;

    private bool gameOverShown;

    private Coroutine flashRoutine;
    private Coroutine coinsPunchRoutine;

    private void Awake()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        if (pauseButton != null) pauseButton.onClick.AddListener(OpenPauseMenu);
        if (pauseRestartButton != null) pauseRestartButton.onClick.AddListener(RestartGame);
        if (pauseHomeButton != null) pauseHomeButton.onClick.AddListener(GoToMainMenu);
        if (pausePotionBarButton != null) pausePotionBarButton.onClick.AddListener(GoToPotionBar);
    }

    private void OnDestroy()
    {
        if (pauseButton != null) pauseButton.onClick.RemoveListener(OpenPauseMenu);
        if (pauseRestartButton != null) pauseRestartButton.onClick.RemoveListener(RestartGame);
        if (pauseHomeButton != null) pauseHomeButton.onClick.RemoveListener(GoToMainMenu);
        if (pausePotionBarButton != null) pausePotionBarButton.onClick.RemoveListener(GoToPotionBar);
    }

    private void OnEnable()
    {
        GameEventManager.OnScoreChanged += HandleScoreChanged;
        GameEventManager.OnBestScoreChanged += HandleBestScoreChanged;
        GameEventManager.OnNewRecordAchieved += HandleNewRecordAchieved;
        GameEventManager.OnHealthChanged += HandleHealthChanged;
        GameEventManager.OnCoinsChanged += HandleCoinsChanged;
        GameEventManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEventManager.OnScoreChanged -= HandleScoreChanged;
        GameEventManager.OnBestScoreChanged -= HandleBestScoreChanged;
        GameEventManager.OnNewRecordAchieved -= HandleNewRecordAchieved;
        GameEventManager.OnHealthChanged -= HandleHealthChanged;
        GameEventManager.OnCoinsChanged -= HandleCoinsChanged;
        GameEventManager.OnGameOver -= HandleGameOver;

        StopFlash();
        StopCoinsPunch();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Escape)) return;

        if (pausePanel != null && pausePanel.activeSelf)
        {
            ClosePauseMenu();
            return;
        }

        if (!gameOverShown && pausePanel != null)
        {
            OpenPauseMenu();
        }
    }

    private void Start()
    {
        // Draw the current cached values immediately so the UI is never empty
        RefreshScoreText();
        RefreshBestScoreText();
        RefreshHealthBar();
        RefreshCoinsText();
    }

    // ------------------------------------------------------------------
    // Event handlers
    // ------------------------------------------------------------------

    private void HandleScoreChanged(int score)
    {
        currentScore = score;
        RefreshScoreText();
    }

    private void HandleBestScoreChanged(int best)
    {
        bestScore = best;
        RefreshBestScoreText();
    }

    private void HandleNewRecordAchieved(int best)
    {
        bestScore = best;
        RefreshBestScoreText();

        if (bestScoreText == null) return;

        StopFlash();
        flashRoutine = StartCoroutine(FlashNewRecordEffect());
    }

    private void HandleHealthChanged(int health, int max)
    {
        currentHealth = health;
        maxHealth = max;
        RefreshHealthBar();
    }

    private void HandleCoinsChanged(int coins)
    {
        currentCoins = coins;
        RefreshCoinsText();

        if (coinsText == null) return;

        StopCoinsPunch();
        coinsPunchRoutine = StartCoroutine(PunchCoins());
    }

    private void HandleGameOver()
    {
        gameOverShown = true;

        // The game over panel takes over the screen, so the pause button is no longer useful.
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);

        if (pausePanel != null && pausePanel.activeSelf)
        {
            pausePanel.SetActive(false);
        }
    }

    // ------------------------------------------------------------------
    // Pause menu
    // ------------------------------------------------------------------

    public void OpenPauseMenu()
    {
        if (gameOverShown || pausePanel == null) return;

        // The panel overlay already blocks the button; hide it so it does not peek through.
        if (pauseButton != null) pauseButton.gameObject.SetActive(false);

        pausePanel.SetActive(true);
        Time.timeScale = 0f;
        SelectPauseDefaultButton();
    }

    public void ClosePauseMenu()
    {
        if (pausePanel != null) pausePanel.SetActive(false);

        if (pauseButton != null && !gameOverShown) pauseButton.gameObject.SetActive(true);

        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private void RestartGame()
    {
        string target = string.IsNullOrWhiteSpace(restartSceneName)
            ? SceneManager.GetActiveScene().name
            : restartSceneName;

        LoadSceneIfAvailable(target, "restart");
    }

    private void GoToMainMenu()
    {
        LoadSceneIfAvailable(mainMenuSceneName, "home");
    }

    private void GoToPotionBar()
    {
        LoadSceneIfAvailable(potionBarSceneName, "potion bar");
    }

    private void LoadSceneIfAvailable(string sceneName, string label)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"UIManager: no scene assigned for the {label} button.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"UIManager: scene '{sceneName}' is not in Build Settings.");
            return;
        }

        PrepareToLeaveScene();
        StartCoroutine(LoadAfterUiSettles(() => SceneManager.LoadScene(sceneName)));
    }

    /// <summary>
    /// Loads on the next frame. Loading synchronously from inside a Button.onClick
    /// handler leaves the EventSystem holding a reference to UI that is being
    /// destroyed, which throws "...Image has been destroyed..." warnings.
    /// </summary>
    private IEnumerator LoadAfterUiSettles(Action loadAction)
    {
        yield return null;
        loadAction();
    }

    private void PrepareToLeaveScene()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (pausePanel != null && pausePanel.activeSelf)
        {
            pausePanel.SetActive(false);
        }
    }

    private void SelectPauseDefaultButton()
    {
        Button target = pauseDefaultSelectedButton != null ? pauseDefaultSelectedButton : pauseRestartButton;

        if (target != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }

    // ------------------------------------------------------------------
    // Rendering
    // ------------------------------------------------------------------

    private void RefreshScoreText()
    {
        if (scoreText != null)
        {
            scoreText.text = scorePrefix + currentScore;
        }
    }

    private void RefreshBestScoreText()
    {
        if (bestScoreText != null)
        {
            bestScoreText.text = bestScorePrefix + bestScore;
        }
    }

    private void RefreshHealthBar()
    {
        if (healthSlider == null) return;

        healthSlider.value = maxHealth > 0 ? (float)currentHealth / maxHealth : 0f;
    }

    private void RefreshCoinsText()
    {
        if (coinsText != null)
        {
            coinsText.text = coinsPrefix + currentCoins;
        }
    }

    private IEnumerator PunchCoins()
    {
        coinsText.transform.localScale = Vector3.one * coinsPunchScale;

        yield return new WaitForSeconds(coinsPunchDuration);

        coinsText.transform.localScale = Vector3.one;
        coinsPunchRoutine = null;
    }

    private void StopCoinsPunch()
    {
        if (coinsPunchRoutine != null)
        {
            StopCoroutine(coinsPunchRoutine);
            coinsPunchRoutine = null;
        }

        if (coinsText != null)
        {
            coinsText.transform.localScale = Vector3.one;
        }
    }

    private IEnumerator FlashNewRecordEffect()
    {
        bestScoreText.color = recordFlashColor;
        bestScoreText.transform.localScale = Vector3.one * 1.2f;

        yield return new WaitForSeconds(flashDuration);

        ResetBestScoreVisuals();
        flashRoutine = null;
    }

    private void StopFlash()
    {
        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
            flashRoutine = null;
        }

        ResetBestScoreVisuals();
    }

    private void ResetBestScoreVisuals()
    {
        if (bestScoreText == null) return;

        bestScoreText.color = normalColor;
        bestScoreText.transform.localScale = Vector3.one;
    }
}
