using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI sizeText;
    public TextMeshProUGUI goalSizeText;
    public GameObject gameCompletePanel;
    public GameObject gameOverPanel;
    public Button restartButton;
    
    [Header("Game Settings")]
    public float gameTime = 120f; // 2 minutes
    public float targetSize = 5f; // Size needed to win
    
    [Header("Size Animation")]
    public float animationDuration = 0.3f;
    public float punchScale = 1.2f; // How much to scale up during punch
    public Color sizeIncreaseColor = Color.yellow;
    public Color normalSizeColor = Color.white;
    
    private PlayerController playerController;
    private float currentTime;
    private bool gameActive = true;
    private bool gameWon = false;
    private Coroutine sizeAnimationCoroutine;
    
    // Game states
    public enum GameState
    {
        Playing,
        GameComplete,
        GameOver
    }
    
    private GameState currentGameState = GameState.Playing;
    
    void Start()
    {
        // Auto-setup UI references if not assigned
        AutoSetupUIReferences();
        
        // Find player controller
        playerController = FindObjectOfType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogError("UIManager: PlayerController not found!");
            return;
        }
        
        // Initialize UI
        currentTime = gameTime;
        UpdateTimerDisplay();
        UpdateSizeDisplay();
        UpdateGoalSizeDisplay();
        
        // Setup restart button
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartGame);
        }
        
        // Hide panels initially
        if (gameCompletePanel != null)
            gameCompletePanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    }
    
    void AutoSetupUIReferences()
    {
        // Find UI elements by name if not already assigned
        if (timerText == null)
        {
            Transform timerTextTransform = transform.Find("TimerPanel/TimerText");
            if (timerTextTransform != null)
            {
                timerText = timerTextTransform.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Auto-setup: TimerText reference found");
            }
        }
        
        if (sizeText == null)
        {
            Transform sizeTextTransform = transform.Find("SizePanel/SizeText");
            if (sizeTextTransform != null)
            {
                sizeText = sizeTextTransform.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Auto-setup: SizeText reference found");
            }
        }
        
        if (goalSizeText == null)
        {
            Transform goalSizeTextTransform = transform.Find("TimerPanel/GoalSizeText");
            if (goalSizeTextTransform != null)
            {
                goalSizeText = goalSizeTextTransform.GetComponent<TMPro.TextMeshProUGUI>();
                Debug.Log("Auto-setup: GoalSizeText reference found");
            }
        }
        
        if (gameCompletePanel == null)
        {
            Transform gameCompletePanelTransform = transform.Find("GameCompletePanel");
            if (gameCompletePanelTransform != null)
            {
                gameCompletePanel = gameCompletePanelTransform.gameObject;
                Debug.Log("Auto-setup: GameCompletePanel reference found");
            }
        }
        
        if (gameOverPanel == null)
        {
            Transform gameOverPanelTransform = transform.Find("GameOverPanel");
            if (gameOverPanelTransform != null)
            {
                gameOverPanel = gameOverPanelTransform.gameObject;
                Debug.Log("Auto-setup: GameOverPanel reference found");
            }
        }
        
        if (restartButton == null)
        {
            Transform restartButtonTransform = transform.Find("GameCompletePanel/RestartButton");
            if (restartButtonTransform != null)
            {
                restartButton = restartButtonTransform.GetComponent<UnityEngine.UI.Button>();
                Debug.Log("Auto-setup: RestartButton reference found");
            }
        }
    }
    
    void Update()
    {
        if (!gameActive) return;
        
        // Update timer
        currentTime -= Time.deltaTime;
        UpdateTimerDisplay();
        
        // Check win condition
        if (!gameWon && playerController.GetCurrentSize() >= targetSize)
        {
            GameComplete();
        }
        
        // Check lose condition
        if (currentTime <= 0f)
        {
            GameOver();
        }
    }
    
    void UpdateTimerDisplay()
    {
        if (timerText != null)
        {
            int minutes = Mathf.FloorToInt(currentTime / 60f);
            int seconds = Mathf.FloorToInt(currentTime % 60f);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            
            // Change color when time is running low
            if (currentTime <= 30f)
            {
                timerText.color = Color.red;
            }
            else if (currentTime <= 60f)
            {
                timerText.color = Color.yellow;
            }
            else
            {
                timerText.color = Color.white;
            }
        }
    }
    
    void UpdateSizeDisplay()
    {
        if (sizeText != null && playerController != null)
        {
            float currentSize = playerController.GetCurrentSize();
            sizeText.text = $"Size: {currentSize:F1}";
        }
    }
    
    void UpdateGoalSizeDisplay()
    {
        if (goalSizeText != null)
        {
            goalSizeText.text = $"Goal Size: {targetSize}";
        }
    }
    
    public void OnPlayerSizeChanged()
    {
        UpdateSizeDisplay();
        AnimateSizeText();
    }
    
    void AnimateSizeText()
    {
        if (sizeAnimationCoroutine != null)
        {
            StopCoroutine(sizeAnimationCoroutine);
            Debug.Log("Stopping previous size text animation");
        }
        Debug.Log($"Starting size text animation. Current scale: {sizeText.transform.localScale}");
        sizeAnimationCoroutine = StartCoroutine(AnimateSizeTextCoroutine());
    }
    
    IEnumerator AnimateSizeTextCoroutine()
    {
        if (sizeText == null) yield break;
        
        // Always use Vector3.one as the base scale to ensure we return to normal size
        Vector3 baseScale = Vector3.one;
        Color originalColor = sizeText.color;
        
        float elapsed = 0f;
        
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / animationDuration;
            
            // Create punch scale animation that goes up and back down
            // Use a curve that peaks early and returns to 1
            float scaleProgress;
            if (progress < 0.5f)
            {
                // First half: scale up to punchScale
                scaleProgress = Mathf.Lerp(1f, punchScale, progress * 2f);
            }
            else
            {
                // Second half: scale back down to 1
                scaleProgress = Mathf.Lerp(punchScale, 1f, (progress - 0.5f) * 2f);
            }
            
            sizeText.transform.localScale = baseScale * scaleProgress;
            
            // Color animation - fade from yellow back to white
            sizeText.color = Color.Lerp(sizeIncreaseColor, originalColor, progress);
            
            yield return null;
        }
        
        // Ensure we end exactly at base scale (Vector3.one)
        sizeText.transform.localScale = baseScale;
        sizeText.color = originalColor;
        sizeAnimationCoroutine = null;
        
        Debug.Log($"Size text animation completed. Final scale: {sizeText.transform.localScale}");
    }
    
    void GameComplete()
    {
        gameWon = true;
        gameActive = false;
        currentGameState = GameState.GameComplete;
        
        // Disable player input
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        if (gameCompletePanel != null)
        {
            gameCompletePanel.SetActive(true);
        }
        
        Debug.Log("Game Complete! Player reached target size!");
    }
    
    void GameOver()
    {
        gameActive = false;
        currentGameState = GameState.GameOver;
        
        // Set timer to 0
        currentTime = 0f;
        UpdateTimerDisplay();
        
        // Disable player input
        if (playerController != null)
        {
            playerController.enabled = false;
        }
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }
        
        Debug.Log("Game Over! Time ran out!");
    }
    
    public void RestartGame()
    {
        // Reset game state
        currentTime = gameTime;
        gameActive = true;
        gameWon = false;
        currentGameState = GameState.Playing;
        
        // Hide panels
        if (gameCompletePanel != null)
            gameCompletePanel.SetActive(false);
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        
        // Reset player
        if (playerController != null)
        {
            playerController.enabled = true; // Re-enable player input
            playerController.totalPoints = 0f;
            playerController.DropAllPickups();
        }
        
        // Reset UI
        UpdateTimerDisplay();
        UpdateSizeDisplay();
        
        Debug.Log("Game Restarted!");
    }
    
    public GameState GetCurrentGameState()
    {
        return currentGameState;
    }
    
    public bool IsGameActive()
    {
        return gameActive;
    }
    
    public float GetRemainingTime()
    {
        return currentTime;
    }
}