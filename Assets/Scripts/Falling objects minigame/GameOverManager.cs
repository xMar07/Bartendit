using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Owns the game over screen and the run-to-run flow.
/// It only listens to GameEventManager - it never reaches into other managers.
/// </summary>
public class GameOverManager : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Root of the game over UI. Hidden until the run ends.")]
    [SerializeField] private GameObject gameOverPanel;

    [Header("Buttons")]
    [SerializeField] private Button homeButton;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button nextSceneButton;

    [Header("Scene Names")]
    [Tooltip("Scene loaded by the Home button. Leave empty until the menu scene exists.")]
    [SerializeField] private string mainMenuSceneName = "MainMenu";

    [Tooltip("Scene loaded by the third button. Leave empty until that scene exists.")]
    [SerializeField] private string nextSceneName = "";

    [Header("Flow")]
    [Tooltip("Freeze the game (Time.timeScale = 0) while the panel is open.")]
    [SerializeField] private bool freezeGameOnGameOver = true;

    [Tooltip("Button selected when the panel opens, for keyboard / gamepad navigation.")]
    [SerializeField] private Button defaultSelectedButton;

    private void Awake()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (homeButton != null) homeButton.onClick.AddListener(GoToMainMenu);
        if (replayButton != null) replayButton.onClick.AddListener(Replay);
        if (nextSceneButton != null) nextSceneButton.onClick.AddListener(GoToNextScene);
    }

    private void OnDestroy()
    {
        if (homeButton != null) homeButton.onClick.RemoveListener(GoToMainMenu);
        if (replayButton != null) replayButton.onClick.RemoveListener(Replay);
        if (nextSceneButton != null) nextSceneButton.onClick.RemoveListener(GoToNextScene);
    }

    private void OnEnable()
    {
        GameEventManager.OnGameOver += HandleGameOver;
    }

    private void OnDisable()
    {
        GameEventManager.OnGameOver -= HandleGameOver;
    }

    private void HandleGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);

        if (freezeGameOnGameOver)
        {
            Time.timeScale = 0f;
        }

        SelectDefaultButton();
    }

    private void SelectDefaultButton()
    {
        Button target = defaultSelectedButton != null ? defaultSelectedButton : replayButton;

        if (target != null && EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(target.gameObject);
        }
    }

    /// <summary>
    /// Reloads the current scene. Score, health and difficulty restart from zero,
    /// while the best score stays because it lives in PlayerPrefs.
    /// </summary>
    private void Replay()
    {
        PrepareToLeaveScene();
        StartCoroutine(LoadAfterUiSettles(() => SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex)));
    }

    private void GoToMainMenu()
    {
        LoadSceneIfAvailable(mainMenuSceneName, "main menu");
    }

    private void GoToNextScene()
    {
        LoadSceneIfAvailable(nextSceneName, "third button");
    }

    private void LoadSceneIfAvailable(string sceneName, string buttonLabel)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"GameOverManager: no scene assigned for the {buttonLabel} yet.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"GameOverManager: scene '{sceneName}' is not in Build Settings.");
            return;
        }

        // Only unfreeze once we know the scene will actually load
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

    /// <summary>
    /// Cleans up UI state while every graphic still exists. Without this, the
    /// EventSystem can keep a reference to a selected button whose Image is
    /// destroyed during scene unload ("...Image has been destroyed...").
    /// </summary>
    private void PrepareToLeaveScene()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            gameOverPanel.SetActive(false);
        }
    }
}
