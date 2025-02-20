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

        Debug.Log("���Ź� �ҷ���");
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
        Debug.Log("���� �Ѵ�");
        bulletPool.ReleaseBullet(gameObject);
    }

    void OnDisable()
    {
        
        if (!isReleased) // �ߺ� ������
        {
            Debug.Log($"OnDisable���� isReleased = true ó����: {gameObject.name}");
            isReleased = true;
        }
        Debug.Log("���� �� ������");
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
