using UnityEngine;
public class ScoreboardUI : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private Transform teamAContent;
    [SerializeField] private Transform teamBContent;
    [SerializeField] private GameObject playerRowPrefab;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) { panel.SetActive(true); Refresh(); }
        if (Input.GetKeyUp(KeyCode.Tab)) panel.SetActive(false);
    }

    public void Refresh()
    {
        foreach (Transform c in teamAContent) Destroy(c.gameObject);
        foreach (Transform c in teamBContent) Destroy(c.gameObject);

        if (RoomPlayerData.instance == null) return;

        foreach (var slot in RoomPlayerData.instance.GetOccupied())
        {
            var parent = slot.Team == 0 ? teamAContent : teamBContent;
            var row = Instantiate(playerRowPrefab, parent);
            var texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            if (texts.Length > 0) texts[0].text = slot.NickName.ToString();
        }
    }
}