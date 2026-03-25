using UnityEngine;
using Fusion;
using RiotStrike.Data;

public class WeaponController : NetworkBehaviour
{
    [Header("Weapon Data")]
    [SerializeField] private WeaponData ak47Data;
    [SerializeField] private WeaponData m4a1Data;
    [SerializeField] private WeaponData pistolData;

    [Header("References")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject muzzleFlashVFX;
    [SerializeField] private Camera fpsCamera;

    [Header("Models")]
    [SerializeField] private GameObject ak47Model;
    [SerializeField] private GameObject m4a1Model;
    [SerializeField] private GameObject pistolModel;

    [Networked] public int CurrentAmmo { get; set; }
    [Networked] public int ReserveAmmo { get; set; }
    [Networked] public bool IsRifleUnlocked { get; set; }

    private WeaponData _currentWeapon;
    private bool _isReloading;
    private float _nextFireTime;
    private int _teamID; // 0 = AK47, 1 = M4A1

    public override void Spawned()
    {
        if (!Object.HasInputAuthority) return;

        var networkPlayer = GetComponent<NetworkPlayer>();
        _teamID = networkPlayer != null ? networkPlayer.Team : 0;

        EquipPistol();
    }

    public void EquipPistol()
    {
        _currentWeapon = pistolData;
        CurrentAmmo = pistolData.magazineSize;
        ReserveAmmo = pistolData.reserveAmmo;
        ShowModel(pistolModel);
    }

    public void EquipRifle()
    {
        _currentWeapon = _teamID == 0 ? ak47Data : m4a1Data;
        CurrentAmmo = _currentWeapon.magazineSize;
        ReserveAmmo = _currentWeapon.reserveAmmo;
        ShowModel(_teamID == 0 ? ak47Model : m4a1Model);
    }

    void ShowModel(GameObject active)
    {
        if (ak47Model) ak47Model.SetActive(false);
        if (m4a1Model) m4a1Model.SetActive(false);
        if (pistolModel) pistolModel.SetActive(false);
        if (active) active.SetActive(true);
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasInputAuthority) return;
        if (!GetInput(out NetworkInputData input)) return;

        if (input.Fire && CanFire())
            Fire();

        if (input.Reload && !_isReloading && CurrentAmmo < _currentWeapon.magazineSize)
            StartCoroutine(Reload());
    }

    bool CanFire()
    {
        return !_isReloading
            && CurrentAmmo > 0
            && Runner.SimulationTime >= _nextFireTime;
    }

    void Fire()
    {
        _nextFireTime = Runner.SimulationTime + 1f / _currentWeapon.fireRate;
        CurrentAmmo--;

        if (muzzleFlashVFX != null)
        {
            muzzleFlashVFX.SetActive(true);
            Invoke(nameof(HideMuzzleFlash), 0.05f);
        }

        if (Runner.IsForward)
        {
            var ray = fpsCamera.ScreenPointToRay(
                new Vector3(Screen.width / 2f, Screen.height / 2f));

            if (Physics.Raycast(ray, out RaycastHit hit, _currentWeapon.range))
            {
                Debug.DrawLine(ray.origin, hit.point, Color.red, 1f);

                var health = hit.collider.GetComponentInParent<PlayerHealth>();
                if (health != null)
                {
                    bool isHeadshot = hit.collider.CompareTag("Head");
                    int dmg = isHeadshot
                        ? _currentWeapon.damage * _currentWeapon.headshotMultiplier
                        : _currentWeapon.damage;
                    RPC_ApplyDamage(health.Object, dmg);
                }
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    void RPC_ApplyDamage(NetworkObject target, int damage)
    {
        var health = target.GetComponent<PlayerHealth>();
        health?.TakeDamage(damage);
    }

    System.Collections.IEnumerator Reload()
    {
        _isReloading = true;
        yield return new WaitForSeconds(_currentWeapon.reloadTime);
        int needed = _currentWeapon.magazineSize - CurrentAmmo;
        int take = Mathf.Min(needed, ReserveAmmo);
        CurrentAmmo += take;
        ReserveAmmo -= take;
        _isReloading = false;
    }

    void HideMuzzleFlash() => muzzleFlashVFX?.SetActive(false);

    public void OnRifleUnlocked()
    {
        IsRifleUnlocked = true;
        EquipRifle();
    }
}
