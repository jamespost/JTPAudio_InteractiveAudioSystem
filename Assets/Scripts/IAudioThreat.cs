/// <summary>
/// A contract for any component that can represent a dynamic audio threat.
/// </summary>
public interface IAudioThreat
{
    /// <summary>
    /// This method should return the current threat level of the object.
    /// The value should be normalized, where 0 is no threat and 1 is maximum threat.
    /// The game-specific logic for calculating this value lives inside the implementing class.
    /// </summary>
    /// <returns>A float between 0 and 1 representing the current threat level.</returns>
    float GetCurrentThreat();
}
