using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class BikeComponent : MonoBehaviour
{
    [Header("Component Information")]
    public string componentName = "Bike Part";
    [TextArea(3, 5)]
    public string description = "Description of this bicycle component";
    
    [Header("Explosion Settings")]
    public Vector3 explosionDirection = Vector3.up;
    public float explosionDistance = 2f;
    public float animationSpeed = 2f;
    
    [Header("Visual Settings")]
    public Color highlightColor = Color.yellow;
    
    // Private variables to store original state
    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private Vector3 explodedPosition;
    private bool isExploded = false;
    private bool isMoving = false;
    
    // Materials for highlighting
    private Renderer[] renderers;
    private Material[][] originalMaterials;
    private Material highlightMaterial;

    [Header("VR Interaction")]
    public bool isGrabbable = true;
    public bool canSnapBack = true;
    private bool isBeingGrabbed = false;
    private bool isFreeModeEnabled = false;

    // References that need to be assigned
    private Rigidbody rb;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;

    void Start()
    {
        // Store original transform data
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        
        // Calculate exploded position
        explodedPosition = originalPosition + (explosionDirection.normalized * explosionDistance);
        
        // Get all renderers for highlighting
        renderers = GetComponentsInChildren<Renderer>();
        StoreOriginalMaterials();
        CreateHighlightMaterial();
    }

    void StoreOriginalMaterials()
    {
        originalMaterials = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            originalMaterials[i] = renderers[i].materials;
        }
    }

    void CreateHighlightMaterial()
    {
        highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.color = highlightColor;
        highlightMaterial.SetFloat("_Mode", 3); // Transparent mode
        highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        highlightMaterial.SetInt("_ZWrite", 0);
        highlightMaterial.DisableKeyword("_ALPHATEST_ON");
        highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
        highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        highlightMaterial.renderQueue = 3000;
    }

    public void ExplodeComponent()
    {
        if (!isMoving && !isExploded)
        {
            StartCoroutine(MoveToPosition(explodedPosition));
            isExploded = true;
        }
    }

    public void AssembleComponent()
    {
        if (!isMoving && isExploded)
        {
            StartCoroutine(MoveToPosition(originalPosition));
            isExploded = false;
        }
    }

    public void ToggleExplosion()
    {
        if (isExploded)
            AssembleComponent();
        else
            ExplodeComponent();
    }

    private System.Collections.IEnumerator MoveToPosition(Vector3 targetPosition)
    {
        isMoving = true;
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;
        float duration = 1f / animationSpeed;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float progress = elapsedTime / duration;
            
            // Use smooth curve for animation
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            yield return null;
        }

        transform.position = targetPosition;
        isMoving = false;
    }

    public void HighlightComponent(bool highlight)
    {
        if (renderers == null) return;

        for (int i = 0; i < renderers.Length; i++)
        {
            if (highlight)
            {
                // Create highlighted materials array
                Material[] highlightedMaterials = new Material[originalMaterials[i].Length + 1];
                for (int j = 0; j < originalMaterials[i].Length; j++)
                {
                    highlightedMaterials[j] = originalMaterials[i][j];
                }
                highlightedMaterials[highlightedMaterials.Length - 1] = highlightMaterial;
                renderers[i].materials = highlightedMaterials;
            }
            else
            {
                // Restore original materials
                renderers[i].materials = originalMaterials[i];
            }
        }
    }

    // VR Interaction Event Handlers
    void OnGrabbed(SelectEnterEventArgs args)
    {
        isBeingGrabbed = true;
        rb.isKinematic = false; // Allow physics when grabbed
        HighlightComponent(true);
        
        // Get the controller that grabbed this object
        var controller = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor;
        if (controller != null)
        {
            // You can add haptic feedback here
            // controller.SendHapticImpulse(0.5f, 0.1f);
        }
    }

    void OnReleased(SelectExitEventArgs args)
    {
        isBeingGrabbed = false;
        rb.isKinematic = true; // Return to kinematic
        HighlightComponent(false);
        
        // Check if we should snap back to original position
        var controller = args.interactorObject as UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor;
        if (controller != null && canSnapBack)
        {
            // Check for button press to snap back (e.g., trigger button)
            // This is a simplified check - you might want to use Input Actions
            CheckForSnapBack(controller);
        }
    }

    void CheckForSnapBack(UnityEngine.XR.Interaction.Toolkit.Interactors.XRBaseInputInteractor controller)
    {
        // This is where you'd check for specific controller button presses
        // For now, we'll use a simple distance check or provide public method
        
        // Example: If released close to original position, snap back
        float distanceToOriginal = Vector3.Distance(transform.position, originalPosition);
        if (distanceToOriginal < 0.5f && canSnapBack)
        {
            SnapToOriginalPosition();
        }
    }

    public void SnapToOriginalPosition()
    {
        if (!isBeingGrabbed)
        {
            StartCoroutine(MoveToPosition(originalPosition));
        }
    }

    public void EnableFreeMode(bool enable)
    {
        isFreeModeEnabled = enable;
        if (grabInteractable != null)
        {
            grabInteractable.enabled = enable;
        }
    }

    // Called when component is selected in VR (via ray-casting)
    public void OnComponentSelected()
    {
        HighlightComponent(true);
        Debug.Log($"Selected: {componentName}");
        
        // Enable grabbing when selected
        if (isGrabbable && grabInteractable != null)
        {
            grabInteractable.enabled = true;
        }
    }

    // Called when component is deselected
    public void OnComponentDeselected()
    {
        HighlightComponent(false);
        
        // Optionally disable grabbing when deselected
        // if (grabInteractable != null)
        // {
        //     grabInteractable.enabled = false;
        // }
    }

    // Getters for other scripts
    public bool IsExploded => isExploded;
    public bool IsMoving => isMoving;
    public string ComponentName => componentName;
    public string Description => description;

    // For debugging in editor
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Vector3 explodedPos = transform.position + (explosionDirection.normalized * explosionDistance);
        Gizmos.DrawLine(transform.position, explodedPos);
        Gizmos.DrawWireSphere(explodedPos, 0.1f);
    }
}