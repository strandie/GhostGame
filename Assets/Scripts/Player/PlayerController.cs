using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Data Source")]
    public PlayerData playerData;

    [Header("Movement Settings")]
    public float moveSpeed = 5f;

    [Header("Rotation Settings")]
    public bool faceMouse = true;

    [Header("References")]
    public Transform gunTransform;
    public SpriteRenderer playerSpriteRenderer;
    public SpriteRenderer gunSpriteRenderer;

    [Header("Shooting Settings")]
    public GameObject bulletPrefab;
    public Transform firePoint; // assign an empty GameObject at barrel tip

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Camera mainCam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;

        if (playerData != null)
        {
            moveSpeed = playerData.moveSpeed;
            faceMouse = playerData.faceMouse;
        }
        else
        {
            Debug.LogWarning("PlayerData not assigned to PlayerController.");
        }
    }

    void Update()
    {
        ProcessInputs();
        HandleFlip();
        RotateGunToMouse();
    }

    void FixedUpdate()
    {
        Move();
    }

    private void ProcessInputs()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput.Normalize(); // consistent diagonal speed

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
        }

    }

    private void Move()
    {
        Vector2 newPosition = rb.position + movementInput * moveSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPosition);
    }

    private void HandleFlip()
    {
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        bool isMouseLeft = mouseWorldPos.x < transform.position.x;

        playerSpriteRenderer.flipX = isMouseLeft;

        Vector3 gunScale = gunTransform.localScale;
        gunScale.y = isMouseLeft ? -1 : 1;
        gunTransform.localScale = gunScale;
    }

    private void RotateGunToMouse()
    {
        if (!faceMouse) return;

        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mouseWorldPos - gunTransform.position;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        gunTransform.rotation = Quaternion.Euler(0, 0, angle);
    }

    private void Shoot()
    {
        if (bulletPrefab == null || firePoint == null) return;

        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 rawDirection = mouseWorldPos - firePoint.position;

        Vector2 shootDirection;

        if (rawDirection.sqrMagnitude < 0.01f)
            shootDirection = gunTransform.right;
        else
            shootDirection = rawDirection.normalized;

        // Offset bullet spawn forward by 0.2 units (adjust if needed)
        Vector2 spawnPos = (Vector2)firePoint.position + shootDirection * 0.2f;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        PlayerBullet bulletScript = bullet.GetComponent<PlayerBullet>();
        if (bulletScript != null)
            bulletScript.Initialize(shootDirection);
    }


}
