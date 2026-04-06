using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> _spawnedPlayers = new();
    private int _teamACount;
    private int _teamBCount;
    private GameObject[] _spawnPointsA;
    private GameObject[] _spawnPointsB;

    public override void Spawned()
    {
        _spawnPointsA = GameObject.FindGameObjectsWithTag("SpawnPointA");
        _spawnPointsB = GameObject.FindGameObjectsWithTag("SpawnPointB");
        Debug.Log($"SpawnPointA: {_spawnPointsA.Length}, SpawnPointB: {_spawnPointsB.Length}");
        // Không spawn ở đây — SpawnCallbackHandler.OnSceneLoadDone sẽ xử lý
        // để đảm bảo map collider đã load xong
    }

    public void SpawnPlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (_spawnedPlayers.ContainsKey(player)) return;

        // Lấy team từ RoomPlayerData nếu có, không thì cân bằng
        int team = GetTeamForPlayer(player);
        if (team == 0) _teamACount++;
        else           _teamBCount++;

        Vector3 spawnPos = GetPublicSpawnPoint(team);

        NetworkObject playerObj = Runner.Spawn(
            playerPrefab,
            spawnPos,
            Quaternion.identity,
            player,
            (runner, obj) => {
                obj.transform.position = spawnPos;
                var fps = obj.GetComponent<FPSController>();
                fps?.InitSpawnPosition(spawnPos);

                var np = obj.GetComponent<NetworkPlayer>();
                if (np != null) np.Team = team;
            }
        );

        if (playerObj == null)
        {
            Debug.LogError($"Spawn returned null for player {player}!");
            return;
        }

        _spawnedPlayers[player] = playerObj;
        Debug.Log($"Spawned player {player} team {team} at {spawnPos}");
    }

    int GetTeamForPlayer(PlayerRef player)
    {
        // Đọc team từ RoomPlayerData nếu còn tồn tại
        if (RoomPlayerData.instance != null)
        {
            var slots = RoomPlayerData.instance.GetOccupied();
            foreach (var slot in slots)
                if (slot.PlayerRef == player)
                    return slot.Team;
        }
        // Fallback: cân bằng team
        return _teamACount <= _teamBCount ? 0 : 1;
    }

    public void DespawnPlayer(PlayerRef player)
    {
        if (!Object.HasStateAuthority) return;
        if (_spawnedPlayers.TryGetValue(player, out var obj))
        {
            if (obj != null) Runner.Despawn(obj);
            _spawnedPlayers.Remove(player);
        }
    }

    public Vector3 GetPublicSpawnPoint(int team)
    {
        GameObject[] points = team == 0 ? _spawnPointsA : _spawnPointsB;
        if (points == null || points.Length == 0)
            return new Vector3(team == 0 ? -10f : 10f, 1f, Random.Range(-5f, 5f));

        // Shuffle để random thứ tự kiểm tra
        var shuffled = new List<GameObject>(points);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }

        // Tìm spawn point không có player đứng
        float checkRadius = 1.2f; // bán kính CharacterController
        foreach (var point in shuffled)
        {
            Vector3 pos = point.transform.position;
            bool occupied = false;
            foreach (var spawned in _spawnedPlayers.Values)
            {
                if (spawned == null) continue;
                if (Vector3.Distance(spawned.transform.position, pos) < checkRadius)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied) return pos;
        }

        // Fallback: tất cả đều bị chiếm thì random bình thường
        return shuffled[Random.Range(0, shuffled.Count)].transform.position;
    }
}