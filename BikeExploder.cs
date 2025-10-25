// ===== UPDATED BikeExploder.cs =====
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;

public class BikeExploder : MonoBehaviour
{
    [Header("Explosion Settings")]
    public float explosionSpeed = 1f;
    public bool startExploded = false;
    
    [Header("VR Interaction")]
    public LayerMask interactionLayers = -1;
    
    // Private variables
    private BikeComponent[] bikeComponents;
    private bool isCurrentlyExploded = false;
    private bool isAnimating = false;
    
    // Selected component for UI display
    private BikeComponent selectedComponent;
    
    // VR Controller variables
    private InputDevice leftControllerDevice;
    private InputDevice rightControllerDevice;
    private bool leftSecondaryPressed = false;  // Changed from grip to secondary button
    private bool rightSecondaryPressed = false; // Changed from grip to secondary button

    void Start()
    {
        // Find all BikeComponent scripts in children
        bikeComponents = GetComponentsInChildren<BikeComponent>();
        
        Debug.Log($"Found {bikeComponents.Length} bike components");
        
        // Initialize VR controllers
        InitializeControllers();
        
        // Set initial state
        if (startExploded)
        {
            ExplodeAllComponents();
        }
    }

    void InitializeControllers()
    {
        var leftHandDevices = new List<InputDevice>();
        var rightHandDevices = new List<InputDevice>();
        
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        
        if (leftHandDevices.Count > 0)
        {
            leftControllerDevice = leftHandDevices[0];
        }
        
        if (rightHandDevices.Count > 0)
        {
            rightControllerDevice = rightHandDevices[0];
        }
    }

    void Update()
    {
        // Handle VR controller input for explosion toggle
        HandleVRInput();
        
        // Handle component selection via ray-casting
        HandleComponentSelection();
    }

    void HandleVRInput()
    {
        // Re-initialize controllers if needed
        if (!leftControllerDevice.isValid || !rightControllerDevice.isValid)
        {
            InitializeControllers();
        }

        // Check for secondary button presses (Y/B buttons on Oculus, or equivalent)
        bool leftSecondaryCurrently, rightSecondaryCurrently;
        
        leftControllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out leftSecondaryCurrently);
        rightControllerDevice.TryGetFeatureValue(CommonUsages.secondaryButton, out rightSecondaryCurrently);

        // Detect secondary button press (not held)
        if (leftSecondaryCurrently && !leftSecondaryPressed)
        {
            Debug.Log("Left secondary button pressed - Toggle explosion");
            ToggleExplosion();
        }
        
        if (rightSecondaryCurrently && !rightSecondaryPressed)
        {
            Debug.Log("Right secondary button pressed - Toggle explosion");
            ToggleExplosion();
        }

        // Check for both secondary buttons pressed simultaneously for full reset
        if (leftSecondaryCurrently && rightSecondaryCurrently && (!leftSecondaryPressed || !rightSecondaryPressed))
        {
            Debug.Log("Both secondary buttons pressed - Full reset to assembled state");
            if (isCurrentlyExploded)
            {
                AssembleAllComponents();
            }
        }

        // Update previous frame states
        leftSecondaryPressed = leftSecondaryCurrently;
        rightSecondaryPressed = rightSecondaryCurrently;
    }

    void HandleComponentSelection()
    {
        // Ray-casting will be handled by XR Ray Interactor in VR
        // For now, we'll disable mouse input to avoid Input System conflicts
        
        // TODO: Implement VR ray-casting for component selection
    }

    public void ToggleExplosion()
    {
        if (isAnimating) return;
        
        if (isCurrentlyExploded)
        {
            AssembleAllComponents();
        }
        else
        {
            ExplodeAllComponents();
        }
    }

    public void ExplodeAllComponents()
    {
        if (isAnimating) return;
        
        StartCoroutine(ExplodeSequence());
    }

    public void AssembleAllComponents()
    {
        if (isAnimating) return;
        
        StartCoroutine(AssembleSequence());
    }

    private System.Collections.IEnumerator ExplodeSequence()
    {
        isAnimating = true;
        
        // Disable drivetrain when exploding
        DrivetrainController drivetrain = GetComponent<DrivetrainController>();
        if (drivetrain != null)
        {
            drivetrain.EnableDrivetrain(false);
        }
        
        foreach (BikeComponent component in bikeComponents)
        {
            component.ExplodeComponent();
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(1f / explosionSpeed);
        
        isCurrentlyExploded = true;
        isAnimating = false;
    }

    private System.Collections.IEnumerator AssembleSequence()
    {
        isAnimating = true;
        
        for (int i = bikeComponents.Length - 1; i >= 0; i--)
        {
            bikeComponents[i].AssembleComponent();
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return new WaitForSeconds(1f / explosionSpeed);
        
        // Re-enable drivetrain when assembled
        DrivetrainController drivetrain = GetComponent<DrivetrainController>();
        if (drivetrain != null)
        {
            drivetrain.EnableDrivetrain(true);
        }
        
        isCurrentlyExploded = false;
        isAnimating = false;
    }

    // UI Methods (implement based on your VR UI system)
    void ShowComponentInfo(BikeComponent component)
    {
        Debug.Log($"Selected: {component.ComponentName}");
        Debug.Log($"Description: {component.Description}");
    }

    void HideComponentInfo()
    {
        Debug.Log("Component deselected");
    }

    // Public methods for external control (UI buttons, etc.)
    public void ExplodeSpecificComponent(string componentName)
    {
        BikeComponent component = System.Array.Find(bikeComponents, c => c.ComponentName == componentName);
        if (component != null)
        {
            component.ExplodeComponent();
        }
    }

    public void AssembleSpecificComponent(string componentName)
    {
        BikeComponent component = System.Array.Find(bikeComponents, c => c.ComponentName == componentName);
        if (component != null)
        {
            component.AssembleComponent();
        }
    }

    // Getters for other scripts
    public bool IsExploded => isCurrentlyExploded;
    public bool IsAnimating => isAnimating;
    public BikeComponent SelectedComponent => selectedComponent;
    public BikeComponent[] AllComponents => bikeComponents;
}

