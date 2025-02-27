using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletCon : MonoBehaviour
{
    [SerializeField] float bulletSpeed = 20f;
    [SerializeField]int damage;
    BulletPooling bulletPool;
    [SerializeField]bool isReleased = false;
    static readonly WaitForSeconds bulletReleaseTime = new WaitForSeconds(2f);
    IEnumerator currentCor;
    [SerializeField]bool shootedByPlayer;
    public int Damage
    {
        get { return damage; } set { damage = value; }
    }

    public void ReflectDamage(int bulletDamage)
    {
        damage = bulletDamage;
    }

    private void FixedUpdate()
    {
        //transform.position += Vector3.forward * bulletSpeed;
        transform.Translate(Vector3.forward * bulletSpeed, Space.Self);

        if (Input.GetKeyDown(KeyCode.U))
        {
            bulletPool.ReleaseBullet(this.gameObject);
        }

        
    }



    public void Initialize(BulletPooling pool, bool x)
    {
        shootedByPlayer = x;
        if (!shootedByPlayer) bulletSpeed = 15f;
        else bulletSpeed = 20f;
        bulletPool = pool;
        if (!isReleased)
        {
            Debug.Log("응 처음부터 true임~");
            return;
        }
        isReleased = true;

        if(currentCor == null)
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
        if(!isReleased) isReleased = true;
    }


    void OnDisable()
    {
        
       
        if(isReleased == true)
        {
            isReleased = false;
            if(currentCor != null)
            {
                StopCoroutine(currentCor);
            }
           
            currentCor = null;
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        if(!shootedByPlayer)//적이 쏠 때
        {
            if (other.gameObject.CompareTag("Player"))
            {
                var player = GetComponent<PlayerMovement>();
                player.ChangeHp(-damage);
            }
        }
        
        
        

        if(shootedByPlayer == true)
        {
            if (other.gameObject.CompareTag("Enemy"))
            {
                var enemy = GetComponent<EnemyAI>();
                enemy.ChangeHp(-damage);
            }
        }
        
        
        
        //else if (other.gameObject.CompareTag("DistroyableObj"))
        //{
        //    //var target = GetComponent<ObjectHealth>();
        //    //target.ChangeHp(-damage);
        //}
    }

}
