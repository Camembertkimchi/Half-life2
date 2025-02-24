using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EnemyWeaponObject", menuName = "EnemyWeapon/New Weapon")]


public class ScriptableWeapon : ScriptableObject
{
    public int damage;
    public float fireRate;
    [Tooltip("�� �ֱ⿡ �߻��� �Ѿ�")] public int oneCyleFireAmmo;//���� �ֱ�� �߻��� �Ѿ�
    public Weapons weapon;
    [Tooltip("���� ���� ���� ���� ���� ����")] public float accuracy;
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



