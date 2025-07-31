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

    private Rigidbody2D rb;
    private Vector2 movementInput;
    private Camera mainCam;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        mainCam = Camera.main;
        moveSpeed = playerData.moveSpeed;
        faceMouse = playerData.faceMouse;
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
    }

    private void Move()
    {
        rb.velocity = movementInput * moveSpeed;
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
        Vector3 mouseWorldPos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = (mouseWorldPos - gunTransform.position);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        gunTransform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
