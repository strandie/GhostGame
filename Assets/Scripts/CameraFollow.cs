using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target; // Assign your player here
    
    [Header("Follow Settings")]
    [Range(0.1f, 5f)]
    public float followSpeed = 2f; // How fast camera catches up to player
    
    [Header("Look Ahead")]
    public bool enableLookAhead = true;
    [Range(0f, 5f)]
    public float lookAheadDistance = 2f; // How far ahead to look based on movement
    [Range(0.1f, 2f)]
    public float lookAheadSmoothTime = 0.5f; // How smooth the look ahead is
    
    [Header("Mouse Influence")]
    public bool enableMouseInfluence = true;
    [Range(0f, 3f)]
    public float mouseInfluenceDistance = 1.5f; // How much mouse position affects camera
    [Range(0.1f, 2f)]
    public float mouseInfluenceSmoothTime = 0.3f;
    
    [Header("Boundaries (Optional)")]
    public bool useBoundaries = false;
    public Vector2 minBounds = new Vector2(-10, -10);
    public Vector2 maxBounds = new Vector2(10, 10);
    
    private Camera cam;
    private Vector3 velocity = Vector3.zero;
    private Vector3 lookAheadVelocity = Vector3.zero;
    private Vector3 mouseInfluenceVelocity = Vector3.zero;
    private Vector3 currentLookAhead = Vector3.zero;
    private Vector3 currentMouseInfluence = Vector3.zero;
    
    // For tracking player movement
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        
        if (target != null)
        {
            lastPlayerPosition = target.position;
            
            // Initialize camera position without smoothing
            Vector3 initialPos = target.position;
            initialPos.z = transform.position.z; // Keep camera's Z position
            transform.position = initialPos;
        }
    }
    
    void LateUpdate()
    {
        if (target == null) return;
        
        // Calculate player velocity for look ahead
        playerVelocity = (target.position - lastPlayerPosition) / Time.deltaTime;
        lastPlayerPosition = target.position;
        
        // Calculate target position
        Vector3 targetPosition = target.position;
        
        // Add look ahead based on player movement
        if (enableLookAhead && playerVelocity.magnitude > 0.1f)
        {
            Vector3 desiredLookAhead = playerVelocity.normalized * lookAheadDistance;
            currentLookAhead = Vector3.SmoothDamp(currentLookAhead, desiredLookAhead, 
                ref lookAheadVelocity, lookAheadSmoothTime);
            targetPosition += currentLookAhead;
        }
        else if (enableLookAhead)
        {
            // Smoothly return to no look ahead when not moving
            currentLookAhead = Vector3.SmoothDamp(currentLookAhead, Vector3.zero, 
                ref lookAheadVelocity, lookAheadSmoothTime);
            targetPosition += currentLookAhead;
        }
        
        // Add mouse influence (similar to Enter the Gungeon)
        if (enableMouseInfluence)
        {
            Vector3 mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            mouseWorldPos.z = 0;
            
            Vector3 mouseDirection = (mouseWorldPos - target.position).normalized;
            Vector3 desiredMouseInfluence = mouseDirection * mouseInfluenceDistance;
            
            currentMouseInfluence = Vector3.SmoothDamp(currentMouseInfluence, desiredMouseInfluence,
                ref mouseInfluenceVelocity, mouseInfluenceSmoothTime);
            targetPosition += currentMouseInfluence;
        }
        
        // Keep the camera's Z position
        targetPosition.z = transform.position.z;
        
        // Apply boundaries if enabled
        if (useBoundaries)
        {
            targetPosition.x = Mathf.Clamp(targetPosition.x, minBounds.x, maxBounds.x);
            targetPosition.y = Mathf.Clamp(targetPosition.y, minBounds.y, maxBounds.y);
        }
        
        // Smoothly move camera to target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, 
            ref velocity, 1f / followSpeed);
    }
    
    // Optional: Draw gizmos in scene view to visualize boundaries
    void OnDrawGizmosSelected()
    {
        if (useBoundaries)
        {
            Gizmos.color = Color.yellow;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) / 2f, (minBounds.y + maxBounds.y) / 2f, 0);
            Vector3 size = new Vector3(maxBounds.x - minBounds.x, maxBounds.y - minBounds.y, 0);
            Gizmos.DrawWireCube(center, size);
        }
        
        if (target != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(target.position, 0.5f);
        }
    }
}
