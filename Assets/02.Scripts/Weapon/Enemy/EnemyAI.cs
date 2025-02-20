using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using static ScriptableWeapon;

public interface IEnemyWeapon
{
    abstract void FireWeapon();
    Weapons Type { get; set; }
     
}

public interface IEnmeyState
{
    void EnterState(EnemyAI enemy);
    void ExitState(EnemyAI enemy);
    void UpdateState(EnemyAI enemy);

}

public class EnemyAI : MonoBehaviour
{
    [SerializeField] GameObject weapon;//����
    IEnemyWeapon currentWeapon;
    
    [SerializeField] int hp;
    NavMeshAgent agent;
    [SerializeField]static readonly WaitForSeconds reloadDelay = new WaitForSeconds(3f);
    [SerializeField] int maxAttackTime = 5;
    [SerializeField] int currentAttackTime;
    bool nowReloading = false;
    bool nowHiding = false;
    bool alive = true;



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
            }

            currentAttackTime = maxAttackTime;
            //����2�� �Ѿ� ������ ���� �������� �ʰ� ���� Ƚ���� ���� ������ ������

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
        yield return new WaitUntil(() => nowHiding == false);
        //�ִϸ��̼� ���
        yield return reloadDelay;
        currentAttackTime = maxAttackTime;
        nowReloading = false; //���� �Ϸ�!
    }





    /// <summary>
    /// Reloading���� �ҷ��� ��. ���� �ܵ� ������� ������.
    /// </summary>
    /// <returns></returns>
    IEnumerator Hiding()//Reloading���� �ҷ�������!
    {
        //�ƹ�ư ���� ����
        //Player�� �þ߿� ���� + ���� ���̴� = ������! else ���� �ֺ��� ����!
        nowHiding = false;

        
        yield return new WaitUntil(() => nowReloading == false);
        //������!
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
