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

    [Range(0f, 1f)]
    public float accuracy = 0.7f; // 1 good 0 bad
    public float maxSpreadAngle = 15f;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Camera mainCam;

    private Animator animator;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        //animator = GetComponent<Animator>();
        //animator = playerSpriteRenderer.GetComponent<Animator>();
        animator = GetComponentInChildren<Animator>();

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
        animator.SetBool("IsRunning", movementInput.sqrMagnitude > 0.01f);
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

        Vector3 gunLocalPos = gunTransform.localPosition;
        gunLocalPos.x = Mathf.Abs(gunLocalPos.x) * (isMouseLeft ? -1 : 1);
        gunTransform.localPosition = gunLocalPos;

        gunTransform.localScale = Vector3.one;

        if (gunSpriteRenderer != null)
            gunSpriteRenderer.flipY = isMouseLeft;

        Vector3 firePointLocalPos = firePoint.localPosition;
        firePointLocalPos.y = Mathf.Abs(firePointLocalPos.y) * (isMouseLeft ? -1 : 1);
        firePoint.localPosition = firePointLocalPos;
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

        float minShootRadius = 1.0f;

        Vector2 shootDirection;

        if (rawDirection.sqrMagnitude < minShootRadius * minShootRadius)
        {
            shootDirection = gunTransform.right;
        }
        else
        {
            shootDirection = rawDirection.normalized;
        }

        float spreadAngle = Mathf.Lerp(maxSpreadAngle, 0f, accuracy);
        float randomOffset = Random.Range(-spreadAngle / 2f, spreadAngle / 2f);

        shootDirection = Quaternion.Euler(0, 0, randomOffset) * shootDirection;

        Vector2 spawnPos = (Vector2)firePoint.position + shootDirection * 0.2f;

        GameObject bullet = Instantiate(bulletPrefab, spawnPos, Quaternion.identity);

        PlayerBullet bulletScript = bullet.GetComponent<PlayerBullet>();
        if (bulletScript != null)
            bulletScript.Initialize(shootDirection);
    }

}
