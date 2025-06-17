using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages the on-screen display of the gameplay log. This version uses a robust
/// prefab-based system, instantiating a new UI object for each log message.
/// As a freelance sound designer, this system is more reliable for debugging.
/// </summary>
public class GameplayLogUI : MonoBehaviour
{
    [Header("UI Setup")]
    [Tooltip("The parent GameObject for the entire log UI. This will be toggled on/off.")]
    [SerializeField] private GameObject logPanel;
    [Tooltip("The parent Transform where new log entry objects will be instantiated. This should be the 'Content' object of your Scroll View.")]
    [SerializeField] private Transform contentParent;
    [Tooltip("The UI Text prefab that will be used to display a single log message.")]
    [SerializeField] private GameObject logEntryPrefab;
    [Tooltip("The Scroll Rect component for the log view.")]
    [SerializeField] private ScrollRect scrollRect;

    [Header("Settings")]
    [Tooltip("The key used to toggle the log display on and off.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;
    [Tooltip("The maximum number of messages to keep in the log. Helps prevent performance issues.")]
    [SerializeField] private int maxMessages = 100;

    // A queue is a more efficient data structure for tracking active messages.
    private readonly Queue<GameObject> messageQueue = new Queue<GameObject>();

    // A simple pool to reuse log entry objects instead of destroying them.
    private readonly Queue<GameObject> objectPool = new Queue<GameObject>();

    private void Awake()
    {
        // Optionally pre-warm the pool with up to maxMessages objects.
        // This ensures we have enough objects in common use cases.
        for (int i = 0; i < maxMessages; i++)
        {
            GameObject pooledObj = Instantiate(logEntryPrefab);
            pooledObj.SetActive(false);
            objectPool.Enqueue(pooledObj);
        }

        // Ensure the log panel is hidden when the game starts.
        if (logPanel != null)
        {
            logPanel.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // Null reference check for the prefab
        if (logEntryPrefab == null)
        {
            Debug.LogError("Log Entry Prefab is not assigned in the GameplayLogUI inspector! The log will not function.");
            this.enabled = false; // Disable the component to prevent further errors.
            return;
        }
        GameplayLogger.OnMessageLogged += HandleNewLog;
    }

    private void OnDisable()
    {
        GameplayLogger.OnMessageLogged -= HandleNewLog;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (logPanel != null)
            {
                logPanel.SetActive(!logPanel.activeSelf);
            }
        }
    }

    /// <summary>
    /// This method now retrieves a prefab instance from the pool for each log message.
    /// </summary>
    private void HandleNewLog(LogEntry entry)
    {
        // Null reference checks for required components
        if (contentParent == null || scrollRect == null) return;

        // If we have too many messages, move the oldest one back to the pool and out of the UI.
        if (messageQueue.Count >= maxMessages)
        {
            GameObject oldestMessage = messageQueue.Dequeue();
            oldestMessage.SetActive(false);
            objectPool.Enqueue(oldestMessage);
        }

        // Acquire an object from the pool or instantiate one if the pool is empty.
        GameObject newEntryObject;
        if (objectPool.Count > 0)
        {
            newEntryObject = objectPool.Dequeue();
            newEntryObject.SetActive(true);
        }
        else
        {
            newEntryObject = Instantiate(logEntryPrefab);
        }

        // Parent it properly under the content area and reset transform as needed.
        newEntryObject.transform.SetParent(contentParent, false);

        // Find the Text component on our new prefab instance.
        Text textComponent = newEntryObject.GetComponent<Text>();
        if (textComponent != null)
        {
            // Set its text using the same formatting logic as before.
            textComponent.text = FormatLogEntry(entry);
        }

        // Add the new object to our queue for tracking.
        messageQueue.Enqueue(newEntryObject);

        // This coroutine will force the scroll view to the bottom.
        StartCoroutine(ForceScrollDown());
    }

    private string FormatLogEntry(LogEntry entry)
    {
        string categoryColor = GetCategoryColor(entry.Category);
        return $"{entry.Timestamp} <color={categoryColor}>[{entry.Category}]</color> {entry.Message}";
    }

    private IEnumerator<WaitForEndOfFrame> ForceScrollDown()
    {
        // We wait for the end of the frame to ensure the new object has been added to the layout.
        yield return new WaitForEndOfFrame();
        // Null reference check for the scrollRect
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    private string GetCategoryColor(LogCategory category)
    {
        switch (category)
        {
            case LogCategory.System: return "#808080";
            case LogCategory.Audio: return "#00FFFF";
            case LogCategory.Gameplay: return "#FFFFFF";
            case LogCategory.AI: return "#FFFF00";
            case LogCategory.Error: return "#FF0000";
            default: return "#FFFFFF";
        }
    }
}