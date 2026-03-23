using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNameplate : NetworkBehaviour
{
    [SerializeField] private GameObject nameplateRoot;
    [SerializeField] private TextMeshPro nameText;
    [SerializeField] private Color teammateColor = Color.cyan;
    [SerializeField] private Color enemyColor = Color.red;

    private Transform _cameraTransform;

    public override void Spawned()
    {
        // Ẩn nameplate của chính mình
        if (Object.HasInputAuthority)
        {
            nameplateRoot.SetActive(false);
            return;
        }

        // Lấy camera local
        _cameraTransform = Camera.main?.transform;

        UpdateNameplate();
    }

    public override void Render()
    {
        if (!Object.HasInputAuthority && nameplateRoot.activeSelf)
        {
            // Billboard — luôn nhìn về phía camera
            if (_cameraTransform != null)
                nameplateRoot.transform.LookAt(
                    nameplateRoot.transform.position + _cameraTransform.rotation * Vector3.forward,
                    _cameraTransform.rotation * Vector3.up
                );
        }
    }

    void UpdateNameplate()
    {
        var networkPlayer = GetComponent<NetworkPlayer>();
        if (networkPlayer == null) return;

        nameText.text = networkPlayer.NickName.ToString();

        // Tìm local player để so team
        foreach (var player in Runner.ActivePlayers)
        {
            if (Runner.TryGetPlayerObject(player, out NetworkObject localObj))
            {
                if (localObj.HasInputAuthority)
                {
                    var localNetPlayer = localObj.GetComponent<NetworkPlayer>();
                    if (localNetPlayer == null) break;

                    bool isTeammate = localNetPlayer.Team == networkPlayer.Team;
                    nameText.color = isTeammate ? teammateColor : enemyColor;
                    break;
                }
            }
        }
    }
}