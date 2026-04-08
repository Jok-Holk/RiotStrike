using System.Collections.Generic;
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

        if (endGamePanel != null)
            endGamePanel.SetActive(false);

        if (!Object.HasStateAuthority) return;

        // Đọc config từ RoomPlayerData
        if (RoomPlayerData.instance != null)
        {
            int pistolTime = RoomPlayerData.instance.PistolTime;
            int rifleTime  = RoomPlayerData.instance.RifleTime;
            int roundTime  = pistolTime + rifleTime;

            if (timerManager != null)
                timerManager.SetTimings(roundTime, pistolTime);

            Debug.Log($"[GameManager] Round={roundTime}s Pistol={pistolTime}s Rifle={rifleTime}s");
        }
        else
        {
            Debug.LogWarning("[GameManager] RoomPlayerData not found, using TimerManager defaults.");
        }

        TeamAScore  = 0;
        TeamBScore  = 0;
        GameStarted = true;
        GameEnded   = false;
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

        // Kiểm tra hết giờ
        if (timerManager != null && timerManager.IsTimeUp())
            EndGame();
    }

    // Gọi từ PlayerHealth khi có kill
    public void RegisterKill(int killerTeam)
    {
        if (!Object.HasStateAuthority) return;
        if (GameEnded) return;

        if (killerTeam == 0) TeamAScore++;
        else                 TeamBScore++;

        Debug.Log($"[GameManager] Kill registered. TeamA={TeamAScore} TeamB={TeamBScore}");
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

    void ShowEndGame()
    {
        ShowEndGameResult(TeamAScore, TeamBScore);
    }

    void ShowEndGameResult(int scoreA, int scoreB)
    {
        if (endGamePanel != null)
            endGamePanel.SetActive(true);

        if (endGameText != null)
        {
            if (scoreA > scoreB)
                endGameText.text = $"TEAM A THẮNG!\n{scoreA} - {scoreB}";
            else if (scoreB > scoreA)
                endGameText.text = $"TEAM B THẮNG!\n{scoreA} - {scoreB}";
            else
                endGameText.text = $"HÒA!\n{scoreA} - {scoreB}";
        }

        Debug.Log($"[GameManager] Game ended. A={scoreA} B={scoreB}");
    }

    void UpdateScoreUI()
    {
        if (teamAScoreText != null) teamAScoreText.text = TeamAScore.ToString();
        if (teamBScoreText != null) teamBScoreText.text = TeamBScore.ToString();
    }
}