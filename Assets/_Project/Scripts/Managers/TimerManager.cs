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
    [Networked] private bool _isGameOver { get; set; }

    public bool IsTimeUp() => _networkTime <= 0f;

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

        // Nếu đã kết thúc rồi thì không chạy tiếp nữa
        if (_isGameOver) return;

        if (_networkTime > 0)
        {
            _networkTime -= Runner.DeltaTime;
        }
        else
        {
            // CHỖ NÀY LÀ HẾT GIỜ
            _networkTime = 0;
            _isGameOver = true;

            // Gọi RPC để thông báo cho tất cả người chơi hiện UI kết quả
            RPC_FinishMatch();
        }

        // Logic unlock Rifle của bạn giữ nguyên...
        if (!_rifleUnlocked && _networkTime <= (totalTime - pistolOnlyDuration))
        {
            _rifleUnlocked = true;
            RPC_OnRifleUnlocked();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_FinishMatch()
    {
        // Giả sử bạn muốn tính điểm từ một script Manager nào đó hoặc truyền từ Host
        // Ở đây tôi ví dụ gọi instance của EndRoundUI bạn đã tạo
        if (EndRoundUI.instance != null)
        {
            // Ví dụ: Hiển thị kết quả Hòa hoặc tính toán thắng thua ở đây
            EndRoundUI.instance.ShowResult("HẾT GIỜ!", 0, 0);
        }

        Debug.Log("Match Finished!");
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
    }
}