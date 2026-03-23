using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private int _teamACount;
    private int _teamBCount;
    private GameObject[] _spawnPointsA;
    private GameObject[] _spawnPointsB;

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();
        if (_runner == null)
        {
            Debug.LogError("No NetworkRunner found!");
            return;
        }

        _spawnPointsA = GameObject.FindGameObjectsWithTag("SpawnPointA");
        _spawnPointsB = GameObject.FindGameObjectsWithTag("SpawnPointB");
        Debug.Log($"Cached SpawnPointA: {_spawnPointsA.Length}, SpawnPointB: {_spawnPointsB.Length}");
        Debug.Log($"Cached SpawnPointA: {_spawnPointsA.Length}, SpawnPointB: {_spawnPointsB.Length}");
        foreach (var p in _spawnPointsA)
            Debug.Log($"A: {p.name} at {p.transform.position}");
        foreach (var p in _spawnPointsB)
            Debug.Log($"B: {p.name} at {p.transform.position}");

        _runner.AddCallbacks(GetComponent<SpawnCallbackHandler>());
    }

    public void SpawnPlayer(PlayerRef player)
    {
        if (!_runner.IsServer) return;

        int team = _teamACount <= _teamBCount ? 0 : 1;
        if (team == 0) _teamACount++;
        else _teamBCount++;

        Vector3 spawnPos = GetSpawnPoint(team);

        NetworkObject playerObj = _runner.Spawn(
            playerPrefab,
            spawnPos,
            Quaternion.identity,
            player,
            (runner, obj) => {
                // Set position và NetworkedPosition TRƯỚC khi Spawned() chạy
                obj.transform.position = spawnPos;
                var fps = obj.GetComponent<FPSController>();
                if (fps != null) fps.InitSpawnPosition(spawnPos);
            }
        );

        if (playerObj == null)
        {
            Debug.LogError("Spawn returned null!");
            return;
        }

        var networkPlayer = playerObj.GetComponent<NetworkPlayer>();
        if (networkPlayer != null)
            networkPlayer.Team = team;

        _spawnedPlayers[player] = playerObj;
        Debug.Log($"Spawned player {player} on team {team} at {spawnPos}");
    }

    Vector3 GetSpawnPoint(int team)
    {
        GameObject[] points = team == 0 ? _spawnPointsA : _spawnPointsB;

        if (points == null || points.Length == 0)
            return new Vector3(team == 0 ? -10f : 10f, 1f, Random.Range(-5f, 5f));

        return points[Random.Range(0, points.Length)].transform.position;
    }
}