using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBarUI : MonoBehaviour
{
    public Health health;
    public Slider slider;

    void Start()
    {
        if (health != null)
        {
            health.onHealthChanged.AddListener(UpdateBar);
        }
        UpdateBar(1f); // full on start
    }

    void UpdateBar(float normalizedHealth)
    {
        slider.value = normalizedHealth;
    }
}

