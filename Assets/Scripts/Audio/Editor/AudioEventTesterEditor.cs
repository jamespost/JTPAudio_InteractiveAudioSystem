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

        // Logic for the repeating test mode
        if (tester.enableRepeat)
        {
            if (tester.isRepeating)
            {
                if (GUILayout.Button("Stop Repeating Sound", GUILayout.Height(30)))
                {
                    tester.isRepeating = false;
                }
            }
            else
            {
                if (GUILayout.Button("Start Repeating Sound", GUILayout.Height(30)))
                {
                    if (tester.eventToTest != null)
                    {
                        tester.isRepeating = true;
                        tester.nextPlayTime = EditorApplication.timeSinceStartup;
                        EditorApplication.update += UpdateRepeating; // Subscribe to the editor update loop
                    }
                    else
                    {
                        Debug.LogWarning("No AudioEvent assigned to the tester.");
                    }
                }
            }
        }
        else // Original one-shot test mode
        {
            if (GUILayout.Button("Play Test Sound", GUILayout.Height(30)))
            {
                if (tester.eventToTest != null)
                {
                    AudioManager.Instance.PostEvent(tester.eventToTest.eventID, tester.gameObject);
                }
                else
                {
                    Debug.LogWarning("No AudioEvent assigned to the tester.");
                }
            }
        }
    }

    private void OnEnable()
    {
        // Ensure the update delegate is registered if needed when the inspector is re-enabled
        AudioEventTester tester = (AudioEventTester)target;
        if (tester != null && tester.isRepeating)
        {
            EditorApplication.update += UpdateRepeating;
        }
    }

    private void OnDisable()
    {
        // IMPORTANT: Always unsubscribe from the update loop when the inspector is disabled or destroyed
        EditorApplication.update -= UpdateRepeating;
    }

    void UpdateRepeating()
    {
        AudioEventTester tester = (AudioEventTester)target;

        // Safety checks
        if (tester == null || !tester.isRepeating || !tester.enableRepeat)
        {
            tester.isRepeating = false;
            EditorApplication.update -= UpdateRepeating;
            Repaint(); // Force the inspector to redraw to update the button state
            return;
        }

        // Check if it's time to play the sound again
        if (EditorApplication.timeSinceStartup >= tester.nextPlayTime)
        {
            if (tester.eventToTest != null && AudioManager.Instance != null)
            {
                AudioManager.Instance.PostEvent(tester.eventToTest.eventID, tester.gameObject);
                tester.nextPlayTime = EditorApplication.timeSinceStartup + tester.repeatInterval;
            }
            else
            {
                // Stop repeating if the event or AudioManager is missing
                tester.isRepeating = false;
                EditorApplication.update -= UpdateRepeating;
                Repaint();
            }
        }
    }
}
