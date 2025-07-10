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
    //HideInInspector = 그냥 에디터상에서 깔끔하게 유지하기 위한 것
    //NonSerialized = 직렬화 막기, 씬 저장시 함께 저장되거나 불러와지지 않도록 할 때 사용.
    //마찬가지로 사용은 안됨. 동적 생성 참조 계산되야할 캐시된 값 등등에 사용
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
    //[SerializeField] private float minCoverDistance = 3f; //엄폐 지점 최소 거리
    //[SerializeField] private float maxCoverDistance = 10f;// " 최대 거리
    [SerializeField] private LayerMask coverPointMask; //엄폐 레이어
    private Vector3 currentCoverPos;
    [SerializeField] private float coverSearchRadius = 20f; //엄폐 지점 검색 반경
    [SerializeField] private float timeInCover = 5f; //엄폐 유지 시간
    [SerializeField] private int maxAttempts = 10;
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
        get
        {
            return maxAttackTime;
        }
    }

    private void SetAIState(AIState newState)
    {
        if (currentState == newState || !alive) return;
        StopAllCoroutines();
        currentState = newState;
        switch (currentState)
        {
            //!NowReloading을 붙이지 않아도 시작 부분에서 제어하는 것이 효과적!
            //애초에 역할은 상태 전환만 하면 된다.
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
        Debug.Log($"봇 상태 변경: {currentState}");
    }
    #region 상태 - 순찰
    private IEnumerator PatrolRoutine()
    {
        if (NowReloading)
        {
            Debug.Log("장전 중이라 무시");
            yield break;
        }
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
                agent.isStopped = true;
                anim.SetBool("Running", false);
                yield return new WaitForSeconds(1f);
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length; //다음 지점
                agent.isStopped = false;
                anim.SetBool("Running", true);
            }
            yield return null;
        }
        anim.SetBool("Running", false);
        agent.isStopped = true;
    }
    #endregion
    #region 상태 - 추격
    private IEnumerator ChaseRoutine()
    {
        if (NowReloading)
        {
            Debug.Log("장전 중이라 무시");
            yield break;
        }
        anim.SetBool("Running", true);
        agent.isStopped = false;
        anim.SetBool("Fire", false);
        while (currentState == AIState.Chase)
        {
            if (!foundPlayer)
            {
                SetAIState(AIState.Search); yield break;
            }

            float currentDistance = Vector3.Distance(transform.position, lastPlayerTransform);
            if (currentDistance <= attackRange) // 16m 이내로 들어오면 공격
            {
                Debug.Log($"공격 범위 ({attackRange}) 진입");
                SetAIState(AIState.Attack);
                yield break;
            }
            agent.SetDestination(lastPlayerTransform);
            yield return null;
        }
        anim.SetBool("Running", false);
        agent.isStopped = true;
    }


    #endregion
    #region 상태 - 공격
    private IEnumerator AttackRoutine()
    {
        agent.isStopped = true;
        anim.SetBool("Running", false);
        anim.SetBool("Fire", true);
        if (currentWeapon != null)
        {
            currentWeapon.Fire();
        }
        while (currentState == AIState.Attack)
        {
            //플레이어 바라보기
           if (foundPlayer)
            {
                LookAtPlayer(lastPlayerTransform);
            }
            if (currentWeapon != null && currentWeapon.ShouldStopFiring) // <- EnemyWeapon에서 보낸 신호 확인
            {
                // 무기 스크립트에게 발사 중지 요청
                currentWeapon.StopFiring();

                if (currentWeapon.NeedToReload())
                {
                    Debug.Log("AttackRoutine: 재장전 필요");
                    SetAIState(AIState.Reload);
                }
                else if (!foundPlayer) // 플레이어를 놓친 경우
                {
                    Debug.Log("AttackRoutine: 플레이어 놓침");
                    SetAIState(AIState.Search);
                }
                // else 필요하면 ㄱ
                yield break;
            }

            //상태 전환 조건 (EnemyAI 자체 판단)
            //플레이어를 완전히 놓쳤을 경우 (weapon.ShouldStopFiring과 중복될 수 있으나 안전을 위해 유지)
            if (!foundPlayer)
            {
                Debug.Log("AttackRoutine: 플레이어 놓침. Search 상태로 전환.");
                currentWeapon?.StopFiring(); // 무기 발사 중지
                SetAIState(AIState.Search);
                yield break;
            }

            //플레이어가 공격 사거리를 벗어났을 경우
            float currentDistance = Vector3.Distance(transform.position, lastPlayerTransform);
            if (currentDistance > attackRange * 1.5f)
            {
                Debug.Log($"AttackRoutine: 플레이어가 너무 멀어짐 ({currentDistance:F2}m). Chase 상태로 전환.");
                currentWeapon.StopFiring(); // 무기 발사 중지
                SetAIState(AIState.Chase);
                yield break;
            }

            yield return null;
        }
        currentWeapon.StopFiring(); // 2중으로 요청
        anim.SetBool("Fire", false);
        anim.SetBool("Running", false);
        Debug.Log("AttackRoutine 종료.");
    }
    #endregion
    #region 상태 - 엄폐
    private IEnumerator CoverRoutine()
    {
        //장전과 한몸이었지만 이제 진짜 한몸이 되버렸고
        //얘는 따로 구분함.
        //NowHiding = true;
        //anim.SetBool("Running", true);
        //agent.isStopped = false;
        ////적절한 지점 찾기
        //currentCoverPos = FindCoverSpot();
        //if (currentCoverPos != Vector3.zero)
        //{
        //    agent.SetDestination(currentCoverPos);
        //    //도달하고 멈추기까지 대기
        //    yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + MIN_DISTANCE_TO_STOP && agent.velocity.sqrMagnitude < 0.1f);
        //
        //    //도달 후
        //    NowHiding = false;
        //    anim.SetBool("Running", false);
        //    agent.isStopped = true;
        //    Debug.Log("엄페 완");
        //    yield break;
        //}
        //else //못찾았다면?
        //{
        //    NowHiding = false;
        //    anim.SetBool("Running", false);
        //    agent.isStopped = true;
        //    yield break;
        //}
        NowHiding = true;
        anim.SetBool("Running", true);
        agent.isStopped = false;

        Debug.Log("CoverRoutine");

        Vector3 coverDestination = FindCoverSpot(); // 엄폐 지점 찾기
        if (coverDestination != Vector3.zero)
        {
            agent.SetDestination(coverDestination);
            // 엄폐 지점 도달까지 대기
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + MIN_DISTANCE_TO_STOP && agent.velocity.sqrMagnitude < 0.1f);

            Debug.Log("엄폐 지점 도달!");
            anim.SetBool("Running", false);
            agent.isStopped = true;
            yield return new WaitForSeconds(timeInCover); 
            if (foundPlayer)
            {
                SetAIState(AIState.Attack);
            }
            else
            {
                SetAIState(AIState.Patrol);
            }
            yield break; // 코루틴 종료
        }
        else // 엄폐 지점을 못 찾았다면
        {
            Debug.LogWarning("엄폐 지점 없는데용");
            NowHiding = false; // 엄폐 실패
            anim.SetBool("Running", false);
            agent.isStopped = true;
            // 엄폐를 못했으니 어떤 상태로 돌아갈지 결정 (예: Attack, Search)
            if (foundPlayer)
            {
                SetAIState(AIState.Attack);
            }
            else
            {
                SetAIState(AIState.Patrol);
            }
            yield break;
        }
    }
    #endregion
    #region 상태 - 순찰
    private IEnumerator SearchRoutine()
    {
        if (NowReloading)
        {
            Debug.Log("장전 중이라 무시");
            yield break;
        }
        anim.SetBool("Running", true);
        anim.SetBool("Fire", false);
        agent.isStopped = false;

        Vector3 searchOrigin = lastPlayerTransform;
        Vector3 randomSearch = Vector3.zero;
        searchTimer = 8f;
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
            yield break;
        }
    }
    #endregion
    #region 상태 - 장전
    private IEnumerator ReloadRoutine()
    {
        //본래 엄폐와 장전을 따로 뒀지만, 엄폐를 장전할 때만 실행하기에
        //하나로 합쳐버림
        //anim.SetBool("Running", false);
        //agent.isStopped = true;
        //NowReloading = true;
        //Debug.Log("재장전 시작!");
        //SetAIState(AIState.Cover); // 엄폐하고서 재장전
        //yield return StartCoroutine(CoverRoutine());
        //if (currentWeapon != null)
        //{
        //    yield return StartCoroutine(currentWeapon.ReloadWeapon());
        //}
        //Debug.Log("재장전 완료!");
        //NowReloading = false;
        //if (foundPlayer)
        //{
        //    SetAIState(AIState.Attack);
        //}
        //else
        //{
        //    SetAIState(AIState.Search);
        //}
        anim.SetBool("Running", false);
        agent.isStopped = true;
        NowReloading = true;
        Debug.Log("재장전 시작!");

        // 엄폐 지점 찾기 및 이동 (CoverRoutine을 직접 호출하여 기다리지 않고, 로직을 여기에 내장)
        Vector3 currentReloadCoverPos = FindCoverSpot();
        if (currentReloadCoverPos != Vector3.zero)
        {
            agent.SetDestination(currentReloadCoverPos);
            anim.SetBool("Running", true);
            agent.isStopped = false;
                                           // 엄폐 지점에 도착할 때까지 대기
            yield return new WaitUntil(() => !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + MIN_DISTANCE_TO_STOP && agent.velocity.sqrMagnitude < 0.1f);
            anim.SetBool("Running", false);
            agent.isStopped = true;
            Debug.Log("엄폐 지점에 도달");
        }
        else
        {
            Debug.Log("재장전 중 엄폐 지점 찾기 실패. 현재 위치에서 재장전.");
            // 엄폐 지점을 찾지 못해도 계속 장전
        }

        // 장전 로직
        if (currentWeapon != null)
        {
            anim.SetBool("Reload", true); // 재장전 애니메이션 시작
            yield return StartCoroutine(currentWeapon.ReloadWeapon()); // 무기 스크립트의 재장전 코루틴 호출
            anim.SetBool("Reload", false); // 재장전 애니메이션 종료
        }

        Debug.Log("재장전 완료!");
        NowReloading = false; // 재장전 플래그 해제

        // 재장전 완료 후 다음 상태 결정
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
        Vector3 playerCurrentPos = lastPlayerTransform;
        for (int i = 0; i < maxAttempts; i++)
        {
            //주변에서 랜덤한 지점 찾기
            Vector3 randomPoint = transform.position + UnityEngine.Random.insideUnitSphere * coverSearchRadius;
            randomPoint.y = transform.position.y; // Y축은 봇과 동일하게 유지하여 수평적인 엄폐만 고려

            NavMeshHit hit;
            //NavMesh 위에 있는지 확인
            if (NavMesh.SamplePosition(randomPoint, out hit, 1.0f, NavMesh.AllAreas))
            {
                Vector3 candidateCoverSpot = hit.position;

                // 3. 엄폐 지점 후보에서 플레이어까지의 시야 방해 여부 확인 (레이캐스트)
                // 레이캐스트 시작점: 엄폐 지점 후보 (살짝 위로 올려서 땅에 박히는 것 방지)
                Vector3 rayStart = candidateCoverSpot + Vector3.up * 0.5f;
                // 레이캐스트 방향: 엄폐 지점 후보에서 플레이어 방향으로
                Vector3 rayDirection = (playerCurrentPos - rayStart).normalized;
                // 레이캐스트 최대 거리: 봇과 플레이어 사이의 거리
                float rayDistance = Vector3.Distance(rayStart, playerCurrentPos);

                RaycastHit hitInfo;
                // 플레이어를 가리는 엄폐물이 있는지 확인 (플레이어 레이어는 무시해야 함)
                // layerMask를 설정하여 플레이어 레이어를 제외하고, 엄폐물 레이어만 검사하도록 합니다.
                // 예: public LayerMask coverLayerMask; (Wall, Obstacle 등)
                if (Physics.Raycast(rayStart, rayDirection, out hitInfo, rayDistance, ObstacleMask))
                {
                    // 레이캐스트가 플레이어가 아닌 다른 물체(엄폐물)에 부딪혔다면, 엄폐 가능성이 있음
                    if (hitInfo.collider.gameObject != null && !hitInfo.collider.CompareTag("Player")) // 플레이어는 엄폐물이 아니므로 제외
                    {
                        Debug.Log($"엄폐 지점 찾음: {candidateCoverSpot}, 가로막는 오브젝트: {hitInfo.collider.name}");
                        return candidateCoverSpot; // 유효한 엄폐 지점 반환
                    }
                }
            }
        }
        Debug.LogWarning("유효한 엄폐 지점을 찾지 못했습니다.");
        return Vector3.zero; // 유효한 엄폐 지점을 찾지 못하면 Vector3.zero 반환
    }

    private void OnEnable()//풀링으로 뽑을 경우를 대비한 것.
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

    private void Update()
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
        Vector3 targetPosFlat = playerPos;
        targetPosFlat.y = transform.position.y;

        Vector3 direction = (targetPosFlat - transform.position).normalized;

        if (direction == Vector3.zero) return; // 방향이 없으면 회전하지 않음

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        // 회전 속도 조절
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
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
        if (!foundPlayer)
        {
            SetAIState(AIState.Cover);
            //맞으면 숨도록
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
