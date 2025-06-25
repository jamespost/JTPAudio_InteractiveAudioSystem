// Assets/Scripts/Data/WaveData.cs
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New WaveData", menuName = "Data/Wave Data")]
public class WaveData : ScriptableObject
{
    [Header("Wave Composition")]
    [Tooltip("A list of enemy groups to spawn in this wave.")]
    public List<SpawnGroup> spawnGroups;
}
