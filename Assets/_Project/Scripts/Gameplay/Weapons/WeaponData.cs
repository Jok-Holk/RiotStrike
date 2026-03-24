using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "RiotStrike/WeaponData")]
public class WeaponData : ScriptableObject
{
    public string weaponName;
    public int damage;
    public float fireRate;            // phát/giây
    public int magazineSize;
    public int reserveAmmo;
    public float reloadTime;
    public float range;               // tầm raycast tối đa
    public int headshotMultiplier;    // hệ số nhân khi headshot
    public WeaponType weaponType;     // Rifle hoặc Pistol
}

public enum WeaponType
{
    Pistol,
    Rifle
}
