using UnityEngine;

public class PlayerController : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            // Assuming your AudioManager is a singleton accessible via an Instance property
            AudioManager.Instance.PostEvent("VONumbers1to9", this.gameObject);
        }
    }
}