using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TentacleLocomotion : MonoBehaviour
{
    [System.Serializable]
    public class TentacleLeg
    {
        public string name;
        public Transform ikTarget;
        public Transform raycastOrigin; // The shoulder/base of the tentacle on the body
        public Vector3 defaultOffset; // The default position relative to the body when standing still
        [HideInInspector] public Vector3 currentGroundedPosition;
        [HideInInspector] public bool isMoving;
        [HideInInspector] public float lastStepTime;
    }

    [Header("Tentacle Configuration")]
    public List<TentacleLeg> tentacles = new List<TentacleLeg>();

    [Header("Movement Settings")]
    public float stepDistance = 1.5f;
    public float stepHeight = 0.5f;
    public float stepDuration = 0.3f;
    public float stepOvershootFraction = 0.5f; // How much to lead the target based on velocity
    public float groundTipOffset = 0.05f; // Lift the tip slightly to prevent clipping
    public LayerMask groundLayer = 1; // Default to "Default" layer

    [Header("Gait Settings")]
    public int maxSimultaneousSteps = 2;
    public float minTimeBetweenSteps = 0.05f;

    [Header("Body Adjustment")]
    public Transform visualBody; // Assign the visual mesh/bone root here to animate body height without affecting the NavMeshAgent
    public bool adjustBodyHeight = true;
    public float bodyHeightOffset = 1.0f;
    public float bodySmoothTime = 0.1f;

    [Header("Debug")]
    public bool showDebugGizmos = true;
    public Color idealPosColor = Color.green;
    public Color targetPosColor = Color.red;
    public Color pathColor = Color.yellow;

    private Vector3 lastBodyPosition;
    private Vector3 currentVelocity;
    private float lastGlobalStepTime;
    private float currentBodyYVelocity; // For SmoothDamp

    private void Start()
    {
        lastBodyPosition = transform.position;

        // Initialize tentacles
        foreach (var leg in tentacles)
        {
            if (leg.ikTarget == null) continue;

            // If default offset is not set, calculate it from current positions
            if (leg.defaultOffset == Vector3.zero)
            {
                // Calculate relative position on XZ plane
                Vector3 relativePos = leg.ikTarget.position - transform.position;
                // Store the offset relative to the body's local rotation
                leg.defaultOffset = transform.InverseTransformDirection(relativePos);
                leg.defaultOffset.y = 0; // Flatten to ground plane for the offset
            }

            // Snap to ground initially
            Vector3 targetPos = GetIdealPosition(leg);
            leg.currentGroundedPosition = targetPos;
            leg.ikTarget.position = targetPos;
            leg.isMoving = false;
        }
    }

    private void Update()
    {
        // Calculate velocity
        Vector3 displacement = transform.position - lastBodyPosition;
        currentVelocity = displacement / Time.deltaTime;
        lastBodyPosition = transform.position;

        // Check legs
        int movingLegs = 0;
        foreach (var leg in tentacles)
        {
            if (leg.isMoving) movingLegs++;
        }

        // Sort legs by distance to ideal position to prioritize the one that needs to move most
        // We can't easily sort the list in place without messing up references if we used indices, 
        // but we can iterate and find the best candidate.
        
        TentacleLeg bestCandidate = null;
        float maxDist = 0f;

        foreach (var leg in tentacles)
        {
            if (leg.isMoving) continue;

            Vector3 idealPos = GetIdealPosition(leg);
            float dist = Vector3.Distance(leg.currentGroundedPosition, idealPos);

            if (dist > stepDistance)
            {
                if (dist > maxDist)
                {
                    maxDist = dist;
                    bestCandidate = leg;
                }
            }
        }

        if (bestCandidate != null && movingLegs < maxSimultaneousSteps && Time.time > lastGlobalStepTime + minTimeBetweenSteps)
        {
            StartCoroutine(StepRoutine(bestCandidate, GetIdealPosition(bestCandidate)));
        }

        // Update IK targets for non-moving legs to stick to the ground (in case the body moves vertically or tilts, 
        // though usually IK targets are in world space so they stay put automatically. 
        // But if we parented them to the body, we'd need to un-parent or counter-move.
        // Assuming IK targets are children of the root or independent. 
        // If they are children, they move with the body. If they are independent, they stay.
        // The user said "IK targets are tied to the tip". 
        // We will assume we have absolute control over the IK target position in World Space.
        
        foreach (var leg in tentacles)
        {
            if (!leg.isMoving)
            {
                leg.ikTarget.position = leg.currentGroundedPosition;
            }
        }

        if (adjustBodyHeight)
        {
            AdjustBodyHeight();
        }
    }

    private Vector3 GetIdealPosition(TentacleLeg leg)
    {
        // Calculate the "home" position relative to the body's current position and rotation
        // We use the defaultOffset rotated by the body's rotation
        Vector3 worldOffset = transform.TransformDirection(leg.defaultOffset);
        Vector3 homePos = transform.position + worldOffset;

        // Add velocity prediction
        homePos += currentVelocity * stepOvershootFraction;

        // Raycast to find ground
        RaycastHit hit;
        Vector3 rayOrigin = homePos;
        rayOrigin.y += 2.0f; // Start ray from above

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 5.0f, groundLayer))
        {
            return hit.point + Vector3.up * groundTipOffset;
        }
        else
        {
            // If no ground, assume ground is at root level (NavMeshAgent level)
            return new Vector3(homePos.x, transform.position.y + groundTipOffset, homePos.z);
        }
    }

    private IEnumerator StepRoutine(TentacleLeg leg, Vector3 targetPos)
    {
        leg.isMoving = true;
        lastGlobalStepTime = Time.time;
        leg.lastStepTime = Time.time;

        Vector3 startPos = leg.currentGroundedPosition;
        float timeElapsed = 0f;

        while (timeElapsed < stepDuration)
        {
            timeElapsed += Time.deltaTime;
            float t = timeElapsed / stepDuration;
            
            // Linear interpolation for XZ
            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, t);

            // Parabolic arc for Y
            // 4 * stepHeight * t * (1 - t) gives a parabola starting at 0, peaking at stepHeight at t=0.5, and ending at 0 at t=1
            currentPos.y += 4.0f * stepHeight * t * (1.0f - t);

            leg.ikTarget.position = currentPos;

            yield return null;
        }

        leg.currentGroundedPosition = targetPos;
        leg.ikTarget.position = targetPos;
        leg.isMoving = false;
    }

    private void AdjustBodyHeight()
    {
        if (visualBody == null) return;

        // Calculate average leg height
        float avgY = 0f;
        int activeLegs = 0;
        foreach (var leg in tentacles)
        {
            if (leg.ikTarget != null)
            {
                avgY += leg.ikTarget.position.y;
                activeLegs++;
            }
        }
        
        if (activeLegs > 0)
        {
            avgY /= activeLegs;
            float targetY = avgY + bodyHeightOffset;

            Vector3 newPos = visualBody.position;
            newPos.y = Mathf.SmoothDamp(newPos.y, targetY, ref currentBodyYVelocity, bodySmoothTime);
            visualBody.position = newPos;
        }
    }

    [ContextMenu("Capture Default Offsets")]
    public void CaptureDefaultOffsets()
    {
        foreach (var leg in tentacles)
        {
            if (leg.ikTarget != null)
            {
                Vector3 relativePos = leg.ikTarget.position - transform.position;
                leg.defaultOffset = transform.InverseTransformDirection(relativePos);
                leg.defaultOffset.y = 0;
            }
        }
        Debug.Log("Captured default offsets for " + tentacles.Count + " tentacles.");
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos) return;

        foreach (var leg in tentacles)
        {
            if (leg.ikTarget == null) continue;

            // Draw current target
            Gizmos.color = targetPosColor;
            Gizmos.DrawWireSphere(leg.ikTarget.position, 0.2f);

            // Draw ideal position
            if (Application.isPlaying)
            {
                Vector3 ideal = GetIdealPosition(leg);
                Gizmos.color = idealPosColor;
                Gizmos.DrawWireSphere(ideal, 0.2f);
                Gizmos.DrawLine(leg.ikTarget.position, ideal);
            }
            else
            {
                // In editor, estimate ideal based on current transform
                if (leg.defaultOffset != Vector3.zero)
                {
                    Vector3 worldOffset = transform.TransformDirection(leg.defaultOffset);
                    Vector3 homePos = transform.position + worldOffset;
                    Gizmos.color = idealPosColor;
                    Gizmos.DrawWireSphere(homePos, 0.2f);
                }
            }

            // Draw raycast origin if set
            if (leg.raycastOrigin != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawWireSphere(leg.raycastOrigin.position, 0.1f);
                Gizmos.DrawLine(leg.raycastOrigin.position, leg.ikTarget.position);
            }
        }
    }
}
