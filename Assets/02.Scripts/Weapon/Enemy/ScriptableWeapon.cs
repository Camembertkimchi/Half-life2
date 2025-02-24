using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWeaponObject", menuName = "EnemyWeapon/New Weapon")]


public class ScriptableWeapon : ScriptableObject
{
    public int damage;
    public float fireRate;
    [Tooltip("한 주기에 발사할 총알")] public int oneCyleFireAmmo;//공격 주기당 발사할 총알
    public Weapons weapon;
    [Tooltip("낮을 수록 좋고 높을 수록 구림")] public float accuracy;
    public GameObject bullet;
    public BulletCon bulletScript;

    public void SetBullet(GameObject prefab)
    {
        bullet = prefab;
    }  
  
}
public enum Weapons
{
    Pistol = 0,
    SMG,
    AR2,
    Shotgun
}



