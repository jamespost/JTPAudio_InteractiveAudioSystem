using UnityEngine;

[CreateAssetMenu(fileName = "LevelData", menuName = "Game/LevelData")]
public class LevelData : ScriptableObject
{
    [Tooltip("Array of WaveData assets defining the sequence of waves for this level.")]
    public WaveData[] waves;
}
