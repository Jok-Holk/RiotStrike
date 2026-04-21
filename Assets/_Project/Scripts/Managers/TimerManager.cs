using UnityEngine;
using TMPro;
using Fusion;

public class TimerManager : NetworkBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;

    [Networked] private float _networkTime { get; set; }
    [Networked] private bool _rifleUnlocked { get; set; }

    [Header("Settings — set từ HostConfigManager")]
    public float totalTime = 600f;
    public float pistolOnlyDuration = 120f;

    private bool _initialized = false;

    public bool IsTimeUp() => _networkTime <= 0f;
    public float GetRemainingTime() => Mathf.Max(0f, _networkTime); // dùng cho ScoreboardUI

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            // Đọc từ GameConfig (lưu trước khi load scene) thay vì dùng Inspector default (600s).
            // Tránh trường hợp timer hiện 10:00 rồi nhảy về 3:00 khi GameManager apply config sau.
            float configRound  = GameConfig.RoundTime  > 0 ? GameConfig.RoundTime  : totalTime;
            float configPistol = GameConfig.PistolTime > 0 ? GameConfig.PistolTime : pistolOnlyDuration;
            totalTime           = configRound;
            pistolOnlyDuration  = configPistol;
            _networkTime        = configRound;
            _rifleUnlocked      = false;
            Debug.Log($"[TimerManager] Spawned: networkTime={_networkTime}s pistolOnly={pistolOnlyDuration}s (from GameConfig)");
        }
        _initialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (_networkTime <= 0) return;

        _networkTime -= Runner.DeltaTime;

        if (!_rifleUnlocked && _networkTime <= (totalTime - pistolOnlyDuration))
        {
            _rifleUnlocked = true;
            RPC_OnRifleUnlocked();
        }
    }

    public override void Render()
    {
        if (!_initialized) return;
        UpdateUI();
    }

    void UpdateUI()
    {
        float t = Mathf.Max(0, _networkTime);
        int minutes = Mathf.FloorToInt(t / 60);
        int seconds = Mathf.FloorToInt(t % 60);

        if (timerText) timerText.text = $"{minutes:00}:{seconds:00}";

        if (phaseText)
        {
            if (_rifleUnlocked)
            {
                phaseText.text = "RIFLE UNLOCKED";
                phaseText.color = Color.green;
                if (timerText) timerText.color = Color.green;
            }
            else
            {
                phaseText.text = "PISTOL ONLY";
                phaseText.color = Color.yellow;
                if (timerText) timerText.color = Color.yellow;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_OnRifleUnlocked()
    {
        Debug.Log("Rifles unlocked!");
        foreach (var wc in FindObjectsByType<WeaponController>(FindObjectsSortMode.None))
            wc.OnRifleUnlocked();
    }

    public void SetTimings(float roundTime, float pistolTime)
    {
        if (!Object.HasStateAuthority) return;
        totalTime = roundTime;
        pistolOnlyDuration = pistolTime;
        _networkTime = roundTime;
        _rifleUnlocked = false;
        Debug.Log($"[TimerManager] SetTimings: round={roundTime}s pistol={pistolTime}s");
    }
}