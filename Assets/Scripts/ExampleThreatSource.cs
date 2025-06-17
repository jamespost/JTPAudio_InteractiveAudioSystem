using UnityEngine;

/// <summary>
/// An EXAMPLE of how a game-specific component would implement the IAudioThreat interface.
/// This script could be attached to an enemy GameObject.
/// </summary>
public class ExampleThreatSource : MonoBehaviour, IAudioThreat
{
    [Tooltip("The base threat level of this enemy when it's idle.")]
    [Range(0f, 1f)]
    public float baseThreat = 0.1f;

    [Tooltip("For testing, press Left Shift to simulate this enemy charging a powerful attack.")]
    public bool isChargingAttack = false;

    // This is the required method from the IAudioThreat interface.
    public float GetCurrentThreat()
    {
        // This is where game-specific logic would go. For example, it could check
        // if the enemy is targeting the player, how close it is, etc.
        // For our example, we'll just return a high value if it's charging an attack.
        if (isChargingAttack)
        {
            return 0.9f; // High threat
        }
        else
        {
            return baseThreat; // Normal, low threat
        }
    }

    // A simple test method for you to see the threat change in action.
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            isChargingAttack = true;
            GameplayLogger.Log("Example Enemy is now CHARGING ATTACK (Threat = 0.9)", LogCategory.Gameplay);
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            isChargingAttack = false;
            GameplayLogger.Log("Example Enemy stopped charging (Threat = 0.1)", LogCategory.Gameplay);
        }
    }
}
