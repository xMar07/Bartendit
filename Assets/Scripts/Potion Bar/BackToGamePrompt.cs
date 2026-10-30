using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles the "go back?" icon in the Potion Bar scene.
/// The icon opens a confirm dialog; Yes returns to the catching minigame
/// (which restarts from the beginning because the scene is reloaded fresh).
/// </summary>
public class BackToGamePrompt : MonoBehaviour
{
    [Header("Confirm Dialog")]
    [Tooltip("Root of the confirmation dialog. Hidden on start.")]
    [SerializeField] private GameObject confirmPanel;

    [Header("Buttons")]
    [SerializeField] private Button backIconButton;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("Scenes")]
    [Tooltip("The catching minigame scene that Yes loads.")]
    [SerializeField] private string minigameSceneName = "SampleScene";

    [Header("Input")]
    [SerializeField] private bool allowEscapeKey = true;

    private void Awake()
    {
        // Safety: never arrive here while the previous scene left time frozen
        Time.timeScale = 1f;

        if (confirmPanel != null) confirmPanel.SetActive(false);

        if (backIconButton != null) backIconButton.onClick.AddListener(ShowPrompt);
        if (yesButton != null) yesButton.onClick.AddListener(ConfirmReturnToMinigame);
        if (noButton != null) noButton.onClick.AddListener(HidePrompt);
    }

    private void OnDestroy()
    {
        if (backIconButton != null) backIconButton.onClick.RemoveListener(ShowPrompt);
        if (yesButton != null) yesButton.onClick.RemoveListener(ConfirmReturnToMinigame);
        if (noButton != null) noButton.onClick.RemoveListener(HidePrompt);
    }

    private void Update()
    {
        if (!allowEscapeKey) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePrompt();
        }
    }

    public void TogglePrompt()
    {
        if (confirmPanel == null) return;

        if (confirmPanel.activeSelf) HidePrompt();
        else ShowPrompt();
    }

    public void ShowPrompt()
    {
        if (confirmPanel == null) return;

        confirmPanel.SetActive(true);

        // Focus "No" so an accidental Enter/Space never leaves the scene
        if (EventSystem.current != null && noButton != null)
        {
            EventSystem.current.SetSelectedGameObject(noButton.gameObject);
        }
    }

    public void HidePrompt()
    {
        if (confirmPanel == null) return;

        confirmPanel.SetActive(false);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    public void ConfirmReturnToMinigame()
    {
        if (!Application.CanStreamedLevelBeLoaded(minigameSceneName))
        {
            Debug.LogWarning($"BackToGamePrompt: scene '{minigameSceneName}' is not in Build Settings.");
            return;
        }

        PrepareToLeaveScene();

        // Reloading the scene restarts the minigame from scratch.
        // Deferred one frame so the click finishes on live UI objects.
        StartCoroutine(LoadAfterUiSettles(() => SceneManager.LoadScene(minigameSceneName)));
    }

    private IEnumerator LoadAfterUiSettles(Action loadAction)
    {
        yield return null;
        loadAction();
    }

    /// <summary>
    /// Clears selection and hides the dialog while its graphics still exist,
    /// so the EventSystem never touches a destroyed UI Image during unload.
    /// </summary>
    private void PrepareToLeaveScene()
    {
        Time.timeScale = 1f;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }

        if (confirmPanel != null)
        {
            confirmPanel.SetActive(false);
        }
    }
}
