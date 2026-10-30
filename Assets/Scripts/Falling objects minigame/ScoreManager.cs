using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    private int currentScore = 0;
    private int bestScore = 0;

    private void OnEnable()
    {
        // Subscribe to the central event hub
        GameEventManager.OnItemCaught += HandleItemCaught;
    }

    private void OnDisable()
    {
        // Unsubscribe to avoid memory leaks
        GameEventManager.OnItemCaught -= HandleItemCaught;
    }

    private void Start()
    {
        // Load persistent high score from previous sessions
        bestScore = PlayerPrefs.GetInt("BestScore", 0);

        // Publish the starting values so the UI and difficulty stay in sync
        GameEventManager.TriggerScoreChanged(currentScore);
        GameEventManager.TriggerBestScoreChanged(bestScore);
    }

    /// <summary>Resets the run score. Useful when a restart flow is added.</summary>
    public void ResetScore()
    {
        currentScore = 0;
        GameEventManager.TriggerScoreChanged(currentScore);
        GameEventManager.TriggerBestScoreChanged(bestScore);
    }

    private void HandleItemCaught(FallingObject item)
    {
        // Only react if it's a Good item (Seal)
        if (item.itemType != FallingObject.ObjectType.Good) return;

        currentScore++;

        // UI, difficulty and anything else listening react to this
        GameEventManager.TriggerScoreChanged(currentScore);

        CheckForNewRecord();
    }

    private void CheckForNewRecord()
    {
        if (currentScore <= bestScore) return;

        bestScore = currentScore;
        PlayerPrefs.SetInt("BestScore", bestScore);
        PlayerPrefs.Save();

        GameEventManager.TriggerBestScoreChanged(bestScore);
        GameEventManager.TriggerNewRecordAchieved(bestScore);
    }
}
