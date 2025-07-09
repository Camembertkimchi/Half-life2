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
    Coroutine currentCor;
    //bool reload = false;
    public bool ShouldStopFiring { get; private set; } = false;
    private void OnEnable()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
        weaponInfo = Instantiate(weaponInfo); // 복사본 안쓰면 골 때리게도 총알을 못가져옴,,,
        //생성을 안할 경우 같은 ScriptableObject를 공유해서 모든 무기가 동일하다고 함
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
        ShouldStopFiring = false; // 이미 재장전 중이면 중복 방지
        //reload = true;
        Debug.Log($"무기 장전 중...");
        enemyAI.anim.SetBool("Reload", true);
        yield return reloadDelay;
        enemyAI.AttackTime = enemyAI.MaxAttackTime; // 총알 채우기
        enemyAI.anim.SetBool("Reload", false);
        //reload = false;
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
    public void Fire()
    {
        // 이미 발사 코루틴이 실행 중이라면, 중복 실행 방지
        if (currentCor == null)
        {
            ShouldStopFiring = false;
            currentCor = StartCoroutine(FireLoop());
        }
        //yield return currentCor; // FireLoop 코루틴이 완전히 끝날 때까지 대기함. 밑에 문제가 발견!
        //이 구조로는 종료될 때까지 기다리기는 하는데, 탄창을 다 비우거나 플레이어가 사라질 때까지 계속 돌아감
        //그러면 다른 함수의 호출이나 플레이어 감지 검사, 거리 검사가 실행될 기회가 없으삼
        //그래서 멀쩡히 작동을 안하게 되고, 바보가 되버리는 것.
    }
    public void StopFiring()
    {
        if (currentCor != null)
        {
            StopCoroutine(currentCor);
            currentCor = null;
            ShouldStopFiring = false;
            enemyAI.anim.SetBool("Fire", false);
            Debug.Log("발사 중지");
        }
    }

    private IEnumerator FireLoop()
    {
        yield return fireDelay;
        Debug.Log("발사 부름");
        while (enemyAI != null && enemyAI.AliveState == true) //&& enemyAI.AttackTime > 0 && enemyAI.foundPlayer)
            //이 뒷 조건문은 지나치게 까탈시러운 것과 밑에서 이미 검사하는 내용이 있어서 삭제
        {
            if (NeedToReload())
            {
                Debug.Log("재장전 필요");
                ShouldStopFiring = true;
                break; // yield break를 사용하면 코루틴이 즉시 종료 되버림
                //즉, While문 바깥 코드들이 실행되지 않음
            }
            if (!enemyAI.foundPlayer)
            {
                Debug.Log("플레이어를 놓침");
                ShouldStopFiring = true;
                break;
            }


            if (fireTimes <= 0)
            {
                // 한 주기가 끝났으므로 다음 주기를 위해 AttackTime 감소 및 fireTimes 초기화
                enemyAI.AttackTime--;
                if (enemyAI.AttackTime <= 0)
                {
                    Debug.Log("재장전 필요");
                    ShouldStopFiring = true;
                    break;
                }
                fireTimes = maxFireTimes; // 다음 주기를 위해 발사 횟수 초기화
                yield return weaponDelay; // 한 주기 발사 후 딜레이
            }

            //실제 발사 로직
            enemyAI.anim.SetBool("Fire", true); // 발사 애니메이션 켜기
            randomSpread = Vector3.zero; // 매 발사마다 스프레드 초기화
            if (type == Weapons.Shotgun)
            {
                for (int i = 0; i < 12; i++)
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

            fireTimes--; // 현재 주기의 발사 횟수 감소
            yield return new WaitForSeconds(weaponInfo.fireRate); // 다음 발사까지 딜레이
        }

        // 루프 종료 시 (alive = false 또는 other conditions)
        enemyAI.anim.SetBool("Fire", false);
        currentCor = null;
    }
}
