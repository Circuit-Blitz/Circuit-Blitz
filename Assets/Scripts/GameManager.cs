using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] private Transform PlayerPrefab;
    
    public override void OnNetworkSpawn() {
        if (IsClient && IsOwner) {
            SpawnCarRpc(NetworkManager.LocalClientId);

            // Disable the UI's camera
            Destroy(GameObject.Find("UICamera"));
        }
    }
    
    [Rpc(SendTo.Server)]
    void SpawnCarRpc(ulong localPlayerId) {
        if (IsServer) {
            // Instantiate on server
            Transform Player = Instantiate(PlayerPrefab);
            Player.GetComponent<NetworkObject>().SpawnWithOwnership(localPlayerId);
        }
    }
}
