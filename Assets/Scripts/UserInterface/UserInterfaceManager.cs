using UnityEngine;

public class UserInterfaceManager : MonoBehaviour
{
    // All UI Elements to show
    [SerializeField] private Transform[] Show;

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
        foreach (Transform child in Show)
        {
            child.gameObject.SetActive(true);
        }
    }
}
