using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPooling : MonoBehaviour
{
    
    public GameObject bulletPrefab;//대상
    ObjectPool<GameObject> bulletPool;

    private void Awake()
    {
     
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
    //ObjectPool.Get()은, 갯수가 없으면 ++을 해서 만들어줌
    public void ReleaseBullet(GameObject bullet)
    {

        bulletPool.Release(bullet);
       
    }

}
