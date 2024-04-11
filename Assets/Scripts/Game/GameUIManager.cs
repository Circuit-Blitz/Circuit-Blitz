using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    private static GameUIManager _instance;
    public static GameUIManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [SerializeField] private RectTransform Countdown;

    void Awake() {
        // Set the global instance
        _instance = this;
        // Hide the countdown
        Countdown.gameObject.SetActive(false);
    }
    
    void Start() {
        // Update placements when it changes
        GameManager.Instance.GetPlacements().OnListChanged += (NetworkListEvent<ulong> changeEvent) => {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<ulong>.EventType.Clear: {
                    placements.Clear();
                    break;
                }
                case NetworkListEvent<ulong>.EventType.Add: {
                    ulong playerId = changeEvent.Value;
                    string text = changeEvent.Index + 1 + ": " + ServerManager.Instance.GetPlayerUsername(playerId);
                    if (GameManager.Instance.DidPlayerClear(playerId)) {
                        text += " ~ " + GameManager.Instance.GetPlayerClearTime(playerId).ToString("F1");
                    }
                    placements.AddText(playerId.ToString(), text);
                    break;
                }
                default: {
                    break;
                }
            }
        };

        // Initialize the placementsUIList with a phony player list for now
        int counter = 0;
        foreach (KeyValuePair<ulong, string> player in ServerManager.Instance.GetPlayerListIterator())
        {
            ulong playerId = player.Key;
            string username = player.Value;
            placements.AddText(playerId.ToString(), ++counter + ": " + username);
        }
    }

    public IEnumerator StartCountdown(uint seconds) {
        // Show the countdown
        Countdown.gameObject.SetActive(true);
        for (int i = 0; i < seconds; i++)
        {
            Countdown.GetComponent<TMP_Text>().text = (seconds - i).ToString();
            yield return new WaitForSeconds(1);
        }
        // Hide the countdown
        Countdown.gameObject.SetActive(false);
    }

    [SerializeField] private ScrollableTextList placements;
}
