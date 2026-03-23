using Fusion;
using UnityEngine;
using System.Collections.Generic;

public class PlayerDebugLogger : MonoBehaviour
{
    private static PlayerDebugLogger _instance;
    private static List<FPSController> _players = new();
    private float _timer;

    void Awake()
    {
        if (_instance != null) { Destroy(gameObject); return; }
        _instance = this;
    }

    public static void Register(FPSController controller)
    {
        if (!_players.Contains(controller))
            _players.Add(controller);
    }

    public static void Unregister(FPSController controller)
    {
        _players.Remove(controller);
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer < 1f) return;
        _timer = 0f;

        foreach (var p in _players)
        {
            if (p == null) continue;
            Debug.Log($"[ALL_POS] Owner={p.Object.InputAuthority} InputAuth={p.Object.HasInputAuthority} StateAuth={p.Object.HasStateAuthority} | pos={p.transform.position}");
        }
    }
}