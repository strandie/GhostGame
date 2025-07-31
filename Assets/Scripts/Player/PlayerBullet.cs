using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBullet : MonoBehaviour
{
    public float speed = 10f;
    public float lifeTime = 2f;

    private Vector2 direction;

    public void Initialize(Vector2 shootDirection)
    {
        direction = shootDirection.normalized;

        // Rotate the bullet sprite to face direction
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);

        Destroy(gameObject, lifeTime);
    }


    void Update()
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Destroy on collision or add logic to damage enemies
        Destroy(gameObject);
    }
}
