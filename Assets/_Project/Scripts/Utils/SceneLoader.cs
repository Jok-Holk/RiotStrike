using UnityEngine.SceneManagement;

public static class SceneLoader
{
    public static void LoadMainMenu() => SceneManager.LoadScene("MainMenu");
    public static void LoadLobby() => SceneManager.LoadScene("Lobby");
    public static void LoadGame() => SceneManager.LoadScene("Game");
}