using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerManager : NetworkBehaviour
{
    private static ServerManager _instance;
    public static ServerManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [SerializeField] private string MapName;
    [SerializeField] private UDictionary<ulong, string> PlayerList = new UDictionary<ulong, string>();

    public override void OnNetworkSpawn() {
        // Event when new player joins
        NetworkManager.Singleton.OnConnectionEvent += OnConnectionEvent;
        // Event when the server shuts down
        NetworkManager.Singleton.OnServerStopped += OnServerStopped;
        // Event when the local client stops
        NetworkManager.Singleton.OnClientStopped += OnClientStopped;
    }

    void Awake() {
        // Set the global instance
        _instance = this;
        // Don't destroy this GameObject when a scene is loaded
        DontDestroyOnLoad(this);
    }
    
    public void SetMap(string mapName) {
        MapName = mapName;
    }
    
    public void ClearPlayerList() {
        PlayerList.Clear();
        UpdatePlayerListUIRpc();
    }

    void OnClientStopped(bool _)
    {
        // Return back to the main menu
        SceneManager.LoadScene("Scenes/MainMenu");
        // Clear any leftover players from the player list
        ServerManager.Instance.ClearPlayerList();
    }

    public void OnServerStopped(bool _) {
        // Clear any leftover players from the player list
        ClearPlayerList();
    }

    public void OnConnectionEvent(NetworkManager manager, ConnectionEventData data) {
        switch (data.EventType)
        {
            case ConnectionEvent.ClientConnected: {
                if (manager.IsServer && !PlayerList.ContainsKey(data.ClientId)) {
                    // Replicate adding the player to everyone
                    AddPlayerRpc(data.ClientId, "Player", RpcTarget.Everyone);

                    // Sync current player list with new player
                    foreach (KeyValuePair<ulong, string> kv in PlayerList)
                    {
                        AddPlayerRpc(kv.Key, kv.Value, RpcTarget.Single(data.ClientId, RpcTargetUse.Temp));
                    }

                    // If we are in the main menu, then sync Player List (User Interface)
                    if (SceneManager.GetActiveScene().name == "MainMenu") {
                        UpdatePlayerListUIRpc();
                    }
                }

                break;
            }
            case ConnectionEvent.ClientDisconnected: {
                // Replicate removing the player to everyone
                RemovePlayerRpc(data.ClientId, RpcTarget.Everyone);

                // If we are in the main menu, then sync Player List (User Interface)
                if (SceneManager.GetActiveScene().name == "MainMenu") {
                    UpdatePlayerListUIRpc();
                }
                break;
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
    public void AddPlayerRpc(ulong playerId, string username, RpcParams rpcParams) {
        if (!PlayerList.ContainsKey(playerId)) {
            PlayerList.Add(playerId, username);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void RemovePlayerRpc(ulong playerId, RpcParams rpcParams) {
        if (PlayerList.ContainsKey(playerId)) {
            PlayerList.Remove(playerId);
        }
        
        // Let the game manager know if a player disconnected
        if (GameManager.Instance) {
            GameManager.Instance.PlayerDisconnected(playerId);
        }
    }

    [Rpc(SendTo.Everyone)]
    public void SetUsernameRpc(ulong playerId, string newUsername) {
        if (PlayerList.ContainsKey(playerId)) {
            PlayerList[playerId] = newUsername;
        }
        // Changes the username in the playerlist
        if (SceneManager.GetActiveScene().name == "MainMenu" &&
            GameObject.Find("UserInterface/GameOptions"))
        {
            GameObject.Find("UserInterface/GameOptions").GetComponent<GameOptionsUI>().SetUsername(playerId, newUsername);
        }
    }
    
    public void PrintPlayerList() {
        foreach (KeyValuePair<ulong, string> kv in GetPlayerListIterator())
        {
            Debug.LogError(kv.Key + " : " + kv.Value);
        }
    }

    public void StartGame() {
        NetworkManager.Singleton.SceneManager.LoadScene(MapName, LoadSceneMode.Single);
    }

    public string GetPlayerUsername(ulong playerId) {
        return PlayerList[playerId];
    }
    
    public bool ContainsPlayer(ulong playerId) {
        return PlayerList.ContainsKey(playerId);
    }

    public List<ulong> GetConnectedPlayerIds() {
        return PlayerList.Keys.ToList();
    }

    public List<string> GetConnectedPlayerUsernames() {
        return PlayerList.Values.ToList();
    }

    public IEnumerable<KeyValuePair<ulong, string>> GetPlayerListIterator() {
        foreach (KeyValuePair<ulong, string> kv in PlayerList)
        {
            yield return kv;
        }
    }
}