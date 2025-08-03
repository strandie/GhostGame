using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewindUI : MonoBehaviour
{
    public static RewindUI Instance;
    
    [Header("UI References")]
    public Image abilityIcon;           // The ability icon
    public Image cooldownOverlay;       // Gray overlay that fills during cooldown
    public TextMeshProUGUI cooldownText; // Text showing remaining time
    
    [Header("Visual Settings")]
    public Color readyColor = Color.white;
    public Color cooldownColor = Color.gray;
    public Sprite rewindIconSprite;     // Assign your rewind ability icon
    
    [Header("Animation Settings")]
    public bool animateIcon = true;
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.2f;
    
    [Header("Audio")]
    public AudioClip readySound;        // Sound when ability comes off cooldown
    public AudioClip activateSound;     // Sound when ability is used
    
    private bool isOnCooldown = false;
    private CanvasGroup canvasGroup;
    private AudioSource audioSource;
    private bool wasOnCooldown = false; // Track state changes
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        audioSource = GetComponent<AudioSource>();
        
        // Add AudioSource if it doesn't exist
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
        
        // Setup initial state
        if (abilityIcon != null && rewindIconSprite != null)
        {
            abilityIcon.sprite = rewindIconSprite;
        }
        
        // Initialize UI as ready
        UpdateCooldown(0f, 10f, false);
    }
    
    void Update()
    {
        // Animate icon when ready
        if (!isOnCooldown && animateIcon && abilityIcon != null)
        {
            float pulse = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseIntensity;
            abilityIcon.transform.localScale = Vector3.one * pulse;
        }
        
        // Handle keyboard input
        if (Input.GetKeyDown(KeyCode.Mouse1) && !isOnCooldown)
        {
            OnAbilityButtonClick();
        }
    }
    
    public void UpdateCooldown(float remainingTime, float totalCooldown, bool onCooldown)
    {
        // Check if we just came off cooldown
        if (wasOnCooldown && !onCooldown)
        {
            PlayReadyEffect();
        }
        
        isOnCooldown = onCooldown;
        wasOnCooldown = onCooldown;
        
        if (onCooldown)
        {
            // On cooldown
            if (abilityIcon != null)
            {
                abilityIcon.color = cooldownColor;
                abilityIcon.transform.localScale = Vector3.one; // Stop pulse animation
            }
            
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(true);
                float fillAmount = remainingTime / totalCooldown;
                cooldownOverlay.fillAmount = fillAmount;
            }
            
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(true);
                // Show decimal for last second, whole numbers otherwise
                if (remainingTime <= 1f)
                {
                    cooldownText.text = remainingTime.ToString("F1");
                }
                else
                {
                    cooldownText.text = Mathf.Ceil(remainingTime).ToString();
                }
            }
            

        }
        else
        {
            // Ready to use
            if (abilityIcon != null)
            {
                abilityIcon.color = readyColor;
            }
            
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(false);
            }
            
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
    
    private void OnAbilityButtonClick()
    {
        if (!isOnCooldown && TimeManager.Instance != null)
        {
            // Play activation sound
            if (audioSource != null && activateSound != null)
            {
                audioSource.PlayOneShot(activateSound);
            }
            
            // Trigger rewind through TimeManager
            TimeManager.Instance.TryStartRewind();
        }
    }
    
    public void SetVisible(bool visible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
        else
        {
            gameObject.SetActive(visible);
        }
    }
    
    public void PlayReadyEffect()
    {
        // Play ready sound
        if (audioSource != null && readySound != null)
        {
            audioSource.PlayOneShot(readySound);
        }
        
        // Visual flash effect
        if (abilityIcon != null)
        {
            StartCoroutine(ReadyFlashEffect());
        }
    }
    
    private System.Collections.IEnumerator ReadyFlashEffect()
    {
        if (abilityIcon == null) yield break;
        
        Color originalColor = abilityIcon.color;
        Vector3 originalScale = abilityIcon.transform.localScale;
        
        // Brief flash and scale effect
        abilityIcon.color = Color.white;
        abilityIcon.transform.localScale = originalScale * 1.1f;
        
        float flashTime = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < flashTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashTime;
            
            abilityIcon.color = Color.Lerp(Color.white, originalColor, t);
            abilityIcon.transform.localScale = Vector3.Lerp(originalScale * 1.1f, originalScale, t);
            
            yield return null;
        }
        
        abilityIcon.color = originalColor;
        abilityIcon.transform.localScale = originalScale;
    }
}