using System.Collections;
using System.Collections.Generic;
using System.Net;
using Unity.VisualScripting;
using UnityEngine;


public interface IDamageable
{
    public void ChangeHp(int value);
}
public class BulletCon : MonoBehaviour
{
    [SerializeField] float bulletSpeed = 20f;
    [SerializeField] int damage;
    BulletPooling bulletPool;
    [SerializeField] bool isReleased = false;
    static readonly WaitForSeconds bulletReleaseTime = new WaitForSeconds(2f);
    IEnumerator currentCor;
    [SerializeField] bool shootedByPlayer;
    //[SerializeField] GameObject fx;
    [SerializeField] float fxLifeTime;
    //Collider col;
    Rigidbody rb;
    [SerializeField] ParticlePool particlePool;
    [SerializeField] TrailRenderer trailRenderer;
    public int Damage
    {
        get { return damage; }
        set { damage = value; }
    }

    public void ReflectDamage(int bulletDamage)
    {
        damage = bulletDamage;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 터널링 방지
                                                                       // 터널링은 속도가 빠른 오브젝트가 콜라이더를 뚫고 가버리는 것
        if (particlePool == null) particlePool = GameObject.Find("ParticlePool").GetComponent<ParticlePool>();
        if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
    }


    private void FixedUpdate()
    {
        Vector3 move = transform.forward * bulletSpeed * Time.fixedDeltaTime;
        rb.MovePosition(rb.position + move);

    }



    public void Initialize(BulletPooling pool, bool x)
    {
        // col = GetComponent<Collider>();
        shootedByPlayer = x;
        //if (!shootedByPlayer) bulletSpeed = 15f;
        //else bulletSpeed = 20f;
        bulletPool = pool;
        if (!isReleased)
        {
            Debug.Log("응 처음부터 true임~");
            return;
        }
        isReleased = true;

        if (currentCor == null)
        {
            currentCor = DelayedRelease();
        }
        StartCoroutine(currentCor);
    }

    private IEnumerator DelayedRelease()
    {
        yield return bulletReleaseTime;
        Release();
    }

    void Release()
    {
        bulletPool.ReleaseBullet(gameObject);
    }

    private void OnEnable()
    {
        if (!isReleased) isReleased = true;
        if (!trailRenderer.enabled) trailRenderer.enabled = true;
    }


    void OnDisable()
    {
        if (isReleased == true)
        {
            trailRenderer.Clear();
            isReleased = false;
            if (currentCor != null)
            {
                StopCoroutine(currentCor);
            }
            currentCor = null;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // 총알끼리 충돌하는 경우 무시
        if (other.gameObject.CompareTag("Bullet"))
        {
            return;
        }
        Vector3 hitPoint = transform.position;
        // 총알의 진행 방향 반대를 충돌 법선으로 사용 (총알이 박히는 것처럼 연출)
        Vector3 hitNormal = -transform.forward;

        // 충돌한 대상이 데미지를 입을 수 있는 대상인지 확인
        IDamageable target = other.gameObject.GetComponent<IDamageable>();

        if (target != null) // 데미지를 입을 수 있는 대상이라면
        {
            // 이 총알이 누구의 총알인지에 따라 데미지 대상을 구분
            if (shootedByPlayer) // 플레이어가 쏜 총알
            {
                if (other.gameObject.CompareTag("Enemy"))
                {
                    // 플레이어 총알이 적군을 맞춤
                    target.ChangeHp(damage);
                    ShowImpactEffect(hitPoint, hitNormal);
                    Release();
                    return;
                }
                else if (other.gameObject.CompareTag("Player"))
                {
                    return;
                }
            }
            else // 적군이 쏜 총알
            {
                if (other.gameObject.CompareTag("Player"))
                {
                    // 적군 총알이 플레이어를 맞춤
                    target.ChangeHp(damage);
                    ShowImpactEffect(hitPoint, hitNormal);
                    Release();
                    return;
                }
                else if (other.gameObject.CompareTag("Enemy"))
                {
                    // 적군 총알이 적군을 맞춤 (팀킬 방지)
                    ShowImpactEffect(hitPoint, hitNormal); // 파티클 효과는 보여줌
                    Release();
                    return;
                }
            }
        }

        // 다른 오브젝트와 충돌 처리
        ShowImpactEffect(hitPoint, hitNormal);
        Release(); // 총알은 충돌 후 사라져야 함
    }

    // 충돌 지점에서 파티클 효과를 보여주는 메서드
    private void ShowImpactEffect(Vector3 position, Vector3 normal)
    {
        if (particlePool == null)
        {
            Debug.LogWarning("ParticlePool이 할당되지 않음");
            return;
        }

        GameObject particleObj = particlePool.GetParticle();
        if (particleObj == null)
        {
            Debug.LogWarning("파티클 오브젝트가 없는데요");
            return;
        }

        particleObj.transform.position = position;
        particleObj.transform.rotation = Quaternion.LookRotation(normal); // 파티클이 벽에 박히는 방향으로 회전

        ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            ps.Play();
        }
        else
        {
            Debug.LogWarning("ParticleSystem 어디있나요?");
        }
    }

    // 왜인지 몰라도 TriggerEnter에선 잘 동작하지만 여기선 비정상
    // 증상: 플레이어가 쏜 총알은 작동하지 않는 수준
    // 하지만 동일한 걸 쓰는 적군은 정확히 동작함.
    //private void OnCollisionEnter(Collision collision)
    //{
    //    // 총알끼리 충돌하는 경우를 먼저 처리
    //    if (collision.gameObject.CompareTag("Bullet"))
    //    {
    //        return;
    //    }
    //
    //    // 1. 충돌 지점과 법선 벡터 가져오기
    //    Vector3 hitPoint = collision.contacts[0].point;
    //    Vector3 hitNormal = collision.contacts[0].normal;
    //
    //    // 2. 피격 대상 처리
    //    // 플레이어가 쏜 총알인 경우 (적 타격)
    //    if (shootedByPlayer)
    //    {
    //        if (collision.gameObject.CompareTag("Enemy"))
    //        {
    //            EnemyAI enemy = collision.gameObject.GetComponent<EnemyAI>();
    //            if (enemy != null)
    //            {
    //                enemy.ChangeHp(damage);
    //            }
    //            ShowImpactEffect(hitPoint, hitNormal);
    //            Release();
    //            return; // 처리 완료 후 함수 종료
    //        }
    //    }
    //    // 적이 쏜 총알인 경우
    //    else // !shootedByPlayer
    //    {
    //        if (collision.gameObject.CompareTag("Player"))
    //        {
    //            PlayerMovement player = collision.gameObject.GetComponent<PlayerMovement>();
    //            if (player != null)
    //            {
    //                player.ChangeHp(damage);
    //            }
    //            ShowImpactEffect(hitPoint, hitNormal);
    //            Release();
    //            return; // 처리 완료 후 함수 종료
    //        }
    //    }
    //
    //    // 3. 그 외의 모든 충돌 (벽, 환경 오브젝트 등)
    //    // 위에서 적이나 플레이어를 맞춘 경우 이미 return 되었으므로,
    //    // 이 부분은 총알이 다른 물리 오브젝트(벽 등)에 부딪혔을 때 처리됩니다.
    //    ShowImpactEffect(hitPoint, hitNormal);
    //    Release(); // 총알 풀로 반환
    //}
    //
    //// 충돌 지점에서 파티클 효과를 보여주는 메서드
    //private void ShowImpactEffect(Vector3 position, Vector3 normal)
    //{
    //    if (particlePool == null)
    //    {
    //        Debug.LogWarning("ParticlePool이 할당되지 않았습니다. 파티클을 재생할 수 없습니다.");
    //        return;
    //    }
    //
    //    GameObject particleObj = particlePool.GetParticle();
    //    if (particleObj == null)
    //    {
    //        Debug.LogWarning("오브젝트 풀에서 파티클 오브젝트를 가져오지 못했습니다.");
    //        return;
    //    }
    //
    //    particleObj.transform.position = position;
    //    particleObj.transform.rotation = Quaternion.LookRotation(normal);
    //
    //    ParticleSystem ps = particleObj.GetComponent<ParticleSystem>();
    //    if (ps != null)
    //    {
    //        ps.Play();
    //    }
    //    else
    //    {
    //        Debug.LogWarning("파티클 오브젝트에 ParticleSystem 컴포넌트가 없습니다.");
    //    }
    //}

}



