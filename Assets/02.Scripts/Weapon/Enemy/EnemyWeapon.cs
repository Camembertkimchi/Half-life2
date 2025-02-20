using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour, IEnemyWeapon
{
    [SerializeField] ScriptableWeapon weaponInfo;
    EnemyAI enemyAI;
    [SerializeField] Transform muzzlePos;
    [SerializeField] int fireTimes;//현재 주기
    static readonly WaitForSeconds weaponDelay = new WaitForSeconds(1f);
    [SerializeField] int maxFireTimes;//한 주기 당 발사하는 총알 갯수
    Weapons type;
    [SerializeField] BulletPooling pool;


    Weapons IEnemyWeapon.Type 
    { 
        get { return type; } 
        set { type = value; } 
    }

    private void OnEnable()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
        weaponInfo = Instantiate(weaponInfo); // 복사본 안쓰면 골 때리게도 총알을 못가져옴,,,
        weaponInfo.SetBullet(pool.bulletPrefab);
        weaponInfo.bulletScript = weaponInfo.bullet.GetComponent<BulletCon>();

        weaponInfo.bulletScript.Damage = weaponInfo.damage;
        maxFireTimes = weaponInfo.oneCyleFireAmmo;
        fireTimes = maxFireTimes;
        type = weaponInfo.weapon;
        
        
    }

    public void FireWeapon()
    {
        StartCoroutine(Fire());

    }

    public IEnumerator Fire()
    {

        while(enemyAI != null && enemyAI.AliveState == true && enemyAI.AttackTime > 0)
        {

            while(fireTimes > 0)
            {
                if(type == Weapons.Shotgun)
                {
                    for(int i = 0; i < 12;  i++)
                    {
                        weaponInfo.bullet = pool.GetBullet();
                        weaponInfo.bullet.transform.position = muzzlePos.position;
                        weaponInfo.bullet.transform.rotation = muzzlePos.rotation;
                        weaponInfo.bulletScript = weaponInfo.bullet.GetComponent<BulletCon>();
                        weaponInfo.bulletScript.Initialize(pool);
                    }
                }
                else
                {
                    weaponInfo.bullet = pool.GetBullet();
                    weaponInfo.bullet.transform.position = muzzlePos.position;
                    weaponInfo.bullet.transform.rotation = muzzlePos.rotation;
                    weaponInfo.bulletScript.Initialize(pool);

                }
                
                fireTimes--;
                yield return new WaitForSeconds(weaponInfo.fireRate);

            }

            fireTimes = maxFireTimes;
            enemyAI.AttackTime--;
            yield return weaponDelay;

        }
        yield break;
    }
}
