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
        // Debug log vị trí đã tắt — di chuyển hoạt động ổn, không cần log nữa
    }
}