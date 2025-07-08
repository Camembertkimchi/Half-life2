using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices.WindowsRuntime;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
public enum AIState
{
    Patrol = 0, //순찰
    Chase, //추격
    Cover, //엄폐
    Attack, //발포
    Search, //탐색(플레이어를 놓칠 경우)
    Reload, //장전
    Defalut //초기값
}

public class EnemyAI : MonoBehaviour
{
    public AIState currentState = AIState.Patrol;
    [SerializeField] GameObject weapon;//무기
    [NonSerialized] EnemyWeapon currentWeapon;
    [SerializeField] int hp;
    NavMeshAgent agent;
    [NonSerialized] public Animator anim;
    Rigidbody rb;
    public bool NowReloading
    {
        get; set;
    }
    public bool NowHiding
    {
        get; set;
    }
    [SerializeField] bool alive = true;
    #region 시야각
    [Header("시야각")]
    [Range(0f,360f)] [SerializeField] float viewAngle; //보는 각도
    [SerializeField] float viewRadius; //보는 길이
    [SerializeField] LayerMask targetMask; //플레이어
    [SerializeField] LayerMask ObstacleMask; //장애물 레이어
    [SerializeField] bool debugingNow;
    [SerializeField] List<Collider> targetList = new List<Collider>();
    Quaternion targetRotation;
    [SerializeField]float rotationSpeed;
    public bool foundPlayer;
    [SerializeField] Vector3 lastPlayerTransform;
    [SerializeField] bool playerInSightThisFrame = false;
    #endregion
    [Header("공격")]
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private int currentAttackTime;
    [SerializeField] int maxAttackTime = 5;
    [SerializeField] bool reloading;
    [Header("총기 발사 횟수")]
    [SerializeField] int smgFireTimes = 2;
    [SerializeField] int arFireTimes = 4;
    [SerializeField] int pistolFireTimes = 5;
    [SerializeField] int shotgunFireTimes = 3;
    //순찰
    [Header("순찰")]
    [SerializeField] private Transform[] patrolPoints;
    private int currentPatrolIndex = 0;
    [SerializeField] private float patrolPointThreshold = 1f;
    [SerializeField] private float searchTimer;
    [Header("엄폐")]
    [SerializeField] private float minCoverDistance = 3f; //엄폐 지점 최소 거리
    [SerializeField] private float maxCoverDistance = 10f;// " 최대 거리
    [SerializeField] private LayerMask coverPointMask; //엄폐 레이어
    private Vector3 currentCoverPos;
    [SerializeField] private float coverSearchRadius = 20f; //엄폐 지점 검색 반경
    [SerializeField] private float timeInCover = 2f; //엄폐 유지 시간
    //최적화
    private Collider[] wallCols = new Collider[10];
    private Collider[] targetCols = new Collider[10];
    private const float MIN_DISTANCE_TO_STOP = 0.5f;
    private Coroutine currentAICor;
    private Coroutine currentAttackCor;

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
    public int MaxAttackTime
    {
        get; private set;
    }

    private void SetAIState(AIState newState)
    {
        if (currentState == newState || !alive) return;
        if (currentAICor != null)
        {
            StopCoroutine(currentAICor);
        }
        currentState = newState;
        Debug.Log($"봇 상태 변경: {currentState}");
        
        switch (currentState)
        {
            case AIState.Patrol:
                currentAICor = StartCoroutine(PatrolRoutine());
                break;
            case AIState.Chase:
                currentAICor = StartCoroutine(ChaseRoutine());
                break;
            case AIState.Cover:
                currentAICor = StartCoroutine(CoverRoutine());
                break;
            case AIState.Attack:
                currentAICor = StartCoroutine(AttackRoutine());
                break;
            case AIState.Search:
                currentAICor = StartCoroutine(SearchRoutine());
                break;
            case AIState.Reload:
                currentAICor = StartCoroutine(ReloadRoutine());
                break;
        }
    }
    #region 상태 - 순찰
    private IEnumerator PatrolRoutine()
    {
        anim.SetBool("Running", true);
        agent.isStopped = false;
        while (currentState == AIState.Patrol)
        {
            if (foundPlayer == true)
            {
                SetAIState(AIState.Chase);
                yield break;
            }

            if (patrolPoints.Length == 0)
            {
                Debug.LogWarning("순찰 지점 설정 안됨!");
                anim.SetBool("Running", false);
                agent.isStopped = true;
                yield break;
            }

            //순찰 지점 이동
            Vector3 targetPatrolPos = patrolPoints[currentPatrolIndex].position;
            agent.SetDestination(targetPatrolPos);

            //순찰 지점 도달 확인
            if (!agent.pathPending && agent.remainingDistance < patrolPointThreshold)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; //다음 지점
                yield return new WaitForSeconds(1f);
            }
            yield return null;
        }
    }
    #endregion
    #region 상태 - 추격
    private IEnumerator ChaseRoutine()
    {
        anim.SetBool("Running", true);
        agent.isStopped = false;

        while (currentState == AIState.Chase)
        {
            if (!foundPlayer)
            {
                SetAIState(AIState.Search); yield break;
            }

            if (Vector3.Distance(transform.position, lastPlayerTransform) <= attackRange)
            {
                SetAIState(AIState.Attack); yield break;
            }

        }
    }


    #endregion
    #region 상태 - 공격
    private IEnumerator AttackRoutine()
    {
        while (currentState == AIState.Attack)
        {
            // 플레이어가 시야에 없거나 사거리 벗어나면 추격
            if (!foundPlayer || Vector3.Distance(transform.position, lastPlayerTransform) > attackRange + 1f)
            {
                SetAIState(AIState.Chase);
                yield break;
            }

            // 재장전이 필요하면 재장전 상태로 전환
            if (currentWeapon != null && currentWeapon.NeedToReload())
            {
                SetAIState(AIState.Reload);
                yield break;
            }

            // 플레이어 바라보기 (부드러운 회전)
            LookAtPlayer(lastPlayerTransform);

            //무기 발사
            if (currentWeapon != null && currentAttackCor == null) // 공격 코루틴이 현재 실행 중이 아닐 때만 시작
            {
                currentAttackCor = StartCoroutine(currentWeapon.Fire()); // EnemyAI 인스턴스를 Fire 코루틴에 전달
            }

            yield return null; // 다음 프레임까지 대기 (공격 코루틴이 자체적으로 대기하니 null)
        }
        // AttackRoutine 끝나면 공격 코루틴도 중지
        if (currentAttackCor != null)
        {
            StopCoroutine(currentAttackCor);
            currentAttackCor = null;
            anim.SetBool("Fire", false);
        }
    }
    #endregion
    #region 상태 - 엄폐
    private IEnumerator CoverRoutine()
    {
        NowHiding = true;
        anim.SetBool("Running", true);
        agent.isStopped = false;
        //적절한 지점 찾기
        currentCoverPos = FindCoverSpot();
        if (currentCoverPos != Vector3.zero)
        {
            agent.SetDestination(currentCoverPos);
            //도달하고 멈추기까지 대기
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + MIN_DISTANCE_TO_STOP && agent.velocity.sqrMagnitude < 0.1f);

            //도달 후
            NowHiding = false;
            anim.SetBool("Running", false);
            agent.isStopped = true;
            Debug.Log("엄페 완");
            if (currentWeapon.NeedToReload())
            {
                SetAIState(AIState.Reload); yield break;
            }
            SetAIState(AIState.Search); yield break;
        }
        else
        {
            SetAIState(AIState.Search);
        }
        yield return null;
    }
    #endregion
    #region 상태 - 순찰
    private IEnumerator SearchRoutine()
    {
        anim.SetBool("Running", false);
        agent.isStopped = false;

        Vector3 searchOrigin = lastPlayerTransform;
        Vector3 randomSearch = Vector3.zero;

        while (currentState == AIState.Search && searchTimer > 0)
        {
            if (foundPlayer)
            {
                SetAIState(AIState.Attack);
                yield break;
            }
            if (agent.remainingDistance <= agent.stoppingDistance + MIN_DISTANCE_TO_STOP || !agent.hasPath || agent.pathStatus != NavMeshPathStatus.PathComplete)
            {
                //마지막 위치 기반으로 넓게 탐색
                randomSearch = GetRandomNavMeshPoint(searchOrigin, viewRadius * 1.5f);
                if (randomSearch != Vector3.zero)
                {
                    agent.SetDestination(randomSearch);
                }
                else
                {
                    Debug.LogWarning("탐색 지점 못찾음");
                    break;
                }
            }
            searchTimer -= Time.deltaTime;
            yield return null;
        }
        if (!foundPlayer)
        {
            SetAIState(AIState.Patrol);
        }
    }
    #endregion
    #region 상태 - 장전
    private IEnumerator ReloadRoutine()
    {
        anim.SetBool("Running", false);
        agent.isStopped = true;
        NowReloading = true;
        Debug.Log("재장전 시작!");
        SetAIState(AIState.Cover); // 엄폐하고서 재장전
        yield return new WaitUntil(() => NowHiding = false);
        if (currentWeapon != null)
        {
            yield return StartCoroutine(currentWeapon.ReloadWeapon());
            yield return new WaitUntil(() => !NowReloading);
        }
        Debug.Log("재장전 완료!");

        if (foundPlayer)
        {
            SetAIState(AIState.Attack);
        }
        else
        {
            SetAIState(AIState.Search);
        }
        yield break;
    }
    #endregion
    Vector3 GetRandomNavMeshPoint(Vector3 origin, float radius)
    {
        Vector3 randomPoint = origin + UnityEngine.Random.insideUnitSphere * radius;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomPoint, out hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return Vector3.zero;
    }
    private Vector3 FindCoverSpot()
    {
        Vector3 coverBestPos = Vector3.zero;
        //레이어에 따른 오브젝트 검색
        int numCover = Physics.OverlapSphereNonAlloc(transform.position, coverSearchRadius, wallCols, coverPointMask);
        if (numCover == 0) return Vector3.zero;
        
        for (int i = 0; i < numCover; i++)
        {
            Collider cover = wallCols[i];
            Vector3 coverPos = cover.transform.position;
            Vector3 directionToCover = (coverPos - transform.position).normalized;
            Vector3 directionToPlayer = (lastPlayerTransform - coverPos).normalized;
            //엄폐물 뒤 반대 방향에 엄폐 위치 두기
            Vector3 potentialPos = coverPos - directionToPlayer * 2f;//엄폐물에서 2M 뒤로
            NavMeshHit hit;
            if (NavMesh.SamplePosition(potentialPos, out hit, 5f, NavMesh.AllAreas))
            {
                Vector3 validCoverPos = hit.position;

                //플레이어로부터 얼마나 가려지는지 Raycast로 확인하고 엄폐물이 가리는지 확인
                if (Physics.Raycast(validCoverPos, (lastPlayerTransform - validCoverPos).normalized, Vector3.Distance(validCoverPos, lastPlayerTransform), ObstacleMask))
                {
                    float distanceFromPlayer = Vector3.Distance(validCoverPos, lastPlayerTransform);
                }
            }
        }
        return coverBestPos;
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
            currentWeapon = weapon.GetComponent<EnemyWeapon>();

            switch (currentWeapon.type)
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
        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }
        if (anim == null)
        {
            anim = GetComponent<Animator>();
        }
        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
        }
        SetAIState(AIState.Patrol);
    }

    private void FixedUpdate()
    {
        if (alive == true)
        {
            Viewing();
        }
    }

    #region 구버전 장전
    // 숨고 -> 장전 -> 움직이기
    /// <summary>
    /// 재장전 할 시기에 숨고 장전하는 로직
    /// </summary>
    /// <returns></returns>
    /// 
    //IEnumerator Reloading()
    //{
    //    if(currentAttackTime != maxAttackTime)
    //    {
    //        StartCoroutine(Hiding());
    //        yield return new WaitUntil(() => NowHiding == false);
    //        anim.SetBool("Running", false);
    //        anim.SetBool("Reload", true);
    //        yield return reloadDelay;
    //        currentAttackTime = maxAttackTime;
    //        NowReloading = false; //장전 완료!
    //        anim.SetBool("Reload", false);
    //    }
    //    
    //
    //}
    #endregion
    #region 구버전 숨기
    /// <summary>
    /// Reloading으로 불러올 것. 절대 단독 사용하지 마세요.
    /// </summary>
    /// <returns></returns>
    //IEnumerator Hiding()//Reloading으로 불러오세요!
    //{
    //    //아무튼 숨는 로직
    //    //앞이 벽이다 = 숨었다! else 벽이 주변에 없다!
    //    yield return new WaitForSeconds(UnityEngine.Random.Range(0f, 0.1f));//동시 실행으로 과부하 방지
    //    int walls = Physics.OverlapSphereNonAlloc(transform.position, 4, wallCols, ObstacleMask);
    //    //Transform lastPos = transform;
    //
    //    if (walls > 0)
    //    {
    //        Transform wall = wallCols[0].transform;
    //
    //        Vector3 directionToPlayer = (lastPlayerTransform - wall.position).normalized;
    //        Vector3 potentialPos = wall.position - directionToPlayer * 2f;
    //        NavMeshHit hit;
    //        if (NavMesh.SamplePosition(potentialPos, out hit, 5f, NavMesh.AllAreas))
    //        {
    //            Vector3 hidePos = hit.position;
    //            agent.SetDestination(hidePos);
    //            anim.SetBool("Running", true);
    //            yield return new WaitUntil(() => 
    //            (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f && agent.velocity.sqrMagnitude < 0.1f));
    //            //길찾기 끝남/남은 거리가 stoppingDistance정도?, 속도가 정지한 수준?
    //            agent.ResetPath();
    //            NowHiding = false;
    //            Debug.Log("잘 숨었다");
    //        }
    //        else
    //        {
    //            Debug.LogWarning("유효 공간 찾기 실패");
    //            anim.SetBool("Running", false);
    //            NowHiding = false;
    //        }
    //    }
    //    else
    //    {
    //        Debug.LogWarning("숨을 곳은 없네");
    //        anim.SetBool("Running", false);
    //        NowHiding = false;
    //    }
    //    yield return new WaitUntil(() => NowReloading == false);
    //    currentCor = null;
    //    //움직여!
    //}
    #endregion
    #region 구버전 추격
    //IEnumerator Chase()
    //{
    //
    //    anim.SetBool("Running", true);
    //    while (targetList.Count == 0 && transform.position != lastPlayerTransform)
    //    {
    //        agent.SetDestination(lastPlayerTransform);
    //        yield return null;
    //       
    //
    //    }
    //    if (targetList.Count == 0)
    //    {
    //        foundPlayer = false;
    //        anim.SetBool("Running", false);
    //    }
    //}
    #endregion
    void Viewing()
    {
        targetList.Clear();
        int numTargets = Physics.OverlapSphereNonAlloc(transform.position, viewRadius, targetCols, targetMask);
        if (numTargets == 0)
        {
            foundPlayer = false;
            return;
        }

        Vector3 lookDir = AngleToDir(transform.eulerAngles.y);
        float cosHalfViewAngle = Mathf.Cos(viewAngle * 0.5f * Mathf.Deg2Rad);

        for (int i = 0; i < numTargets; i++)
        {
            Collider target = targetCols[i];
            Vector3 targetDir = (target.transform.position - transform.position).normalized;

            if (Vector3.Dot(lookDir, targetDir) >= cosHalfViewAngle)
            {
                RaycastHit hit;
                Vector3 raycastOrigin = transform.position + Vector3.up * 0.5f;
                Vector3 directionToTargetCenter = (target.bounds.center - raycastOrigin).normalized;

                if (!Physics.Raycast(raycastOrigin, directionToTargetCenter, out hit, viewRadius, ObstacleMask))
                {
                    targetList.Add(target);
                    playerInSightThisFrame = true;
                    lastPlayerTransform = target.transform.position;
                    // HandleAttack(target); // 시야 함수에서는 공격 로직을 호출하지 않음, Attack 상태에서 처리
                }
                else if (hit.collider == target) // Raycast가 자기 자신(타겟)에 맞았다면 통과 (예: 플레이어 Collider가 크면)
                {
                    targetList.Add(target);
                    playerInSightThisFrame = true;
                    lastPlayerTransform = target.transform.position;
                }
            }
        }
        foundPlayer = playerInSightThisFrame;
    }
    Vector3 AngleToDir(float angle)
    {
        float radian = angle * Mathf.Deg2Rad;
        return new Vector3(Mathf.Sin(radian), 0f, Mathf.Cos(radian));
    }

    void LookAtPlayer(Vector3 playerPos)
    {
        Vector3 direction = (playerPos - transform.position).normalized;
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


    private void OnDrawGizmosSelected()
    {
        // 1. 시야 범위 그리기
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, viewRadius);

        // 시야 각도 (부채꼴)
        Vector3 forward = transform.forward;
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle / 2, 0) * forward;
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle / 2, 0) * forward;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary * viewRadius);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary * viewRadius);
        Gizmos.DrawLine(transform.position + leftBoundary * viewRadius, transform.position + rightBoundary * viewRadius);


        // 2. 발견된 플레이어 위치 그리기
        if (foundPlayer)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(lastPlayerTransform, 0.5f); // 플레이어의 마지막 목격 위치
            Gizmos.DrawLine(transform.position, lastPlayerTransform); // 봇에서 플레이어까지 선 그리기
        }

        // 3. NavMeshAgent의 목적지 그리기
        if (agent != null && agent.hasPath)
        {
            Gizmos.color = Color.yellow;
            // 현재 목적지
            Gizmos.DrawSphere(agent.destination, 0.3f);

            // 경로 그리기
            Vector3 lastCorner = transform.position;
            foreach (var corner in agent.path.corners)
            {
                Gizmos.DrawLine(lastCorner, corner);
                lastCorner = corner;
            }
        }

        // 4. 공격 사거리 그리기
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // 오렌지색, 반투명
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 5. 순찰 지점 그리기
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < patrolPoints.Length; i++)
            {
                if (patrolPoints[i] != null)
                {
                    Gizmos.DrawSphere(patrolPoints[i].position, 0.5f); // 각 순찰 지점
                    // 순찰 경로 선으로 연결 (선형 순찰이라고 가정)
                    if (i < patrolPoints.Length - 1 && patrolPoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[i + 1].position);
                    }
                    else if (patrolPoints.Length > 1 && i == patrolPoints.Length - 1 && patrolPoints[0] != null)
                    {
                        // 마지막 지점에서 첫 지점으로 (루프)
                        Gizmos.DrawLine(patrolPoints[i].position, patrolPoints[0].position);
                    }
                }
            }
            // 현재 순찰 중인 지점은 더 눈에 띄게
            if (patrolPoints.Length > currentPatrolIndex && patrolPoints[currentPatrolIndex] != null)
            {
                Gizmos.color = Color.white;
                Gizmos.DrawSphere(patrolPoints[currentPatrolIndex].position, 0.6f);
            }
        }

        // 6. 엄폐 지점 검색 반경 그리기
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, coverSearchRadius);

        // 7. 현재 엄폐 지점 그리기
        if (currentState == AIState.Cover && currentCoverPos != Vector3.zero)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(currentCoverPos, Vector3.one * 0.8f); // 엄폐 지점은 큐브로
        }
    }


}
