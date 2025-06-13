using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates a custom Inspector for the AudioEventTester component.
/// This adds a "Play Test Sound" button to make auditioning events easy.
/// </summary>
[CustomEditor(typeof(AudioEventTester))]
public class AudioEventTesterEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields (like the eventToTest field)
        DrawDefaultInspector();

        // Get a reference to the component we are inspecting
        AudioEventTester tester = (AudioEventTester)target;

        // Add some space for visual clarity
        EditorGUILayout.Space();

        // Add a button. If it's clicked, the code inside the 'if' statement runs.
        if (GUILayout.Button("Play Test Sound", GUILayout.Height(30)))
        {
            // Check if an event has actually been assigned
            if (tester.eventToTest != null)
            {
                // Trigger the event via the AudioManager singleton, using the
                // GameObject this component is attached to as the sound source.
                AudioManager.Instance.PostEvent(tester.eventToTest.eventID, tester.gameObject);
            }
            else
            {
                Debug.LogWarning("No AudioEvent assigned to the tester.");
            }
        }
    }
}
