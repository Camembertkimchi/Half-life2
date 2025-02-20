using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletCon : MonoBehaviour
{
    [SerializeField] float bulletSpeed = 20f;
    int damage;
    BulletPooling bulletPool;
    [SerializeField]bool isReleased = false;
    static readonly WaitForSeconds bulletReleaseTime = new WaitForSeconds(2f);
    IEnumerator currentCor;

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

    public void Initialize(BulletPooling pool)
    {
        bulletPool = pool;
        Debug.Log(bulletPool.ToString());
        if (isReleased)
        {
            return;
        }
        isReleased = true;

        Debug.Log("제거반 불러라");
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
        Debug.Log("제거 한다");
        bulletPool.ReleaseBullet(gameObject);
    }

    void OnDisable()
    {
        
        if (!isReleased) // 중복 방지용
        {
            Debug.Log($"OnDisable에서 isReleased = true 처리됨: {gameObject.name}");
            isReleased = true;
        }
        Debug.Log("응애 나 꺼졌엉");
        if(currentCor != null)
        {
            StopCoroutine(currentCor);
            currentCor = null;
        }
    }



    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            var player = GetComponent<PlayerMovement>();
            player.ChangeHp(-damage);
        }
        
         
        
        
        
        else if (other.gameObject.CompareTag("DistroyableObj"))
        {
            //var target = GetComponent<ObjectHealth>();
            //target.ChangeHp(-damage);
        }
    }

}
