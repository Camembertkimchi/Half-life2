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
    [SerializeField] GameObject weapon;//무기
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
        yield return new WaitUntil(() => nowHiding == false);
        //애니메이션 재생
        yield return reloadDelay;
        currentAttackTime = maxAttackTime;
        nowReloading = false; //장전 완료!
    }





    /// <summary>
    /// Reloading으로 불러올 것. 절대 단독 사용하지 마세요.
    /// </summary>
    /// <returns></returns>
    IEnumerator Hiding()//Reloading으로 불러오세요!
    {
        //아무튼 숨는 로직
        //Player가 시야에 없다 + 앞이 벽이다 = 숨었다! else 벽이 주변에 없다!
        nowHiding = false;

        
        yield return new WaitUntil(() => nowReloading == false);
        //움직여!
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
