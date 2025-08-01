using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageFlash : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Color flashColor = Color.red;
    public float flashDuration = 0.1f;

    private Color originalColor;
    private Coroutine flashRoutine;

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer)
            originalColor = spriteRenderer.color;
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(DoFlash());
    }

    private System.Collections.IEnumerator DoFlash()
    {
        spriteRenderer.color = flashColor;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.color = originalColor;
    }
}
