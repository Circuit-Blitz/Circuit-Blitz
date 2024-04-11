using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GameOptionsUI : MonoBehaviour
{
    [SerializeField] private Button StartGameBtn;
    [SerializeField] private ScrollableTextList UsernameList;
    private void Awake()
    {
        // Disable the start game button if not server
        if (NetworkManager.Singleton.IsServer) {
            StartGameBtn.gameObject.SetActive(true);
            StartGameBtn.onClick.AddListener(() =>
            {
                ServerManager.Instance.StartGame();
            });
        } else {
            StartGameBtn.gameObject.SetActive(false);
        }

        UpdatePlayerList();
    }

    public void UpdatePlayerList()
    {
        UsernameList.Clear();

        foreach (KeyValuePair<ulong, string> kv in ServerManager.Instance.GetPlayerListIterator()) {
            UsernameList.AddText(kv.Key.ToString(), kv.Value);
        }
    }

    public void SetUsername(ulong playerId, string username) {
        UsernameList.SetText(playerId.ToString(), username);
    }
}
