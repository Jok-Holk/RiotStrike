/// <summary>
/// Static storage cho lobby config — lưu trước khi load game scene
/// để GameManager đọc được dù RoomPlayerData bị null do scene transition.
/// Chỉ host cập nhật; client đọc từ TimerManager (networked) sau khi host set.
/// </summary>
public static class GameConfig
{
    public static int WaitTime   = 10;
    public static int PistolTime = 60;
    public static int RifleTime  = 120;

    public static int RoundTime => PistolTime + RifleTime;

    /// Lưu giá trị từ RoomPlayerData ngay trước khi load game scene.
    public static void SaveFromRoomData()
    {
        if (RoomPlayerData.instance == null) return;
        WaitTime   = RoomPlayerData.instance.WaitTime;
        PistolTime = RoomPlayerData.instance.PistolTime;
        RifleTime  = RoomPlayerData.instance.RifleTime;
        UnityEngine.Debug.Log($"[GameConfig] Saved: wait={WaitTime}s pistol={PistolTime}s rifle={RifleTime}s");
    }
}
