using System.Collections;
using System.Collections.Generic;
using System.Net;
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
    //[SerializeField] GameObject fx;
    [SerializeField] float fxLifeTime;
    //Collider col;
    Rigidbody rb;
    [SerializeField]ParticlePool particlePool;
    public int Damage
    {
        get { return damage; } set { damage = value; }
    }

    public void ReflectDamage(int bulletDamage)
    {
        damage = bulletDamage;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous; // 터널링 방지
       if(particlePool == null) particlePool = GameObject.Find("ParticlePool").GetComponent<ParticlePool>();
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
       
      


        if (!shootedByPlayer)//적이 쏠 때
        {
            if (other.gameObject.CompareTag("Player"))
            {
                PlayerMovement player = other.GetComponent<PlayerMovement>();
                player.ChangeHp(damage);
                Release();
            }
        }




        if (shootedByPlayer == true)
        {
            if (other.gameObject.CompareTag("Enemy"))
            {
                EnemyAI enemy = other.GetComponent<EnemyAI>();
                enemy.ChangeHp(damage);
                Release();
            }
        }

        if (other.gameObject.CompareTag("Bullet"))
        {
            return;
        }

        if (other != null)
        {
            Vector3 hitPoint = transform.position;
            Vector3 hitNormal = -transform.forward;

            var a = particlePool.GetParticle();
            a.transform.position = hitPoint;
            a.transform.rotation = Quaternion.LookRotation(hitNormal);

            // fx 파티클 시스템이 있다면, 재생
            ParticleSystem ps = a.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Play();
            }


            
        }
        Release();

       
    }


}
    


