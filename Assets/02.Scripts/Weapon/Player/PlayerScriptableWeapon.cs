using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "PlayerWeaponObject", menuName = "PlayerWeapon/New Weapon")]

public class PlayerScriptableWeapon : ScriptableObject
{
  
    public void SetBullet(GameObject prefab)
    {
        bullet = prefab;
    }
    public BulletCon bulletScript;
    public GameObject bullet;
    public string WeaponName;
    public int damage;
    public float fireRate;
    public int maxAmmo;
    public int maxMag;
    public PlayerWeaponState state;
    public WeaponLoadOut weaponLoadOut;
    public WeaponType weaponType;
    public AmmoType ammoType;
    [Header("��Ȯ��. Min���� ���� ��Ȯ�� ��, Max���� ���� ���� ��")]
    public float acurracyMin;
    public float acurracyMax;
    public float spreadIncreaseRate = 0.05f; // �ʴ� ��Ȯ�� ���� �ӵ�
    public float spreadRecoveryRate = 0.1f; // �ʴ� ��Ȯ�� ȸ�� �ӵ�

}
public enum WeaponType { Melee = 0, SemiAuto, FullAuto }
public enum WeaponLoadOut { Defalut = 0, Pistol, FullAuto, SGSR, Explosive }
public enum AmmoType { None = 0, Pistol, Magnum, AR, SG, Sniper, Granade, RPG }
