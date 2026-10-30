using UnityEngine;

/// <summary>
/// Turns the player's score into a falling speed multiplier.
/// Ramps up in two segments, then locks at the maximum multiplier.
/// </summary>
public class DifficultyManager : MonoBehaviour
{
    [Header("Speed Curve")]
    [Tooltip("Multiplier at score 0.")]
    [SerializeField] private float startMultiplier = 1f;

    [Header("First Milestone")]
    [Tooltip("Score at which the first speed milestone is reached.")]
    [SerializeField] private int firstMilestoneScore = 50;

    [Tooltip("Speed multiplier reached at the first milestone.")]
    [SerializeField] private float firstMilestoneMultiplier = 1.4f;

    [Header("Maximum Difficulty")]
    [Tooltip("Score at which the speed caps out.")]
    [SerializeField] private int maxSpeedScore = 150;

    [Tooltip("Hard cap on the falling speed multiplier.")]
    [SerializeField] private float maxMultiplier = 2f;

    [Header("Debug")]
    [SerializeField] private bool logDifficultyChanges = false;

    private float currentMultiplier = -1f;

    private void OnEnable()
    {
        GameEventManager.OnScoreChanged += HandleScoreChanged;
    }

    private void OnDisable()
    {
        GameEventManager.OnScoreChanged -= HandleScoreChanged;
    }

    private void Start()
    {
        // Always begin a run at the starting difficulty (also resets static state)
        ApplyMultiplier(startMultiplier);
    }

    private void HandleScoreChanged(int currentScore)
    {
        ApplyMultiplier(CalculateMultiplier(currentScore));
    }

    private float CalculateMultiplier(int score)
    {
        // Segment 1: score 0 -> first milestone
        if (score <= firstMilestoneScore)
        {
            float t = Mathf.InverseLerp(0f, Mathf.Max(1f, firstMilestoneScore), score);
            return Mathf.Lerp(startMultiplier, firstMilestoneMultiplier, t);
        }

        // Segment 2: first milestone -> max speed score
        if (score <= maxSpeedScore)
        {
            float t = Mathf.InverseLerp(firstMilestoneScore, Mathf.Max(firstMilestoneScore + 1, maxSpeedScore), score);
            return Mathf.Lerp(firstMilestoneMultiplier, maxMultiplier, t);
        }

        // Stabilised at the maximum
        return maxMultiplier;
    }

    private void ApplyMultiplier(float multiplier)
    {
        multiplier = Mathf.Clamp(multiplier, startMultiplier, Mathf.Max(startMultiplier, maxMultiplier));

        if (Mathf.Approximately(multiplier, currentMultiplier)) return;

        currentMultiplier = multiplier;
        GameEventManager.TriggerSpeedMultiplierChanged(multiplier);

        if (logDifficultyChanges)
        {
            Debug.Log("Difficulty: speed x" + multiplier.ToString("0.00"));
        }
    }
}
