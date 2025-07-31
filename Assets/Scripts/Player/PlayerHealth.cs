using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    private Health health;

    void Awake()
    {
        health = GetComponent<Health>();
    }

    public void TakeDamage(float damage)
    {
        health.TakeDamage(damage);
        Debug.Log("Player took damage!");
    }
}

