using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Drives the main menu: starts the minigame, quits the game, and shows the
/// placeholder tutorial panel. The panel closes when the player clicks
/// anywhere (a drag/scroll does not close it) or presses Escape.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button startButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private Button tutorialButton;

    [Header("Tutorial Panel")]
    [Tooltip("Full screen panel shown by the '?' button. Starts hidden.")]
    [SerializeField] private GameObject tutorialPanel;

    [Tooltip("Scroll view inside the tutorial panel, used to reset it to the top.")]
    [SerializeField] private ScrollRect tutorialScroll;

    [Header("Scenes")]
    [Tooltip("Scene loaded by the Start button.")]
    [SerializeField] private string gameSceneName = "SampleScene";

    [Header("Tutorial Dismiss")]
    [SerializeField] private bool allowEscapeKey = true;

    [Tooltip("Mouse travel in pixels above which the click counts as a scroll drag instead of a dismiss click.")]
    [SerializeField] private float clickDragThreshold = 25f;

    private Vector3 pressPosition;
    private bool pressValid;
    private int ignoreClicksUntilFrame;

    private void Awake()
    {
        Time.timeScale = 1f;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        if (startButton != null) startButton.onClick.AddListener(StartGame);
        if (exitButton != null) exitButton.onClick.AddListener(ExitGame);
        if (tutorialButton != null) tutorialButton.onClick.AddListener(OpenTutorial);
    }

    private void OnDestroy()
    {
        if (startButton != null) startButton.onClick.RemoveListener(StartGame);
        if (exitButton != null) exitButton.onClick.RemoveListener(ExitGame);
        if (tutorialButton != null) tutorialButton.onClick.RemoveListener(OpenTutorial);
    }

    private void Update()
    {
        if (tutorialPanel == null || !tutorialPanel.activeSelf) return;

        if (allowEscapeKey && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseTutorial();
            return;
        }

        // The click that opened the panel is still reported on this frame, so ignore it.
        if (Time.frameCount <= ignoreClicksUntilFrame) return;

        if (Input.GetMouseButtonDown(0))
        {
            pressPosition = Input.mousePosition;
            pressValid = true;
            return;
        }

        if (!pressValid || !Input.GetMouseButtonUp(0)) return;

        pressValid = false;

        // A long press/drag is the player scrolling, not a click to dismiss.
        if ((Input.mousePosition - pressPosition).sqrMagnitude > clickDragThreshold * clickDragThreshold) return;

        // Let the scrollbar be used without closing the panel.
        if (IsPointerOverScrollbar()) return;

        CloseTutorial();
    }

    private void StartGame()
    {
        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogWarning("MainMenuManager: no game scene assigned to the Start button.");
            return;
        }

        if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
        {
            Debug.LogWarning($"MainMenuManager: scene '{gameSceneName}' is not in Build Settings.");
            return;
        }

        StartCoroutine(LoadAfterUiSettles(gameSceneName));
    }

    private void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OpenTutorial()
    {
        if (tutorialPanel == null) return;

        tutorialPanel.SetActive(true);
        pressValid = false;

        // The button's onClick fires on the same mouse-up that would dismiss the panel.
        ignoreClicksUntilFrame = Time.frameCount + 2;

        if (tutorialScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            tutorialScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void CloseTutorial()
    {
        pressValid = false;

        if (tutorialPanel != null) tutorialPanel.SetActive(false);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private bool IsPointerOverScrollbar()
    {
        if (EventSystem.current == null) return false;

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].gameObject != null && results[i].gameObject.GetComponentInParent<Scrollbar>() != null)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Loads on the next frame so the EventSystem is not holding a reference to
    /// UI that is being destroyed by the scene load.
    /// </summary>
    private IEnumerator LoadAfterUiSettles(string sceneName)
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        yield return null;
        SceneManager.LoadScene(sceneName);
    }
}
