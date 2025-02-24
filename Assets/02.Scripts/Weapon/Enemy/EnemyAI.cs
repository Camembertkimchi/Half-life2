using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Rendering.LookDev;
using UnityEngine;
using UnityEngine.AI;
using static ScriptableWeapon;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;

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
    [SerializeField]static readonly WaitForSeconds reloadDelay = new WaitForSeconds(2f);
    [SerializeField] int maxAttackTime = 5;
    [SerializeField] int currentAttackTime;
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

    #endregion



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
    }

    private void Update()//For Test
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            currentWeapon.FireWeapon();
            Debug.Log("발사해봄");
        }

        state?.UpdateState();
        Viewing();
        if (targetList.Count > 0)
        {
            LookAtPlayer(targetList[0].transform); // 감지된 플레이어 바라보기
        }

    }








    void ChangeState(IEnemyState status)
    {
        state?.ExitState();
        state = status;
        state.UpdateState();
    }


    // 숨고 -> 장전 -> 움직이기
    /// <summary>
    /// 재장전 할 시기에 숨고 장전하는 로직
    /// </summary>
    /// <returns></returns>
    /// 
    IEnumerator Reloading()
    {
        StartCoroutine("Hiding");
        yield return new WaitUntil(() => NowHiding == false);
        //애니메이션 재생
        yield return reloadDelay;
        currentAttackTime = maxAttackTime;
        NowReloading = false; //장전 완료!
    }





    /// <summary>
    /// Reloading으로 불러올 것. 절대 단독 사용하지 마세요.
    /// </summary>
    /// <returns></returns>
    IEnumerator Hiding()//Reloading으로 불러오세요!
    {
        //아무튼 숨는 로직
        //Player가 시야에 없다 + 앞이 벽이다 = 숨었다! else 벽이 주변에 없다!
        NowHiding = false;

        
        yield return new WaitUntil(() => NowReloading == false);
        //움직여!
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
      

        //OverlapSphere 결과가 없으면 바로 반환
        Collider[] targets = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        if (targets.Length == 0) return;

        float lookingAngle = transform.eulerAngles.y;  //정면
        Vector3 lookDir = AngleToDir(lookingAngle);

        //미리 계산 (각도 변환을 반복하지 않도록)
        float halfViewAngle = viewAngle * 0.5f;
        float cosHalfViewAngle = Mathf.Cos(halfViewAngle * Mathf.Deg2Rad);

        if (debugingNow)
        {
            Debug.Log("시야각 디버깅 확인");
            Vector3 rightDir = AngleToDir(lookingAngle + halfViewAngle);
            Vector3 leftDir = AngleToDir(lookingAngle - halfViewAngle);

            Debug.DrawRay(transform.position, rightDir * viewRadius, Color.blue);
            Debug.DrawRay(transform.position, leftDir * viewRadius, Color.blue);
            Debug.DrawRay(transform.position, lookDir * viewRadius, Color.cyan);
        }

        targetList.Clear();

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
                    Debug.Log("타겟 추가 확인");
                    if (debugingNow) Debug.DrawLine(transform.position, target.transform.position, Color.red);
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

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
    }



 

    public void ChangeHp(int damage)
    {
        
        hp += damage;
        if(hp <= 0)
        {
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

        //죽는 애니메이션
        //무기 드랍 Instantiate로 해보삼
        currentWeapon = null;//이걸로 한번 비워주시고
        
        yield return new WaitForSeconds(10f);
        //Destroy할지 뭐 Disable 할지 알잘딱 해주셈 ㅇㅇ
    }


}
