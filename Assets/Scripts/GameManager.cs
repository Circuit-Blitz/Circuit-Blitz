using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Transform PlayerPrefab;
    [SerializeField] private String mapName;

    public override void OnNetworkSpawn()
    {
        if (IsServer) {
            NetworkManager.SceneManager.OnSceneEvent += SceneManager_OnSceneEvent;
        }
    }

    void Awake() {
        // Don't destroy this GameObject when a scene is loaded
        DontDestroyOnLoad(this);
    }
    
    public void SetMap(String mapName) {
        this.mapName = mapName;
    }
    
    public void StartGame() {
        NetworkManager.SceneManager.LoadScene(this.mapName, LoadSceneMode.Single);
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
    void SpawnCarRpc(ulong localPlayerId) {
        // Instantiate on server
        Transform Player = Instantiate(PlayerPrefab);
        Player.GetComponent<NetworkObject>().SpawnWithOwnership(localPlayerId);
    }
}
