using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPooling : MonoBehaviour
{
    //�̱������� ���� ����
    public static BulletPooling Instance {  get; private set; }
    public GameObject bulletPrefab;//���
    ObjectPool<GameObject> bulletPool;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        //ObjectPool ������<T> where T : class
        //createFunc = Func<T> ����ٴ� �̾߱�. �� ������Ʈ ����
        //actionOnGet: Action<T>. Ǯ���� ������ ����
        //actionOnRelease: Action<T>. Ǯ�� ��ȯ �Ǹ� ����
        //actionOnDestory: Action<T>. ������ �� ����
        bulletPool = new ObjectPool<GameObject>
            (
            createFunc: () => Instantiate(bulletPrefab, transform),
            actionOnGet: bullet => bullet.SetActive(true),
            actionOnRelease: bullet => bullet.SetActive(false),
            actionOnDestroy: bullet => Destroy(bullet),
            collectionCheck: false, defaultCapacity: 50, maxSize: 300
            );


    }


    public GameObject GetBullet()=>bulletPool.Get();
    //ObjectPool.Get()��, ������ ������ ++�� �ؼ� �������
    public void ReleaseBullet(GameObject bullet)
    {

        bulletPool.Release(bullet);
       
    }

}
