using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UsernameSelectionUI : MonoBehaviour
{
    [SerializeField] private Button NextBtn;
    [SerializeField] private TMP_InputField NameField;
    [SerializeField] private Transform MapSelectionUI;
    [SerializeField] private Transform GameOptionsUI;

    private void Awake()
    {
        NextBtn.onClick.AddListener(() => {
            // Do not proceed if the username field is not filled out
            if (NameField.text.Length == 0) return;
            
            gameObject.SetActive(false);
            if (NetworkManager.Singleton.IsServer) {
                MapSelectionUI.gameObject.SetActive(true);
            } else {
                GameOptionsUI.gameObject.SetActive(true);
            }
            
            ServerManager.Instance.SetUsernameRpc(NetworkManager.Singleton.LocalClientId, NameField.text);
        });
    }
}
