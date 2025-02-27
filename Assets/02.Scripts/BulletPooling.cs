using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class BulletPooling : MonoBehaviour
{
    //싱글톤으로 접근 용이
    public static BulletPooling Instance {  get; private set; }
    public GameObject bulletPrefab;//대상
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
        //ObjectPool 생성자<T> where T : class
        //createFunc = Func<T> 만든다는 이야기. 새 오브젝트 생성
        //actionOnGet: Action<T>. 풀에서 꺼내면 실행
        //actionOnRelease: Action<T>. 풀에 반환 되면 실행
        //actionOnDestory: Action<T>. 삭제될 때 실행
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
