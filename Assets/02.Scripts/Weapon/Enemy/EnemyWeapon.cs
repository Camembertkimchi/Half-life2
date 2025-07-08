using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour
{
    public ScriptableWeapon weaponInfo;
    EnemyAI enemyAI;
    [SerializeField] Transform muzzlePos;
    [SerializeField] int fireTimes;//현재 주기
    static readonly WaitForSeconds weaponDelay = new WaitForSeconds(1f);
    static readonly WaitForSeconds fireDelay = new WaitForSeconds(0.3f);//발견하고 총을 쏘는 시간
    static readonly WaitForSeconds reloadDelay = new WaitForSeconds(3.3f);
    [SerializeField] int maxFireTimes;//한 주기 당 발사하는 총알 갯수
    public Weapons type;
    [SerializeField] BulletPooling pool;
    float accuracy;
    Vector3 randomSpread;
    IEnumerator currentCor;

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
    public bool NeedToReload()
    {
        return enemyAI.AttackTime <= 0 && !enemyAI.NowReloading;// 총알이 없고, 현재 재장전 중이 아닐 때
    }

    /// <summary>
    /// 재장전 코루틴 EnemyAI에서 호출
    /// </summary>
    public IEnumerator ReloadWeapon()
    {
        if (enemyAI.NowReloading) yield break; // 이미 재장전 중이면 중복 방지
        enemyAI.NowReloading = true;
        Debug.Log($"무기 장전 중...");
        enemyAI.anim.SetBool("Running", false);
        enemyAI.anim.SetBool("Reload", true);
        yield return reloadDelay;
        enemyAI.AttackTime = enemyAI.MaxAttackTime; // 총알 채우기
        enemyAI.NowReloading = false;
        Debug.Log($"재장전 완");
    }
    private void SpawnBullet(Vector3 spread)
    {
        weaponInfo.bullet = pool.GetBullet();
        weaponInfo.bullet.transform.position = muzzlePos.position;
        weaponInfo.bullet.transform.rotation = Quaternion.LookRotation(muzzlePos.forward + spread);
        weaponInfo.bulletScript = weaponInfo.bullet.GetComponent<BulletCon>();
        if (weaponInfo.bulletScript.Damage != weaponInfo.damage)
        {
            weaponInfo.bulletScript.Damage = weaponInfo.damage;
        }
        weaponInfo.bulletScript.Initialize(pool, false);
    }
    public void FireWeapon()
    {
        if (currentCor == null && enemyAI.AliveState)
        {
            currentCor = Fire();
            StartCoroutine(currentCor);
        }
    }

    public IEnumerator Fire()
    {
        yield return fireDelay;
        Debug.Log("발사 부름");
        while (enemyAI != null && enemyAI.AliveState == true && enemyAI.AttackTime > 0 && enemyAI.foundPlayer)
        {
            while(fireTimes > 0)
            {
                enemyAI.anim.SetBool("Fire", true);
                randomSpread = Vector3.zero;
                if (type == Weapons.Shotgun)
                {
                    for(int i = 0; i < 12;  i++)
                    {
                        randomSpread.x = Random.Range(-accuracy * 0.5f, accuracy * 0.5f);
                        randomSpread.y = Random.Range((-accuracy + 0.5f) * 0.2f, (accuracy - 0.5f) * 0.2f);
                        SpawnBullet(randomSpread);
                    }
                }
                else
                {
                    randomSpread.x = Random.Range(-accuracy * 0.5f, accuracy * 0.5f);
                    randomSpread.y = Random.Range((-accuracy + 0.5f) * 0.2f, (accuracy - 0.5f) * 0.2f);
                    SpawnBullet(randomSpread);

                }
                
                fireTimes--;
                yield return new WaitForSeconds(weaponInfo.fireRate);

            }

            if (enemyAI.foundPlayer && fireTimes > 0) yield return weaponDelay;
            else break;
        }
        enemyAI.anim.SetBool("Fire", false);
        currentCor = null;
        yield break;
    }
}
