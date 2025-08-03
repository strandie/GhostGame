using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour, ITimeRewindable
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
    public Transform firePoint;

    [Range(0f, 1f)]
    public float accuracy = 0.7f;
    public float maxSpreadAngle = 15f;

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Camera mainCam;
    private Animator animator;
    private PlayerHealth playerHealth;
    private Health health; // For compatibility with both Health types
    private string entityId;

    [Header("Recording")]
    public PlayerRecorder playerRecorder;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        animator = GetComponentInChildren<Animator>();
        playerHealth = GetComponent<PlayerHealth>();
        health = GetComponent<Health>(); // Try both types
        entityId = System.Guid.NewGuid().ToString();

        if (playerData != null)
        {
            moveSpeed = playerData.moveSpeed;
            faceMouse = playerData.faceMouse;
        }
        else
        {
            Debug.LogWarning("PlayerData not assigned to PlayerController.");
        }

        if (playerRecorder == null)
            playerRecorder = GetComponent<PlayerRecorder>();

        // CRITICAL: Register with TimeManager
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.RegisterEntity(this);
            Debug.Log("Player registered with TimeManager");
        }
        else
        {
            Debug.LogError("TimeManager.Instance is null! Make sure TimeManager exists in scene.");
        }
    }

    void Update()
    {
        ProcessInputs();
        if (animator != null)
            animator.SetBool("IsRunning", movementInput.sqrMagnitude > 0.01f);
        HandleFlip();
        RotateGunToMouse();
    }

    void FixedUpdate()
    {
        Move();
    }

    void OnDestroy()
    {
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.UnregisterEntity(this);
        }
    }

    private void ProcessInputs()
    {
        movementInput.x = Input.GetAxisRaw("Horizontal");
        movementInput.y = Input.GetAxisRaw("Vertical");
        movementInput.Normalize();

        if (Input.GetMouseButtonDown(0))
        {
            Shoot();
            AudioSource audio = GetComponent<AudioSource>();
            if (audio != null)
                audio.Play();
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

        RewindablePlayerBullet bulletScript = bullet.GetComponent<RewindablePlayerBullet>();
        if (bulletScript != null)
            bulletScript.Initialize(shootDirection);
        
        if (playerRecorder != null)
            playerRecorder.NotifyShoot(shootDirection);
    }

    // ITimeRewindable implementation
    public string GetEntityId()
    {
        return entityId;
    }
    
    public EntitySnapshot TakeSnapshot()
    {
        EntitySnapshot snapshot = new EntitySnapshot(
            entityId,
            transform.position,
            transform.rotation,
            rb.velocity,
            gameObject.activeInHierarchy,
            GetCurrentHealth()
        );
        
        // Add player-specific data
        snapshot.flipX = playerSpriteRenderer.flipX;
        snapshot.isRunning = movementInput.sqrMagnitude > 0.01f;
        snapshot.gunRotation = gunTransform.eulerAngles;
        
        return snapshot;
    }
    
    public BulletSnapshot TakeBulletSnapshot()
    {
        return null;
    }
    
    public void RestoreFromSnapshot(EntitySnapshot snapshot)
    {
        // Restore position and state
        transform.position = snapshot.position;
        transform.rotation = snapshot.rotation;
        rb.velocity = snapshot.velocity;
        
        // Restore player-specific data
        if (playerSpriteRenderer != null)
            playerSpriteRenderer.flipX = snapshot.flipX;
            
        if (animator != null)
            animator.SetBool("IsRunning", snapshot.isRunning);
            
        if (gunTransform != null)
            gunTransform.eulerAngles = snapshot.gunRotation;
        
        // Restore health
        SetCurrentHealth(snapshot.health);
        
        // Set active state
        gameObject.SetActive(snapshot.isActive);
    }
    
    private float GetCurrentHealth()
    {
        if (playerHealth != null && playerHealth.GetComponent<Health>() != null)
            return playerHealth.GetComponent<Health>().CurrentHealth;
        if (health != null)
            return health.CurrentHealth;
        return 100f;
    }
    
    private void SetCurrentHealth(float newHealth)
    {
        if (playerHealth != null && playerHealth.GetComponent<Health>() != null)
            playerHealth.GetComponent<Health>().CurrentHealth = newHealth;
        else if (health != null)
            health.CurrentHealth = newHealth;
    }
    
    public bool IsActive()
    {
        return gameObject.activeInHierarchy;
    }
}