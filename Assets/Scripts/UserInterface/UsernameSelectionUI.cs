using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UsernameSelectionUI : MonoBehaviour
{
    [SerializeField] private Button nextBtn;
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private Transform MapSelectionUI;
    [SerializeField] private Transform GameOptionsUI;

    private void Awake()
    {
        nextBtn.onClick.AddListener(() => {
            // Do not proceed if the username field is not filled out
            if (nameField.text.Length == 0) return;
            
            gameObject.SetActive(false);
            if (NetworkManager.Singleton.IsServer) {
                MapSelectionUI.gameObject.SetActive(true);
            } else {
                GameOptionsUI.gameObject.SetActive(true);
            }
            
            GameManager.Instance.SetUsernameRpc(NetworkManager.Singleton.LocalClientId, nameField.text);
        });
    }
}
