using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance {
        get {
            if(_instance == null)
                Debug.LogError("Game Manager is NULL");
            return _instance;
        }
    }
    
    [SerializeField] private Transform RacecarPrefab;
    public List<Transform> SpawnPads;
    public List<Transform> Checkpoints;
    private Transform LocalPlayer;

    void Awake() {
        // Set the global instance
        _instance = this;
    }

    public override void OnNetworkSpawn() {
        // Spawn every player's car
        SpawnPlayerRpc(NetworkManager.LocalClientId);
    }
    
    [Rpc(SendTo.Server)]
    public void SpawnPlayerRpc(ulong playerId) {
        // Do not spawn this player if the id does not exist in the player list
        if (!ServerManager.Instance.ContainsPlayer(playerId)) return;

        // Get a random pad to set the player's position to
        // Set the prefab's position to the pad's position so that interpolation doesn't mess up anything
        // Offset spawn's position upwards so that car does not clip into the ground
        int randInt = Random.Range(0, SpawnPads.Count);
        Transform Pad = SpawnPads[randInt];
        Vector3 spawnPos = Pad.position + Vector3.up * 4;
        RacecarPrefab.position = spawnPos;

        // Instantiate a new racecar as the player with ownership being given to the id
        Transform Player = Instantiate(RacecarPrefab);
        Player.GetComponent<NetworkObject>().SpawnWithOwnership(playerId);
        
        // Set the player's position to the random pad
        SetPositionOfPlayerRpc(spawnPos, RpcTarget.Single(playerId, RpcTargetUse.Temp));
        SpawnPads.RemoveAt(randInt);
    }

    public void SetLocalPlayer(Transform player) {
        LocalPlayer = player;
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void SetPositionOfPlayerRpc(Vector3 pos, RpcParams rpcParams) {
        // Set the player's position to the new position
        // Use rigidbody because it's what actually dictates the car's position
        LocalPlayer.GetComponent<Rigidbody>().position = pos;
    }
}
