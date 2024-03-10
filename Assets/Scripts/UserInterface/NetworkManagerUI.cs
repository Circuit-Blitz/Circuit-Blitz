using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] private Button serverBtn;
    [SerializeField] private Button hostBtn;
    [SerializeField] private Button clientBtn;
    [SerializeField] private Transform MapSelectionUI;
    
    private void Awake() {
        hostBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartHost();
            gameObject.SetActive(false);
            MapSelectionUI.gameObject.SetActive(true);
        });
        clientBtn.onClick.AddListener(() => {
            NetworkManager.Singleton.StartClient();
            gameObject.SetActive(false);
        });
    }
}
