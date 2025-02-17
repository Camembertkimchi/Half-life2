using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerWeaponObject", menuName = "PlayerWeapon/New Weapon")]

public class PlayerScriptableWeapon : ScriptableObject
{
    public enum WeaponType { Melee = 0, SemiAuto, FullAuto }
    public enum WeaponLoadOut { Defalut = 0, Pistol, FullAuto, SGSR, Explosive }
    public enum AmmoType { Pistol = 0, Magnum, AR, SG, Sniper, Granade, RPG }

    public string WeaponName;
    public int damage;
    public float fireRate;
    public int maxAmmo;
    public WeaponLoadOut weaponLoadOut;
    public WeaponType weaponType;
    public AmmoType ammoType;


}
