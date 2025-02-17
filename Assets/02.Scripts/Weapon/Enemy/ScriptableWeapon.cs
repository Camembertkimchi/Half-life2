using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWeaponObject", menuName = "EnemyWeapon/New Weapon")]
public abstract class ScriptableWeapon : ScriptableObject
{
    public enum Weapons 
    {   
        Pistol = 0,
        SMG,
        AR2,
        Shotgun 
    }


    public string WeaponName;
    public int damage;
    public float fireRate;
    public int maxAmmo;


    public GameObject bullet;
}



