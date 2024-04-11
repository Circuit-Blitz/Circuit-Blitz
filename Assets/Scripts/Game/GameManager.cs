using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

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
        public double _startTime;
        public bool _started;
        public bool _ended;
        
        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref _startTime);
            serializer.SerializeValue(ref _started);
            serializer.SerializeValue(ref _ended);
        }
    }
    private readonly NetworkVariable<GameState> CurrentGameState = new(new GameState {
        _started = false,
        _ended = false,
    }, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [SerializeField] private NetworkList<ulong> Placements;
    
    [SerializeField] private UDictionary<ulong, int> ServerCurrentCheckpoints = new();
    [SerializeField] private UDictionary<ulong, Transform> ServerPlayerTransforms = new();
    [SerializeField] private UDictionary<ulong, double> ServerClearTimes = new();
    
    [SerializeField] private int LocalCurrentCheckpoint;
    [SerializeField] private UDictionary<ulong, double> LocalClearTimes = new();

    private double StartTime {
        get {
            return CurrentGameState.Value._startTime;
        }
        set {
            GameState newState = CurrentGameState.Value;
            newState._startTime = value;
            CurrentGameState.Value = newState;
        }
    }

    private bool GameStarted {
        get {
            return CurrentGameState.Value._started;
        }
        set {
            GameState newState = CurrentGameState.Value;
            newState._started = value;
            CurrentGameState.Value = newState;
        }
    }

    private bool GameEnded {
        get {
            return CurrentGameState.Value._ended;
        }
        set {
            GameState newState = CurrentGameState.Value;
            newState._ended = value;
            CurrentGameState.Value = newState;
        }
    }

    void Awake() {
        // Set the global instance
        _instance = this;

        // Initialize placements
        Placements = new NetworkList<ulong>(
            new List<ulong>(),
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );
    }

    public NetworkList<ulong> GetPlacements() {
        return Placements;
    }

    public bool DidGameEnd() {
        return GameEnded;
    }

    public bool DidGameStart() {
        return GameStarted;
    }

    public bool DidPlayerClear(ulong playerId) {
        return LocalClearTimes.ContainsKey(playerId);
    }

    public double GetPlayerClearTime(ulong playerId) {
        LocalClearTimes.TryGetValue(playerId, out double clearTime);
        return clearTime;
    }
    
    private void UpdatePlacements() {
        // Only the server is allowed to write to placements (NetworkList)
        if (!IsServer) return;

        // Update the placements
        Placements.Clear();
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
            Placements.Add(_playerId);
        }
    }
    
    [Rpc(SendTo.Everyone)]
    private void PlayerFinishedRpc(ulong playerId, double clearTime) {
        LocalClearTimes[playerId] = clearTime;
    }

    private void UpdateCheckpointsCollectedLocal() {
        // LocalPlayer could be destroyed or null
        if (!LocalPlayer) return;
        // Get local player's position
        Vector3 pos = LocalPlayer.position;
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

    private void UpdateCheckpointCollectedServer(ulong playerId, Vector3 pos) {
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
                ServerClearTimes.Add(playerId, Time.timeAsDouble - StartTime);
                PlayerFinishedRpc(playerId, ServerClearTimes[playerId]);

                // Check if the current game has ended
                // If it did, then return all players back to the main menu
                TryEndGame();
            }
            // Only the server runs this
            UpdatePlacements();
        }
    }

    private void Update() {
        // Update checkpoints for local
        UpdateCheckpointsCollectedLocal();

        // Server time >:)
        if (!IsServer) return;

        foreach (KeyValuePair<ulong, Transform> player in ServerPlayerTransforms)
        {
            ulong playerId = player.Key;

            // Player transform gets destroyed when the player leaves midgame
            Transform playerTransform = player.Value;
            if (!playerTransform) continue;

            UpdateCheckpointCollectedServer(playerId, playerTransform.position);
        }
    }

    private void TryEndGame() {
        // Game has already ended, do nothing
        if (DidGameEnd()) return;
        // If all players connected to the server right now has cleared the track
        // Then the game has ended, return all players to the main lobby
        if (ServerManager.Instance.GetConnectedPlayerIds().All(playerId => ServerClearTimes.Keys.Contains(playerId))) {
            GameEnded = true;
            ReturnToLobbyRpc();
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
        foreach(ulong playerId in ServerManager.Instance.GetConnectedPlayerIds())
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
            GameStarted = true;
            
            // Set the start timestamp
            StartTime = Time.timeAsDouble;
        }
    }

    public override void OnNetworkSpawn() {
        // Spawn every player's car
        SpawnPlayerRpc(NetworkManager.Singleton.LocalClientId);
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
        // Remove the disconnected player from the LocalClearTimes dictionary
        LocalClearTimes.Remove(playerId);

        if (IsServer) {
            // Remove the disconnected player from relevant dictionaries
            // Then, update the placements so other players get their leaderboard updated
            ServerCurrentCheckpoints.Remove(playerId);
            ServerPlayerTransforms.Remove(playerId);
            ServerClearTimes.Remove(playerId);
            UpdatePlacements();
            // When a player disconnects, the server checks if the remaining
            // players have cleared the track, if so then the game has ended
            TryEndGame();
        }
    }
}
