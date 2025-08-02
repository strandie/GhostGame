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
    public Button abilityButton;        // Optional button for clicking
    
    [Header("Visual Settings")]
    public Color readyColor = Color.white;
    public Color cooldownColor = Color.gray;
    public Sprite rewindIconSprite;     // Assign your rewind ability icon
    
    [Header("Animation Settings")]
    public bool animateIcon = true;
    public float pulseSpeed = 2f;
    public float pulseIntensity = 0.2f;
    
    private bool isOnCooldown = false;
    private CanvasGroup canvasGroup;
    
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
        
        // Setup initial state
        if (abilityIcon != null && rewindIconSprite != null)
        {
            abilityIcon.sprite = rewindIconSprite;
        }
        
        // Setup button click if button exists
        if (abilityButton != null)
        {
            abilityButton.onClick.AddListener(OnAbilityButtonClick);
        }
        
        // Initialize UI
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
    }
    
    public void UpdateCooldown(float remainingTime, float totalCooldown, bool onCooldown)
    {
        isOnCooldown = onCooldown;
        
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
                cooldownText.text = Mathf.Ceil(remainingTime).ToString();
            }
            
            if (abilityButton != null)
            {
                abilityButton.interactable = false;
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
            
            if (abilityButton != null)
            {
                abilityButton.interactable = true;
            }
        }
    }
    
    private void OnAbilityButtonClick()
    {
        if (!isOnCooldown && TimeManager.Instance != null)
        {
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
        // Optional: Add particle effects, sound, etc. when ability comes off cooldown
        if (abilityIcon != null)
        {
            // You could add a brief flash or scale effect here
            StartCoroutine(ReadyFlashEffect());
        }
    }
    
    private System.Collections.IEnumerator ReadyFlashEffect()
    {
        if (abilityIcon == null) yield break;
        
        Color originalColor = abilityIcon.color;
        abilityIcon.color = Color.white;
        
        float flashTime = 0.3f;
        float elapsed = 0f;
        
        while (elapsed < flashTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / flashTime;
            abilityIcon.color = Color.Lerp(Color.white, originalColor, t);
            yield return null;
        }
        
        abilityIcon.color = originalColor;
    }
}
