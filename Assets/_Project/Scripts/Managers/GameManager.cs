using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager instance;

    [Header("References")]
    [SerializeField] private TimerManager timerManager;

    [Header("Score UI")]
    [SerializeField] private TextMeshProUGUI teamAScoreText;
    [SerializeField] private TextMeshProUGUI teamBScoreText;
    [SerializeField] private GameObject      endGamePanel;
    [SerializeField] private TextMeshProUGUI endGameText;

    [Networked] public int  TeamAScore  { get; set; }
    [Networked] public int  TeamBScore  { get; set; }
    [Networked] public bool GameStarted { get; set; }
    [Networked] public bool GameEnded   { get; set; }

    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        instance = this;
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (endGamePanel != null) endGamePanel.SetActive(false);

        if (!Object.HasStateAuthority) return;

        TeamAScore  = 0;
        TeamBScore  = 0;
        GameStarted = true;
        GameEnded   = false;

        // Dùng coroutine để chờ RoomPlayerData sync xong trước khi đọc config
        // Tránh đọc giá trị mặc định (0) khi RoomPlayerData chưa sẵn sàng
        StartCoroutine(ApplyLobbyConfig());
    }

    IEnumerator ApplyLobbyConfig()
    {
        // Chờ tối đa 3 giây cho RoomPlayerData
        float timeout = 3f;
        while (RoomPlayerData.instance == null && timeout > 0f)
        {
            timeout -= Time.deltaTime;
            yield return null;
        }

        if (RoomPlayerData.instance != null)
        {
            int pistolTime = RoomPlayerData.instance.PistolTime;
            int rifleTime  = RoomPlayerData.instance.RifleTime;
            int roundTime  = pistolTime + rifleTime;

            // Chỉ apply nếu có giá trị hợp lệ (> 0)
            if (roundTime > 0 && timerManager != null)
            {
                timerManager.SetTimings(roundTime, pistolTime);
                Debug.Log($"[GameManager] Config từ lobby: Round={roundTime}s Pistol={pistolTime}s Rifle={rifleTime}s");
            }
            else
            {
                Debug.LogWarning($"[GameManager] RoomPlayerData có nhưng roundTime={roundTime} — dùng default của TimerManager.");
            }
        }
        else
        {
            Debug.LogWarning("[GameManager] RoomPlayerData không tìm thấy sau 3s — dùng default TimerManager.");
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(TeamAScore):
                case nameof(TeamBScore):
                    UpdateScoreUI();
                    break;
                case nameof(GameEnded):
                    if (GameEnded) ShowEndGame();
break;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!GameStarted || GameEnded) return;
        if (timerManager != null && timerManager.IsTimeUp())
            EndGame();
    }

    public void RegisterKill(int killerTeam)
    {
        if (!Object.HasStateAuthority) return;
        if (GameEnded) return;
        if (killerTeam == 0) TeamAScore++;
        else                 TeamBScore++;
        Debug.Log($"[GameManager] Kill. TeamA={TeamAScore} TeamB={TeamBScore}");
    }

    void EndGame()
    {
        GameEnded = true;
        RPC_NotifyGameEnd(TeamAScore, TeamBScore);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    void RPC_NotifyGameEnd(int scoreA, int scoreB)
    {
        ShowEndGameResult(scoreA, scoreB);
    }

    void ShowEndGame() => ShowEndGameResult(TeamAScore, TeamBScore);

    void ShowEndGameResult(int scoreA, int scoreB)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible   = true;

        string result;
        if (scoreA > scoreB)      result = "TEAM A THẮNG!";
        else if (scoreB > scoreA) result = "TEAM B THẮNG!";
        else                      result = "HÒA!";

        if (EndRoundUI.instance != null)
            EndRoundUI.instance.ShowResult(result, scoreA, scoreB);
        else
        {
            if (endGamePanel != null) endGamePanel.SetActive(true);
            if (endGameText  != null) endGameText.text = $"{result}\n{scoreA} - {scoreB}";
        }

        Debug.Log($"[GameManager] Game ended. A={scoreA} B={scoreB}");
    }

    void UpdateScoreUI()
    {
        if (teamAScoreText != null) teamAScoreText.text = TeamAScore.ToString();
        if (teamBScoreText != null) teamBScoreText.text = TeamBScore.ToString();
    }
}