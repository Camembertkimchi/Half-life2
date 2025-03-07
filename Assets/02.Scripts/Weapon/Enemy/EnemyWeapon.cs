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
    static readonly WaitForSeconds fireDelay = new WaitForSeconds(0.3f);//발견하고 총을 쏘는 시간
    [SerializeField] int maxFireTimes;//한 주기 당 발사하는 총알 갯수
    Weapons type;
    [SerializeField] BulletPooling pool;
    float accuracy;
    Vector3 randomSpread;
    IEnumerator currentCor;



    Weapons IEnemyWeapon.Type 
    { 
        get { return type; } 
        set { type = value; } 

        //기능 구현 => 에셋 => 
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
        
        
        accuracy = weaponInfo.accuracy;

    }

    public void FireWeapon()
    {
        if (currentCor == null)
        {
            currentCor = Fire();
            StartCoroutine(currentCor);
        }
    }

    public IEnumerator Fire()
    {
        yield return fireDelay;
        while (enemyAI != null && enemyAI.AliveState == true && enemyAI.AttackTime > 0)
        {

            while(fireTimes > 0)
            {
                
                if (type == Weapons.Shotgun)
                {

                    
                    for(int i = 0; i < 12;  i++)
                    {
                        randomSpread.x = Random.Range(-accuracy * 0.5f, accuracy * 0.5f);
                        randomSpread.y = Random.Range((-accuracy + 0.5f) * 0.2f, (accuracy - 0.5f) * 0.2f);

                        

                        weaponInfo.bullet = pool.GetBullet();
                        weaponInfo.bullet.transform.position = muzzlePos.position;
                        weaponInfo.bullet.transform.rotation = Quaternion.LookRotation(muzzlePos.forward + randomSpread);
                        weaponInfo.bulletScript = weaponInfo.bullet.GetComponent<BulletCon>();
                        if (weaponInfo.bulletScript.Damage != weaponInfo.damage)
                        {
                            weaponInfo.bulletScript.Damage = weaponInfo.damage;
                        }
                        weaponInfo.bulletScript.Initialize(pool, false);
                    }
                }
                else
                {
                    randomSpread.x = Random.Range(-accuracy * 0.5f, accuracy * 0.5f);
                    randomSpread.y = Random.Range((-accuracy + 0.5f) * 0.2f, (accuracy - 0.5f) * 0.2f);


                    weaponInfo.bullet = pool.GetBullet();
                    weaponInfo.bullet.transform.position = muzzlePos.position;
                    weaponInfo.bullet.transform.rotation = Quaternion.LookRotation(muzzlePos.forward + randomSpread);
                    weaponInfo.bulletScript = weaponInfo.bullet.GetComponent<BulletCon>();
                    if (weaponInfo.bulletScript.Damage != weaponInfo.damage)
                    {
                        weaponInfo.bulletScript.Damage = weaponInfo.damage;
                    }
                    weaponInfo.bulletScript.Initialize(pool, false);

                }
                
                fireTimes--;
                yield return new WaitForSeconds(weaponInfo.fireRate);

            }

            fireTimes = maxFireTimes;
            enemyAI.AttackTime--;
            yield return weaponDelay;

        }
        currentCor = null;
        yield break;
    }
}
