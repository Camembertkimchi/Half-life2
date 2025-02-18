using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWeaponObject", menuName = "EnemyWeapon/New Weapon")]


public class ScriptableWeapon : ScriptableObject
{
    public int damage;
    public float fireRate;
    public int oneCyleFireAmmo;//공격 주기당 발사할 총알
    public Weapons weapon;

    public GameObject bullet;
    public BulletCon bulletScript;

    
  
}
public enum Weapons
{
    Pistol = 0,
    SMG,
    AR2,
    Shotgun
}



