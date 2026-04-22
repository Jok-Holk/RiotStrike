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
    private bool _timingSet   = false; // true khi SetTimings() đã được gọi

    public bool IsTimeUp() => _networkTime <= 0f;
    public float GetRemainingTime() => Mathf.Max(0f, _networkTime); // dùng cho ScoreboardUI

    public override void Spawned()
    {
        if (Object.HasStateAuthority && !_timingSet)
        {
            // SetTimings() chưa được gọi → tự khởi tạo từ GameConfig
            float configRound  = GameConfig.RoundTime  > 0 ? GameConfig.RoundTime  : totalTime;
            float configPistol = GameConfig.PistolTime > 0 ? GameConfig.PistolTime : pistolOnlyDuration;
            totalTime          = configRound;
            pistolOnlyDuration = configPistol;
            _networkTime       = configRound;
            _rifleUnlocked     = false;
            Debug.Log($"[TimerManager] Spawned (no SetTimings): networkTime={_networkTime}s");
        }
        else if (Object.HasStateAuthority)
        {
            Debug.Log($"[TimerManager] Spawned: giữ nguyên SetTimings = {_networkTime}s");
        }
        _initialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (_networkTime <= 0) return;

        // Phase 1: SafeZone đang đếm ngược → round timer chưa chạy
        // Chỉ bắt đầu đếm khi SafeZoneManager báo game đã start (hết chờ)
        if (SafeZoneManager.instance != null && !SafeZoneManager.instance.GameStarted) return;

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
        // Auto-find refs nếu Inspector reference bị mất sau merge
        // Chỉ tìm trong cùng Canvas để không nhầm với WaitCountdownUI hay panel khác
        if (timerText == null || phaseText == null) FindTextRefs();
        UpdateUI();
    }

    void FindTextRefs()
    {
        // Chỉ tìm trong ACTIVE objects — tránh lấy nhầm Text từ Canvas_HUD bị ẩn của player khác
        foreach (var tmp in FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            string n = tmp.gameObject.name;
            if (timerText == null && (n == "Text_Timer" || n == "TimerText" || n == "Timer" || n.Contains("_Timer")))
                timerText = tmp;
            if (phaseText == null && (n == "Text_Phase" || n == "PhaseText" || n == "Phase" || n.Contains("_Phase")))
                phaseText = tmp;
            if (timerText != null && phaseText != null) break;
        }
        if (timerText != null) Debug.Log($"[Timer] Found timerText: {timerText.gameObject.name}");
        else Debug.LogWarning("[Timer] timerText NOT found. TMP objects in scene: " +
            string.Join(", ", System.Array.ConvertAll(
                FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include, FindObjectsSortMode.None),
                t => t.gameObject.name)));
        if (phaseText != null) Debug.Log($"[Timer] Found phaseText: {phaseText.gameObject.name}");
    }

    void UpdateUI()
    {
        bool waitPhase = SafeZoneManager.instance != null && !SafeZoneManager.instance.GameStarted;

        // Phase 1: SafeZone countdown → WaitCountdownUI tự xử lý, TimerManager ẩn UI
        if (waitPhase)
        {
            if (timerText) timerText.text = "";
            if (phaseText) phaseText.text = "";
            return;
        }

        // Phase 2: Round timer
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
        _timingSet         = true;
        totalTime          = roundTime;
        pistolOnlyDuration = pistolTime;
        _networkTime       = roundTime;
        _rifleUnlocked     = false;
        Debug.Log($"[TimerManager] SetTimings: round={roundTime}s pistol={pistolTime}s");
    }
}