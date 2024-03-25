using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Button backBtn;
    [SerializeField] private Transform UsernameSelectionUI;
    [SerializeField] private Transform MainMenuUI;
    
    private void MoveToUsernameSelection() {
        gameObject.SetActive(false);
        UsernameSelectionUI.gameObject.SetActive(true);
    }
    
    private void Awake() {
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            MoveToUsernameSelection();
        });
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            MoveToUsernameSelection();
        });
        backBtn.onClick.AddListener(() => {
            gameObject.SetActive(false);
            MainMenuUI.gameObject.SetActive(true);
        });
    }
}
