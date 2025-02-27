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
    [SerializeField] GameObject weapon;//����
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

    #region �þ߰�
    [Range(0f,360f)] [SerializeField] float viewAngle; //���� ����
    [SerializeField] float viewRadius; //���� ����
    [SerializeField] LayerMask targetMask; //�÷��̾�
    [SerializeField] LayerMask ObstacleMask; //��ֹ� ���̾�
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



    //�� �� ������Ƽ�� GetComponemtFromParent<EnemyAI>()�� �����ͼ� �Ẹ�� �Ǹ� ����
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
            // �̷����ϸ� MethodInfo���·� ������
            //var method = weapon.GetType().GetMethod("Fire");

            // ������ MethodInfo���¸� IEnumerator�� ��ȯ�ϰڴ� ��, MonoBehaviour�� ������ �ڷ�ƾ�� �θ��� ������ MonoBehaviour(this) ������ �ڷ�ƾ�� MethodInfo�� ������ �������� attackCor�� ��ڴٴ� ��
            // ��, ���� ��ũ��Ʈ�� MonoBehaviour�� ��� �ް� �־ ���� MonoBehaviour�� ���ٸ� (IEnumerator)Delegate.CreateDelegate(typeof(IEnumerator), weapon.GetComponent<MonoBehaviour>(), method); �̷��� �����
            //attackCor = (IEnumerator)Delegate.CreateDelegate(typeof(IEnumerator), this, method);

            //�׳� ���⿡�� �Լ� �θ��� �ɷ� �ϴ� �غ���
            currentWeapon = weapon.GetComponent<IEnemyWeapon>();

            switch (currentWeapon.Type)
            {
                case Weapons.Pistol: maxAttackTime = pistolFireTimes; break;
                case Weapons.Shotgun: maxAttackTime = shotgunFireTimes; break;
                case Weapons.SMG: maxAttackTime = smgFireTimes; break;
                case Weapons.AR2: maxAttackTime = arFireTimes; break;
                default: Debug.Log("���� ����");
                    maxAttackTime = 0; break;
            }

            currentAttackTime = maxAttackTime;
            //����2�� �Ѿ� ������ ���� �������� �ʰ� ���� Ƚ���� ���� ������ ������

            agent = GetComponent<NavMeshAgent>();

        }
        else
        {
            Debug.Log("���� ������");
           
        }
    }

    private void Update()//For Test
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            currentWeapon.FireWeapon();
            Debug.Log("�߻��غ�");
        }

        //state?.UpdateState();
        Viewing();
        if (targetList.Count > 0)
        {
            LookAtPlayer(targetList[0].transform); // ������ �÷��̾� �ٶ󺸱�
        }
        else if(foundPlayer == true)
        {
            currentCor = Chase();
            StartCoroutine(currentCor);
        }
        if (currentAttackTime <= 0)
        {
            if(currentCor != null)
            {
                StopCoroutine(currentCor);
                currentCor = null;
               
            }
            currentCor = Reloading();
            StartCoroutine(currentCor);
        }
    }








    void ChangeState(IEnemyState status)
    {
        state?.ExitState();
        state = status;
        state.UpdateState();
    }


    // ���� -> ���� -> �����̱�
    /// <summary>
    /// ������ �� �ñ⿡ ���� �����ϴ� ����
    /// </summary>
    /// <returns></returns>
    /// 
    IEnumerator Reloading()
    {
        StartCoroutine("Hiding");
        yield return new WaitUntil(() => NowHiding == false);
        //�ִϸ��̼� ���
        yield return reloadDelay;
        currentAttackTime = maxAttackTime;
        NowReloading = false; //���� �Ϸ�!
    
    }





    /// <summary>
    /// Reloading���� �ҷ��� ��. ���� �ܵ� ������� ������.
    /// </summary>
    /// <returns></returns>
    IEnumerator Hiding()//Reloading���� �ҷ�������!
    {
        //�ƹ�ư ���� ����
        //���� ���̴� = ������! else ���� �ֺ��� ����!
        Collider[] walls = Physics.OverlapSphere(transform.position, 4, ObstacleMask);
        //Transform lastPos = transform;

        if (walls.Length > 0)
        {
            Transform wall = walls[0].transform;

            Vector3 directionToPlayer = (lastPlayerTransform - wall.position).normalized;
            Vector3 hidePos = wall.position - directionToPlayer;

            agent.SetDestination(hidePos);
            NowHiding = false;
        }
        else
        {
            NowHiding = false;
        }
        yield return new WaitUntil(() => NowReloading == false);
        currentCor = null;
        //������!
    }

    IEnumerator Chase()
    {
       

        while(targetList.Count == 0 && transform.position != lastPlayerTransform)
        {
            agent.SetDestination(lastPlayerTransform);
            yield return null;
            
        }
        if(targetList.Count == 0)
        {
            foundPlayer = false;
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
        //if (targetList.Count <= 0 && foundPlayer == true)
        //{
        //    Debug.Log("�߰� ����");
        //    StartCoroutine(Chase());
        //}
        //OverlapSphere ����� ������ �ٷ� ��ȯ
        Collider[] targets = Physics.OverlapSphere(transform.position, viewRadius, targetMask);
        if (targets.Length == 0) return;

        float lookingAngle = transform.eulerAngles.y;  //����
        Vector3 lookDir = AngleToDir(lookingAngle);

        //�̸� ��� (���� ��ȯ�� �ݺ����� �ʵ���)
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

            //���� �� ��
            if (Vector3.Dot(lookDir, targetDir) >= cosHalfViewAngle)
            {
                //Raycast�� ���� ������ �ִ��� Ȯ��
                if (!Physics.Raycast(transform.position, targetDir, viewRadius, ObstacleMask))
                {
                    targetList.Add(target);
                    foundPlayer = true;
                    lastPlayerTransform = target.transform.position;
                    if (debugingNow) Debug.DrawLine(transform.position, target.transform.position, Color.red);
                    //����
                    if (currentAttackTime > 0)
                    {
                        currentWeapon.FireWeapon();
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
        direction.y = 0; // �� ���̴� �� ���� (ȸ���� ���� ���⸸)

        targetRotation = Quaternion.LookRotation(direction);
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

        //�״� �ִϸ��̼�
        //���� ��� Instantiate�� �غ���
        currentWeapon = null;//�̰ɷ� �ѹ� ����ֽð�
        
        yield return new WaitForSeconds(10f);
        //Destroy���� �� Disable ���� ���ߵ� ���ּ� ����
    }


}
