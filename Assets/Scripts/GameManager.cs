using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance {
        get {
            if(_instance is null)
                Debug.LogError("Game Manager is NULL");
            return _instance;
        }
    }

    [SerializeField] private Transform PlayerPrefab;
    [SerializeField] private String mapName;
    [SerializeField] private UDictionary<ulong, String> players = new UDictionary<ulong, string>();

    public override void OnNetworkSpawn()
    {
        if (IsServer) {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
        }
    }

    void Awake() {
        // Set the global instance
        _instance = this;
        // Don't destroy this GameObject when a scene is loaded
        DontDestroyOnLoad(this);
        // Event when new player joins
        NetworkManager.Singleton.OnConnectionEvent += AddPlayer;
    }
    
    public void SetMap(String mapName) {
        this.mapName = mapName;
    }

    public void AddPlayer(NetworkManager manager, ConnectionEventData data) {
        if (manager.IsServer && !players.ContainsKey(data.ClientId)) {
            Debug.Log(data.ClientId);

            // Replicate adding the player to everyone
            AddPlayerRpc(data.ClientId, "Player", RpcTarget.Everyone);

            // Sync current player list with new player
            foreach (KeyValuePair<ulong, String> kv in players)
            {
                AddPlayerRpc(kv.Key, kv.Value, RpcTarget.Single(data.ClientId, RpcTargetUse.Temp));
            }

            // If we are in the main menu, then sync Player List (User Interface)
            if (SceneManager.GetActiveScene().name == "MainMenu") {
                UpdatePlayerListUIRpc();
            }
        }
    }
    
    [Rpc(SendTo.Everyone)]
    public void UpdatePlayerListUIRpc() {
        if (GameObject.Find("UserInterface/GameOptions")) {
            GameObject.Find("UserInterface/GameOptions").GetComponent<GameOptionsUI>().UpdatePlayerList();
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void AddPlayerRpc(ulong playerId, String username, RpcParams rpcParams) {
        if (!players.ContainsKey(playerId)) {
            players.Add(playerId, username);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetUsernameRpc(ulong playerId, String newUsername) {
        if (players.ContainsKey(playerId)) {
            players[playerId] = newUsername;
        }
        // Changes the username in the playerlist
        if (SceneManager.GetActiveScene().name == "MainMenu" &&
            GameObject.Find("UserInterface/GameOptions"))
        {
            GameObject.Find("UserInterface/GameOptions").GetComponent<GameOptionsUI>().SetUsername(playerId, newUsername);
        }
    }

    public void printPlayerList() {
        foreach (KeyValuePair<ulong, String> kv in getPlayerListIterator())
        {
            Debug.LogError(kv.Key + " : " + kv.Value);
        }
    }

    public void StartGame() {
        NetworkManager.SceneManager.LoadScene(this.mapName, LoadSceneMode.Single);
    }
    
    public String getPlayerUsername(ulong playerId) {
        return players[playerId];
    }

    public IEnumerable<KeyValuePair<ulong, String>> getPlayerListIterator() {
        foreach (KeyValuePair<ulong, String> kv in players)
        {
            yield return kv;
        }
    }

    private void SceneManager_OnSceneEvent(SceneEvent sceneEvent) {
        switch (sceneEvent.SceneEventType) {
            case SceneEventType.LoadComplete:
            {
                if (sceneEvent.ClientId == NetworkManager.ServerClientId)
                {
                    // Spawn the car for players when the scene is loaded
                    StartGameRpc();
                }
                break;
            }
        }
    }

    [Rpc(SendTo.Everyone)]
    public void StartGameRpc() {
        SpawnCarRpc(NetworkManager.LocalClientId);
    }
    
    [Rpc(SendTo.Server)]
    void SpawnCarRpc(ulong playerId) {
        // Instantiate on server
        Transform Player = Instantiate(PlayerPrefab);
        Player.GetComponent<NetworkObject>().SpawnWithOwnership(playerId);
    }
}
