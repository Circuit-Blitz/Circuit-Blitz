using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public class GameOptionsUI : MonoBehaviour
{
    [SerializeField] private Button StartGameBtn;
    [SerializeField] private RectTransform PlayerList;
    [SerializeField] private RectTransform UsernameTag;
    private void Awake()
    {
        StartGameBtn.onClick.AddListener(() => {
            GameManager.Instance.StartGame();
        });
        
        UpdatePlayerList();
    }

    public void UpdatePlayerList()
    {
        foreach (Transform child in PlayerList.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (KeyValuePair<ulong, String> kv in GameManager.Instance.getPlayerListIterator()) {
            AddPlayer(kv.Key);
        }
    }
    
    private void AddPlayer(ulong playerId) {
        RectTransform tag = Instantiate(UsernameTag);
        tag.transform.SetParent(PlayerList);
        tag.name = playerId.ToString();

        PlayerList.sizeDelta = new Vector2(PlayerList.sizeDelta.x, PlayerList.childCount * 50 + 30);

        tag.offsetMin = new Vector2(0, 0);
        tag.offsetMax = new Vector2(0, (PlayerList.childCount - 1) * -100 - 80);
        tag.sizeDelta = new Vector2(0, 50);
        tag.GetComponent<Text>().text = GameManager.Instance.getPlayerUsername(playerId);
    }

    public void SetUsername(ulong playerId, String username) {
        PlayerList.Find(playerId.ToString()).GetComponent<Text>().text = username;
    }
}
