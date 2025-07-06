using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class WaveSpawnerTester : MonoBehaviour
{
    [SerializeField]
    private WaveManager waveManager; // Reference to the WaveManager

    [SerializeField, Tooltip("Manually select a wave index to trigger.")]
    private int waveIndex = 0;

#if UNITY_EDITOR
    [CustomEditor(typeof(WaveSpawnerTester))]
    public class WaveSpawnerTesterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            WaveSpawnerTester tester = (WaveSpawnerTester)target;

            if (tester.waveManager == null)
            {
                EditorGUILayout.HelpBox("Assign a WaveManager to test wave spawning.", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("Trigger Selected Wave"))
            {
                if (tester.waveManager.currentLevelData != null && tester.waveIndex >= 0 && tester.waveIndex < tester.waveManager.currentLevelData.waves.Length)
                {
                    tester.waveManager.StartCoroutine(tester.waveManager.SpawnWave(tester.waveManager.currentLevelData.waves[tester.waveIndex]));
                    Debug.Log($"Manually triggered wave {tester.waveIndex + 1}.");
                }
                else
                {
                    Debug.LogError("Invalid wave index or no LevelData assigned to WaveManager.");
                }
            }
        }
    }
#endif
}
