using UnityEngine;

using UnityEngine.XR;
using System.Collections.Generic;

public class DrivetrainController : MonoBehaviour
{
    [Header("Drivetrain Components")]
    public Transform leftPedal;
    public Transform rightPedal;
    public Transform pedalChainRing;
    public Transform rearWheel;
    public Transform rearChainRing;
    
    [Header("Gear Ratios")]
    public float chainRingTeeth = 50f;
    public float casseteTeeth = 25f;
    public float wheelCircumference = 2.1f;
    
    [Header("Rotation Settings")]
    public float rotationSpeed = 30f; // Degrees per second when spinning
    public bool enableDrivetrain = true;
    
    [Header("Controls")]
    [Tooltip("Use thumbstick for pedaling instead of physical interaction")]
    public bool useThumbstickControl = true;
    
    // Private variables
    private BikeExploder bikeExploder;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable leftPedalGrab;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable rightPedalGrab;
    
    // Rotation tracking
    private float currentPedalRotation = 0f;
    
    // Store original rotations to avoid the 90-degree offset issue
    private Quaternion leftPedalOriginalRotation;
    private Quaternion rightPedalOriginalRotation;
    private Quaternion chainRingOriginalRotation;
    private Quaternion rearWheelOriginalRotation;
    private Quaternion rearChainRingOriginalRotation;

    void Start()
    {
        bikeExploder = GetComponentInParent<BikeExploder>();
        
        // Store original rotations
        if (leftPedal != null)
            leftPedalOriginalRotation = leftPedal.rotation;
        if (rightPedal != null)
            rightPedalOriginalRotation = rightPedal.rotation;
        if (pedalChainRing != null)
            chainRingOriginalRotation = pedalChainRing.rotation;
        if (rearWheel != null)
            rearWheelOriginalRotation = rearWheel.rotation;
        if (rearChainRing != null)
            rearChainRingOriginalRotation = rearChainRing.rotation;
        
        // Get grab interactables for pedals
        if (leftPedal != null)
            leftPedalGrab = leftPedal.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (rightPedal != null)
            rightPedalGrab = rightPedal.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        
        SetPedalGrabbing(false);
    }

    void Update()
    {
        // Only enable drivetrain when bike is assembled
        if (!enableDrivetrain || bikeExploder == null || bikeExploder.IsExploded)
        {
            SetPedalGrabbing(true); // Allow grabbing when exploded
            return;
        }

        // Disable grabbing when assembled
        SetPedalGrabbing(false);
        
        // Handle rotation input
        if (useThumbstickControl)
        {
            HandleThumbstickRotation();
        }
        else
        {
            HandleTriggerRotation();
        }
        
        // Apply rotations
        ApplyDrivetrainRotations();
    }

    void SetPedalGrabbing(bool canGrab)
    {
        if (leftPedalGrab != null)
            leftPedalGrab.enabled = canGrab;
        if (rightPedalGrab != null)
            rightPedalGrab.enabled = canGrab;
    }

    void HandleThumbstickRotation()
    {
        List<InputDevice> leftHandDevices = new List<InputDevice>();
        List<InputDevice> rightHandDevices = new List<InputDevice>();
        
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        
        float rotationInput = 0f;
        
        // Check left controller thumbstick
        if (leftHandDevices.Count > 0)
        {
            Vector2 leftThumbstick;
            if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out leftThumbstick))
            {
                rotationInput += leftThumbstick.y; // Up/down on thumbstick
            }
        }
        
        // Check right controller thumbstick
        if (rightHandDevices.Count > 0)
        {
            Vector2 rightThumbstick;
            if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.primary2DAxis, out rightThumbstick))
            {
                rotationInput += rightThumbstick.y; // Up/down on thumbstick
            }
        }
        
        // Apply rotation based on thumbstick input
        if (Mathf.Abs(rotationInput) > 0.1f) // Deadzone
        {
            currentPedalRotation += rotationInput * rotationSpeed * Time.deltaTime;
        }
    }

    void HandleTriggerRotation()
    {
        List<InputDevice> leftHandDevices = new List<InputDevice>();
        List<InputDevice> rightHandDevices = new List<InputDevice>();
        
        InputDevices.GetDevicesAtXRNode(XRNode.LeftHand, leftHandDevices);
        InputDevices.GetDevicesAtXRNode(XRNode.RightHand, rightHandDevices);
        
        float rotationInput = 0f;
        
        // Check left trigger
        if (leftHandDevices.Count > 0)
        {
            float leftTrigger;
            if (leftHandDevices[0].TryGetFeatureValue(CommonUsages.trigger, out leftTrigger))
            {
                if (leftTrigger > 0.5f)
                    rotationInput += 1f; // Forward rotation
            }
        }
        
        // Check right trigger  
        if (rightHandDevices.Count > 0)
        {
            float rightTrigger;
            if (rightHandDevices[0].TryGetFeatureValue(CommonUsages.trigger, out rightTrigger))
            {
                if (rightTrigger > 0.5f)
                    rotationInput -= 1f; // Backward rotation
            }
        }
        
        // Apply rotation based on trigger input
        if (Mathf.Abs(rotationInput) > 0.1f)
        {
            currentPedalRotation += rotationInput * rotationSpeed * Time.deltaTime;
        }
    }

    void ApplyDrivetrainRotations()
    {
        // Calculate gear ratio
        float gearRatio = chainRingTeeth / casseteTeeth;
        float rearWheelRotation = currentPedalRotation * gearRatio;

        // Apply rotations relative to original orientations
        if (leftPedal != null)
        {
            leftPedal.rotation = leftPedalOriginalRotation * Quaternion.Euler(currentPedalRotation, 0, 0);
        }
        
        if (rightPedal != null)
        {
            // Right pedal is 180 degrees offset from left
            rightPedal.rotation = rightPedalOriginalRotation * Quaternion.Euler(currentPedalRotation + 180f, 0, 0);
        }

        if (pedalChainRing != null)
        {
            pedalChainRing.rotation = chainRingOriginalRotation * Quaternion.Euler(0, 0, currentPedalRotation);
        }

        if (rearWheel != null)
        {
            rearWheel.rotation = rearWheelOriginalRotation * Quaternion.Euler(0, 0, rearWheelRotation);
        }

        if (rearChainRing != null)
        {
            rearChainRing.rotation = rearChainRingOriginalRotation * Quaternion.Euler(0, 0, rearWheelRotation);
        }
    }

    // Public methods for external control
    public void EnableDrivetrain(bool enable)
    {
        enableDrivetrain = enable;
        Debug.Log($"Drivetrain enabled: {enable}");
    }

    public void ResetRotations()
    {
        currentPedalRotation = 0f;
        
        // Reset all components to original rotations
        if (leftPedal != null)
            leftPedal.rotation = leftPedalOriginalRotation;
        if (rightPedal != null)
            rightPedal.rotation = rightPedalOriginalRotation;
        if (pedalChainRing != null)
            pedalChainRing.rotation = chainRingOriginalRotation;
        if (rearWheel != null)
            rearWheel.rotation = rearWheelOriginalRotation;
        if (rearChainRing != null)
            rearChainRing.rotation = rearChainRingOriginalRotation;
    }

    // For debugging
    void OnDrawGizmos()
    {
        if (leftPedal != null && rightPedal != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(leftPedal.position, rightPedal.position);
        }
        
        if (pedalChainRing != null && rearChainRing != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(pedalChainRing.position, rearChainRing.position);
        }
    }
}