using UnityEngine;
using TMPro;
using Fusion;

public class TimerManager : NetworkBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI phaseText;
    public TextMeshProUGUI timerText;

    // [Networked] để tất cả client thấy cùng thời gian — fix lỗi mỗi máy đếm khác nhau
    [Networked] private float _networkTime { get; set; }
    [Networked] private bool _rifleUnlocked { get; set; }

    [Header("Settings — set từ HostConfigManager")]
    public float totalTime = 600f;
    public float pistolOnlyDuration = 120f; // bao lâu chỉ dùng pistol trước khi rifle unlock

    private bool _initialized = false;

    public override void Spawned()
    {
        if (Object.HasStateAuthority)
        {
            _networkTime = totalTime;
            _rifleUnlocked = false;
        }
        _initialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (_networkTime <= 0) return;

        _networkTime -= Runner.DeltaTime;

        // Rifle unlock khi hết thời gian pistol only
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
        if (timerText) timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);

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
        // Thông báo cho WeaponController của từng player
        foreach (var wc in FindObjectsByType<WeaponController>(FindObjectsSortMode.None))
            wc.OnRifleUnlocked();
    }

    // Gọi từ HostConfigManager khi host set xong
    public void SetTimings(float roundTime, float pistolTime)
    {
        if (!Object.HasStateAuthority) return;
        totalTime = roundTime;
        pistolOnlyDuration = pistolTime;
        _networkTime = roundTime;
        _rifleUnlocked = false;
    }
}