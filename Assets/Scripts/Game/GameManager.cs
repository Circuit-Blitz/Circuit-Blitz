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
    public Transform Checkpoints;
    private Transform LocalPlayer;
    
    public struct GameState : INetworkSerializable {
        public bool _started;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _started);
        }
    }
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(new GameState {
        _started = false,
    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private NetworkList<ulong> placements;
    
    [SerializeField] private UDictionary<ulong, int> currentCheckpoint = new UDictionary<ulong, int>();
    
    [SerializeField] private UDictionary<ulong, Transform> playerTransformList = new UDictionary<ulong, Transform>();

    void Awake() {
        // Set the global instance
        _instance = this;

        // Initialize placements
        placements = new NetworkList<ulong>(
            new List<ulong>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public NetworkList<ulong> getPlacements() {
        return placements;
    }

    public bool isGameStarted() {
        return gameState.Value._started;
    }
    
    private void updatePlacements() {
        // Only the server is allowed to write to placements (NetworkList)
        if (!IsServer) return;

        // Update the placements
        placements.Clear();
        List<ulong> newPlacements = currentCheckpoint.Keys.OrderByDescending(playerId => currentCheckpoint[playerId]).ToList();
        foreach (ulong _playerId in newPlacements)
        {
            placements.Add(_playerId);
        }
    }

    private void updateCheckpointCollected(ulong playerId, Vector3 pos) {
        BoxCollider nextCheckpointCollider = Checkpoints.transform.GetChild(currentCheckpoint[playerId]).GetComponent<BoxCollider>();
        if (nextCheckpointCollider.bounds.Contains(pos)) {
            currentCheckpoint[playerId]++;
            updatePlacements();
        }
    }

    private void Update() {
        foreach (KeyValuePair<ulong, Transform> player in playerTransformList)
        {
            ulong playerId = player.Key;

            // Player transform gets destroyed when the player leaves midgame
            Transform playerTransform = player.Value;
            if (!playerTransform) continue;

            updateCheckpointCollected(playerId, playerTransform.position);
        }
    }

    IEnumerator StartGame()
    {
        // Initialize numCheckpointsCollected
        foreach(ulong playerId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            currentCheckpoint.Add(playerId, 0);
        }

        //yield on a new YieldInstruction that waits for 5 seconds.
        yield return new WaitForSeconds(5);

        GameState newState = gameState.Value;
        newState._started = true;
        gameState.Value = newState;
    }

    public override void OnNetworkSpawn() {
        // Spawn every player's car
        SpawnPlayerRpc(NetworkManager.LocalClientId);
        // Start the game
        StartCoroutine(StartGame());
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
        playerTransformList[playerId] = Player;
        
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
    
    public void PlayerDisconnected(ulong playerId)  {
        currentCheckpoint.Remove(playerId);
        playerTransformList.Remove(playerId);
        updatePlacements();
    }
}
