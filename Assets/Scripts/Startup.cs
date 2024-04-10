using UnityEngine;
using UnityEngine.SceneManagement;

public class Startup : MonoBehaviour
{
    void Awake()
    {
        // We need a startup scene to prevent NetworkManager and ServerManager
        // From creating multiple duplicates of each other, very important
        SceneManager.LoadScene("Scenes/MainMenu");
    }
}