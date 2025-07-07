using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    private static UIManager _instance;
    public static UIManager Instance => _instance;

    private Dictionary<string, GameObject> _activeUIElements = new Dictionary<string, GameObject>();
    private List<GameObject> _uiElementLayout = new List<GameObject>();

    public Font uiFont;
    public Color primaryColor = new Color(1.0f, 0.72f, 0.0f);

    private GUIStyle _uiStyle;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
    }

    private void OnEnable()
    {
        EventManager.OnUIUpdateRequest += HandleUIUpdateRequest;
        EventManager.OnUIHideRequest += HandleUIHideRequest;
    }

    private void OnDisable()
    {
        EventManager.OnUIUpdateRequest -= HandleUIUpdateRequest;
        EventManager.OnUIHideRequest -= HandleUIHideRequest;
    }

    private void SetupGUIStyle()
    {
        _uiStyle = new GUIStyle()
        {
            font = uiFont,
            fontSize = 18,
            alignment = TextAnchor.UpperLeft,
            normal = { textColor = primaryColor }
        };
    }

    private void HandleUIUpdateRequest(string id, string text)
    {
        if (_activeUIElements.ContainsKey(id))
        {
            _activeUIElements[id].GetComponent<UITextElement>().text = text;
        }
        else
        {
            GameObject uiElementObject = ObjectPooler.Instance.SpawnFromPool("UIElement", Vector3.zero, Quaternion.identity);
            if (uiElementObject != null)
            {
                UITextElement uiElement = uiElementObject.GetComponent<UITextElement>();
                if (uiElement != null)
                {
                    uiElement.id = id;
                    uiElement.text = text;
                    _activeUIElements.Add(id, uiElementObject);
                    _uiElementLayout.Add(uiElementObject);
                }
            }
        }
    }

    private void HandleUIHideRequest(string id)
    {
        if (_activeUIElements.ContainsKey(id))
        { 
            GameObject objectToReturn = _activeUIElements[id];
            ObjectPooler.Instance.ReturnToPool("UIElement", objectToReturn);
            _activeUIElements.Remove(id);
            _uiElementLayout.Remove(objectToReturn);
        }
    }

    private void OnGUI()
    {
        if (uiFont != null && _uiStyle == null)
        {
            SetupGUIStyle();
        }

        if (_uiStyle == null) return;

        float yPos = 10f;
        for (int i = 0; i < _uiElementLayout.Count; i++)
        {
            GameObject uiElementObject = _uiElementLayout[i];
            if (uiElementObject.activeInHierarchy)
            {
                UITextElement uiElement = uiElementObject.GetComponent<UITextElement>();
                if (uiElement != null)
                {
                    GUI.Label(new Rect(10, yPos, 300, 40), uiElement.text, _uiStyle);
                    yPos += 25f;
                }
            }
        }
    }
}
