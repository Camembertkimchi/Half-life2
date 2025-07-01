using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public interface IEnemyWeapon
{
    abstract void FireWeapon();
    Weapons Type { get; set; }
     
}
public interface IEnemyState
{
    void EnterState(EnemyAI enemy);
    void ExitState();
    void UpdateState();

}

public class EnemyAI : MonoBehaviour
{
    [SerializeField] GameObject weapon;//무기
    IEnemyWeapon currentWeapon;
    
    [SerializeField] int hp;
    NavMeshAgent agent;
    [SerializeField]static readonly WaitForSeconds reloadDelay = new WaitForSeconds(3.3f);
    [SerializeField] int maxAttackTime = 5;
    [SerializeField] int currentAttackTime;
    [SerializeField] Animator anim;
    Rigidbody rb;
    public bool NowReloading
    {
        get; set;
    }
    public bool NowHiding
    {
        get; set;
    }

    bool alive = true;
    IEnemyState state;

    #region 시야각
    [Range(0f,360f)] [SerializeField] float viewAngle; //보는 각도
    [SerializeField] float viewRadius; //보는 길이
    [SerializeField] LayerMask targetMask; //플레이어
    [SerializeField] LayerMask ObstacleMask; //장애물 레이어
    [SerializeField] bool debugingNow;
    [SerializeField] List<Collider> targetList = new List<Collider>();
    Quaternion targetRotation;
    [SerializeField]float rotationSpeed;
    [SerializeField] bool foundPlayer;
    [SerializeField] Vector3 lastPlayerTransform;
    #endregion

    IEnumerator currentCor;

    [SerializeField] int smgFireTimes = 2;
    [SerializeField] int arFireTimes = 4;
    [SerializeField] int pistolFireTimes = 5;
    [SerializeField] int shotgunFireTimes = 3;



    //이 두 프로퍼티는 GetComponemtFromParent<EnemyAI>()로 가져와서 써보셈 되면 ㄱㄱ
    public bool AliveState
    {
        get { return alive; } private set { alive = value; } 
    }
    public int AttackTime
    {
        get { return currentAttackTime; }
        set { currentAttackTime = value; }
    }

    private void OnEnable()
    {
        if(weapon != null)
        {
            // 이렇게하면 MethodInfo형태로 가져옴
            //var method = weapon.GetType().GetMethod("Fire");

            // 가져온 MethodInfo형태를 IEnumerator로 변환하겠다 즉, MonoBehaviour를 가지고 코루틴을 부르기 때문에 MonoBehaviour(this) 형태의 코루틴을 MethodInfo의 정보를 바탕으로 attackCor에 담겠다는 뜻
            // 즉, 지금 스크립트는 MonoBehaviour를 상속 받고 있어서 만약 MonoBehaviour가 없다면 (IEnumerator)Delegate.CreateDelegate(typeof(IEnumerator), weapon.GetComponent<MonoBehaviour>(), method); 이렇게 써야함
            //attackCor = (IEnumerator)Delegate.CreateDelegate(typeof(IEnumerator), this, method);

            //그냥 무기에서 함수 부르는 걸로 일단 해보자
            currentWeapon = weapon.GetComponent<IEnemyWeapon>();

            switch (currentWeapon.Type)
            {
                case Weapons.Pistol: maxAttackTime = pistolFireTimes; break;
                case Weapons.Shotgun: maxAttackTime = shotgunFireTimes; break;
                case Weapons.SMG: maxAttackTime = smgFireTimes; break;
                case Weapons.AR2: maxAttackTime = arFireTimes; break;
                default: Debug.Log("무기 없음");
                    maxAttackTime = 0; break;
            }

            currentAttackTime = maxAttackTime;
            //하프2는 총알 갯수에 따라 장전하지 않고 공격 횟수에 따라서 장전을 결정함

            
        }
        else
        {
            Debug.Log("무기 못읽음");
           
        }
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

    }

    private void Update()
    {
        if(alive == true)
        {
            Viewing();
            if (targetList.Count > 0)
            {
                LookAtPlayer(targetList[0].transform); // 감지된 플레이어 바라보기
            }
            else if (foundPlayer == true)
            {
                currentCor = Chase();
                StartCoroutine(currentCor);
            }
            if (currentAttackTime <= 0)
            {
                if (currentCor != null)
                {
                    StopCoroutine(currentCor);
                    currentCor = null;

                }
                currentCor = Reloading();
                StartCoroutine(currentCor);
                
            }

           if(lastPlayerTransform != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
            {
                anim.SetBool("Running", false);
            }
           
        }
       

        

    }


    // 숨고 -> 장전 -> 움직이기
    /// <summary>
    /// 재장전 할 시기에 숨고 장전하는 로직
    /// </summary>
    /// <returns></returns>
    /// 
    IEnumerator Reloading()
    {
        if(currentAttackTime != maxAttackTime)
        {
            StartCoroutine("Hiding");
            yield return new WaitUntil(() => NowHiding == false);
            anim.SetBool("Running", false);
            anim.SetBool("Reload", true);
            yield return reloadDelay;
            currentAttackTime = maxAttackTime;
            NowReloading = false; //장전 완료!
            anim.SetBool("Reload", false);
        }
        
    
    }
    /// <summary>
    /// Reloading으로 불러올 것. 절대 단독 사용하지 마세요.
    /// </summary>
    /// <returns></returns>
    IEnumerator Hiding()//Reloading으로 불러오세요!
    {
        //아무튼 숨는 로직
        //앞이 벽이다 = 숨었다! else 벽이 주변에 없다!
        Collider[] walls = Physics.OverlapSphere(transform.position, 4, ObstacleMask);
        //Transform lastPos = transform;

        if (walls.Length > 0)
        {
            Transform wall = walls[0].transform;

            Vector3 directionToPlayer = (lastPlayerTransform - wall.position).normalized;
            Vector3 hidePos = wall.position - directionToPlayer;

            agent.SetDestination(hidePos);
            anim.SetBool("Running", true);
            NowHiding = false;
        }
        else
        {
            NowHiding = false;
        }
        yield return new WaitUntil(() => NowReloading == false);
        currentCor = null;
        //움직여!
    }

    IEnumerator Chase()
    {

        anim.SetBool("Running", true);
        while (targetList.Count == 0 && transform.position != lastPlayerTransform)
        {
            agent.SetDestination(lastPlayerTransform);
            yield return null;
           

        }
        if (targetList.Count == 0)
        {
            foundPlayer = false;
            anim.SetBool("Running", false);
        }
    }



    void OnDrawGizmos()
    {
        if (debugingNow)
        {
            Vector3 pos = transform.position + Vector3.up * 0.5f;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(pos, viewRadius);
        }
    }



    void Viewing()
    {
        targetList.Clear();
        Collider[] targets = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        if (targets.Length == 0) return;

        float lookingAngle = transform.eulerAngles.y;  //정면
        Vector3 lookDir = AngleToDir(lookingAngle);

        //미리 계산 (각도 변환을 반복하지 않도록)
        float halfViewAngle = viewAngle * 0.5f;
        float cosHalfViewAngle = Mathf.Cos(halfViewAngle * Mathf.Deg2Rad);

        if (debugingNow)
        {
            Vector3 rightDir = AngleToDir(lookingAngle + halfViewAngle);
            Vector3 leftDir = AngleToDir(lookingAngle - halfViewAngle);

            Debug.DrawRay(transform.position, rightDir * viewRadius, Color.blue);
            Debug.DrawRay(transform.position, leftDir * viewRadius, Color.blue);
            Debug.DrawRay(transform.position, lookDir * viewRadius, Color.cyan);
        }


        

        foreach (Collider target in targets)
        {
            Vector3 targetDir = (target.transform.position - transform.position).normalized;

            //내적 값 비교
            if (Vector3.Dot(lookDir, targetDir) >= cosHalfViewAngle)
            {
                //Raycast로 적이 가려져 있는지 확인
                if (!Physics.Raycast(transform.position, targetDir, viewRadius, ObstacleMask))
                {
                    targetList.Add(target);
                    foundPlayer = true;
                    lastPlayerTransform = target.transform.position;
                    if (debugingNow) Debug.DrawLine(transform.position, target.transform.position, Color.red);
                    //공격
                    if (currentAttackTime > 0)
                    {
                        currentWeapon.FireWeapon();
                        anim.SetBool("Fire", true);
                    }

                    
                }
                
            }
           
        }
        


    }
    Vector3 AngleToDir(float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radian), 0f, Mathf.Cos(radian));
    }

    void LookAtPlayer(Transform player)
    {
        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0; // 고개 숙이는 걸 방지 (회전은 수평 방향만)

        targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        //weapon.transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }



 

    public void ChangeHp(int damage)
    {
        
        hp -= damage;
        if(hp <= 0)
        {
            alive = false;
            Die();
        }
    }

    void Die()
    {
            StopAllCoroutines();
            StartCoroutine(Dead());
    }

    IEnumerator Dead()
    {

        anim.SetTrigger("Dead");


        yield return new WaitForSeconds(10f);
        gameObject.SetActive(false);
    }





}
