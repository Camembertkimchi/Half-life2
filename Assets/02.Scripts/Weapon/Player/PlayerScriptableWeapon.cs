using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerWeaponObject", menuName = "PlayerWeapon/New Weapon")]

public class PlayerScriptableWeapon : ScriptableObject
{
    public enum WeaponType { Melee = 0, SemiAuto, FullAuto }
    public enum WeaponLoadOut { Defalut = 0, Pistol, FullAuto, SGSR, Explosive }
    public enum AmmoType { None = 0, Pistol, Magnum, AR, SG, Sniper, Granade, RPG }
    public void SetBullet(GameObject prefab)
    {
        bullet = prefab;
    }
    public BulletCon bulletCon;
    public GameObject bullet;
    public string WeaponName;
    public int damage;
    public float fireRate;
    public int currentAmmo;
    public int maxAmmo; 
    public WeaponLoadOut weaponLoadOut;
    public WeaponType weaponType;
    public AmmoType ammoType;
    [Header("정확도. Min에는 가장 정확한 값, Max에는 가장 퍼진 값")]
    public float acurracyMin;
    public float acurracyMax;

}
