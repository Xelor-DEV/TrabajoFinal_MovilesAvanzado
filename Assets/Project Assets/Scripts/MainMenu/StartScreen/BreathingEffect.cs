using UnityEngine;
using DG.Tweening;

public class BreathingEffect : MonoBehaviour
{
    [Header("Breathing Settings")]
    [Tooltip("Multiplier applied to the original scale during breathing")]
    public float scaleMultiplier = 1.2f;
    
    [Tooltip("Duration in seconds for scaling up and down")]
    public float scaleDuration = 0.8f;
    
    [Tooltip("Delay in seconds at peak scale before scaling back")]
    public float peakDelay = 0.3f;
    
    [Tooltip("The ease type for scaling animations")]
    public Ease easeType = Ease.InOutSine;

    private Vector3 originalScale;
    private Sequence breathingSequence;

    void Start()
    {
        // Store the original scale
        originalScale = transform.localScale;
        
        // Create the breathing sequence
        CreateBreathingSequence();
    }

    void CreateBreathingSequence()
    {
        // Calculate the target scale
        Vector3 targetScale = originalScale * scaleMultiplier;
        
        // Create new sequence
        breathingSequence = DOTween.Sequence();
        
        // Scale up to target
        breathingSequence.Append(transform.DOScale(targetScale, scaleDuration).SetEase(easeType));
        
        // Add delay at peak scale
        breathingSequence.AppendInterval(peakDelay);
        
        // Scale back to original
        breathingSequence.Append(transform.DOScale(originalScale, scaleDuration).SetEase(easeType));
        
        // Add delay at original scale before restarting (optional)
        // breathingSequence.AppendInterval(0.1f);
        
        // Set infinite loops
        breathingSequence.SetLoops(-1, LoopType.Restart);
        
        // Play the sequence
        breathingSequence.Play();
    }

    // Call this method if you need to update parameters at runtime
    public void UpdateBreathingParameters(float newMultiplier, float newDuration, float newDelay)
    {
        // Kill existing sequence
        if (breathingSequence != null && breathingSequence.IsActive())
            breathingSequence.Kill();
        
        // Update parameters
        scaleMultiplier = newMultiplier;
        scaleDuration = newDuration;
        peakDelay = newDelay;
        
        // Create new sequence with updated parameters
        CreateBreathingSequence();
    }

    // Restart the breathing effect
    public void RestartBreathing()
    {
        if (breathingSequence != null && breathingSequence.IsActive())
            breathingSequence.Restart();
    }

    // Pause the breathing effect
    public void PauseBreathing()
    {
        if (breathingSequence != null && breathingSequence.IsActive())
            breathingSequence.Pause();
    }

    // Resume the breathing effect
    public void ResumeBreathing()
    {
        if (breathingSequence != null && breathingSequence.IsActive())
            breathingSequence.Play();
    }

    void OnDestroy()
    {
        // Clean up tweens when object is destroyed
        if (breathingSequence != null && breathingSequence.IsActive())
            breathingSequence.Kill();
    }
}