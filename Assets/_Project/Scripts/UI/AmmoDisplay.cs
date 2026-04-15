using Fusion;
using TMPro;
using UnityEngine;

/// <summary>
/// Gắn vào Panel_Ammo trong Canvas_HUD của PlayerPrefab.
/// Tự tìm WeaponController của local player và hiển thị ammo.
/// </summary>
public class AmmoDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI currentAmmoText;  // vd: "30"
    [SerializeField] private TextMeshProUGUI reserveAmmoText;  // vd: "/ 90"
    [SerializeField] private TextMeshProUGUI weaponNameText;   // vd: "AK-47u" (optional)

    private WeaponController _wc;
    private int _lastAmmo    = -1;
    private int _lastReserve = -1;
    private int _lastSlot    = -1;

    void Update()
    {
        // Tìm WeaponController của local player
        if (_wc == null || !_wc.Object.IsValid)
        {
            _wc = null;
            foreach (var wc in FindObjectsByType<WeaponController>(FindObjectsSortMode.None))
            {
                if (wc.Object != null && wc.Object.IsValid && wc.Object.HasInputAuthority)
                {
                    _wc = wc;
                    break;
                }
            }
            return;
        }

        int ammo    = _wc.CurrentAmmo;
        int reserve = _wc.ReserveAmmo;
        int slot    = _wc.CurrentSlot;

        // Chỉ update UI khi có thay đổi
        if (ammo == _lastAmmo && reserve == _lastReserve && slot == _lastSlot) return;

        _lastAmmo    = ammo;
        _lastReserve = reserve;
        _lastSlot    = slot;

        if (currentAmmoText)  currentAmmoText.text  = ammo.ToString();
        if (reserveAmmoText)  reserveAmmoText.text   = $"/ {reserve}";

        if (weaponNameText)
        {
            if (slot == 0)
                weaponNameText.text = "PISTOL";
            else
                weaponNameText.text = _wc.TeamID == 0 ? "AK-47u" : "M4A1";
        }
    }
}
