using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    private static GameUIManager _instance;
    public static GameUIManager Instance {
        get {
            if(_instance == null)
                Debug.LogError("Game Manager is NULL");
            return _instance;
        }
    }

    void Awake() {
        // Set the global instance
        _instance = this;
    }
    
    void Start() {
        // Update placements when it changes
        GameManager.Instance.getPlacements().OnListChanged += (NetworkListEvent<ulong> changeEvent) => {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<ulong>.EventType.Clear: {
                    placements.Clear();
                    break;
                }
                case NetworkListEvent<ulong>.EventType.Add: {
                    ulong playerId = changeEvent.Value;
                    string text = changeEvent.Index + 1 + ": " + ServerManager.Instance.GetPlayerUsername(playerId);
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
    
    [SerializeField] private ScrollableTextList placements;
}
