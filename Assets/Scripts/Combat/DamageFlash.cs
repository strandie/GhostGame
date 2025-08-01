using UnityEngine;
using System.Collections;

public class DamageFlash : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Material flashMaterial; // Use a URP shader graph material that’s solid white
    public float flashDuration = 0.1f;

    private Material originalMaterial;
    private Coroutine flashRoutine;

    void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (spriteRenderer)
            originalMaterial = spriteRenderer.material;
    }

    public void Flash()
    {
        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(DoFlash());
    }

    private IEnumerator DoFlash()
    {
        spriteRenderer.material = flashMaterial;
        yield return new WaitForSeconds(flashDuration);
        spriteRenderer.material = originalMaterial;
    }
}
