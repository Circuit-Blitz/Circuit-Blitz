using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : NetworkBehaviour
{
    private static GameManager _instance;
    public static GameManager Instance
    {
        get
        {
            return _instance;
        }
    }

    [SerializeField] private Transform RacecarPrefab;
    public List<Transform> SpawnPads;
    public Transform Checkpoints;
    private Transform LocalPlayer;
    
    public struct GameState : INetworkSerializable {
        public bool _started;
        public double _startTime;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _started);
            serializer.SerializeValue(ref _startTime);
        }
    }
    private NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(new GameState {
        _started = false,
    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private NetworkList<ulong> placements;
    
    [SerializeField] private UDictionary<ulong, int> ServerCurrentCheckpoints = new UDictionary<ulong, int>();
    
    [SerializeField] private UDictionary<ulong, Transform> ServerPlayerTransforms = new UDictionary<ulong, Transform>();

    [SerializeField] private UDictionary<ulong, double> ServerClearTimes = new UDictionary<ulong, double>();
    
    [SerializeField] private int LocalCurrentCheckpoint;
    [SerializeField] private UDictionary<ulong, double> LocalClearTimes = new UDictionary<ulong, double>();

    private double startTime {
        get {
            return gameState.Value._startTime;
        }
        set {
            GameState newState = gameState.Value;
            newState._startTime = value;
            gameState.Value = newState;
        }
    }

    private bool gameStarted {
        get {
            return gameState.Value._started;
        }
        set {
            GameState newState = gameState.Value;
            newState._started = value;
            gameState.Value = newState;
        }
    }

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
        return gameStarted;
    }

    public bool didPlayerClear(ulong playerId) {
        return LocalClearTimes.ContainsKey(playerId);
    }

    public double getPlayerClearTime(ulong playerId) {
        double clearTime = -1;
        LocalClearTimes.TryGetValue(playerId, out clearTime);
        return clearTime;
    }
    
    private void updatePlacements() {
        // Only the server is allowed to write to placements (NetworkList)
        if (!IsServer) return;

        // Update the placements
        placements.Clear();
        List<ulong> newPlacements = ServerCurrentCheckpoints.Keys.OrderByDescending(playerId => {
            // If the player has cleared it, then they are at the top
            // and are compared with other players who have also cleared it
            if (ServerClearTimes.ContainsKey(playerId)) {
                double clearTime = ServerClearTimes[playerId];
                return ServerCurrentCheckpoints[playerId] + 100/(1 + clearTime);  // Max extra points = 100
            } else {
                return ServerCurrentCheckpoints[playerId];
            }
        }).ToList();
        foreach (ulong _playerId in newPlacements)
        {
            placements.Add(_playerId);
        }
    }
    
    [Rpc(SendTo.Everyone)]
    private void playerFinishedRpc(ulong playerId, double clearTime) {
        LocalClearTimes[playerId] = clearTime;
    }

    private void updateCheckpointsCollectedLocal() {
        // LocalPlayer could be destroyed or null
        if (!LocalPlayer) return;
        // Get local player's position
        Vector3 pos = LocalPlayer.position;
        // Player id is local player's
        ulong playerId = NetworkManager.Singleton.LocalClientId;
        // Don't go out of bounds
        if (LocalCurrentCheckpoint >= Checkpoints.childCount) return;
        // Get the next checkpoint's collider
        Transform nextCheckpoint = Checkpoints.GetChild(LocalCurrentCheckpoint);
        Transform nextCheckpointHitBox = nextCheckpoint.Find("HitBox");
        BoxCollider nextCheckpointCollider = nextCheckpointHitBox.GetComponent<BoxCollider>();
        // Check if the position of the racecar is within the bounding box of the collider
        // If it is, then the player has touched their next checkpoint
        if (nextCheckpointCollider.bounds.Contains(pos)) {
            LocalCurrentCheckpoint++;
            // Hide the touched checkpoint, show the next one if there is a next
            nextCheckpoint.Find("Visible").gameObject.SetActive(false);
            // Don't go out of bounds
            if (LocalCurrentCheckpoint >= Checkpoints.childCount) return;
            // Get the next checkpoint and make it visible
            Transform nextnextCheckpoint = Checkpoints.GetChild(LocalCurrentCheckpoint);
            nextnextCheckpoint.Find("Visible").gameObject.SetActive(true);
        }
    }

    private void updateCheckpointCollectedServer(ulong playerId, Vector3 pos) {
        // Only the server is allowed to run this
        if (!IsServer) return;
        // Don't go out of bounds
        if (ServerCurrentCheckpoints[playerId] >= Checkpoints.childCount) return;
        // Get the next checkpoint's collider
        Transform nextCheckpoint = Checkpoints.GetChild(ServerCurrentCheckpoints[playerId]);
        Transform nextCheckpointHitBox = nextCheckpoint.Find("HitBox");
        BoxCollider nextCheckpointCollider = nextCheckpointHitBox.GetComponent<BoxCollider>();
        // Check if the position of the racecar is within the bounding box of the collider
        // If it is, then the player has touched their next checkpoint
        if (nextCheckpointCollider.bounds.Contains(pos)) {
            ServerCurrentCheckpoints[playerId]++;
            // Player has cleared the stage
            if (ServerCurrentCheckpoints[playerId] == Checkpoints.childCount) {
                ServerClearTimes.Add(playerId, Time.timeAsDouble - startTime);
                playerFinishedRpc(playerId, ServerClearTimes[playerId]);
                
                // If all players connected to the server right now has cleared the track
                // Then return all players to the main lobby
                if (NetworkManager.ConnectedClientsIds.All(playerId => ServerClearTimes.Keys.Contains(playerId))) {
                    ReturnToLobbyRpc();
                }
            }
            // Only the server runs this
            updatePlacements();
        }
    }

    private void Update() {
        // Update checkpoints for local
        updateCheckpointsCollectedLocal();

        // Server time >:)
        if (!IsServer) return;

        foreach (KeyValuePair<ulong, Transform> player in ServerPlayerTransforms)
        {
            ulong playerId = player.Key;

            // Player transform gets destroyed when the player leaves midgame
            Transform playerTransform = player.Value;
            if (!playerTransform) continue;

            updateCheckpointCollectedServer(playerId, playerTransform.position);
        }
    }


    [Rpc(SendTo.Everyone)]
    private void ReturnToLobbyRpc()
    {
        StartCoroutine(ReturnToLobby());
    }
    IEnumerator ReturnToLobby() {
        // Countdown timer
        const uint seconds = 10;

        // Start the UI countdown
        StartCoroutine(GameUIManager.Instance.StartCountdown(seconds));

        // yield on a new YieldInstruction that waits for X seconds.
        yield return new WaitForSeconds(seconds);

        if (IsServer) {
            // Despawn network objects
            foreach (Transform player in ServerPlayerTransforms.Values)
            {
                player.gameObject.GetComponent<CarController>().NetworkObject.Despawn(true);
            }
            // Shut down the server
            NetworkManager.Singleton.Shutdown();
        }
    }

    IEnumerator StartGame()
    {
        // Initialize numCheckpointsCollected
        foreach(ulong playerId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            ServerCurrentCheckpoints.Add(playerId, 0);
        }

        // Hide all checkpoints except the first one
        if (Checkpoints.childCount > 0) {
            Checkpoints.GetChild(0).Find("Visible").gameObject.SetActive(true);
            for (int i = 1; i < Checkpoints.childCount; i++)
            {
                Checkpoints.GetChild(i).Find("Visible").gameObject.SetActive(false);
            }
        }

        // Countdown seconds
        const uint seconds = 5;

        // Start the UI countdown
        StartCoroutine(GameUIManager.Instance.StartCountdown(seconds));

        // Only the server can modify these network variables
        if (IsServer) {
            // yield on a new YieldInstruction that waits for X seconds.
            yield return new WaitForSeconds(seconds);

            // Start the game
            gameStarted = true;
            
            // Set the start timestamp
            startTime = Time.timeAsDouble;
        }
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
        ServerPlayerTransforms[playerId] = Player;
        
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
        ServerCurrentCheckpoints.Remove(playerId);
        ServerPlayerTransforms.Remove(playerId);
        updatePlacements();
    }
}
