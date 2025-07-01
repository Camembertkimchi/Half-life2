using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ParticlePool : MonoBehaviour
{
    public GameObject fxPrefab;//대상
    ObjectPool<GameObject> particlePool;

    private void Awake()
    {

        particlePool = new ObjectPool<GameObject>
            (
            createFunc: () => Instantiate(fxPrefab, transform),
            actionOnGet: fx => fx.SetActive(true),
            actionOnRelease: fx => fx.SetActive(false),
            actionOnDestroy: fx => Destroy(fx),
            collectionCheck: false, defaultCapacity: 50, maxSize: 300
            );


    }


    public GameObject GetParticle() => particlePool.Get();
    //ObjectPool.Get()은, 갯수가 없으면 ++을 해서 만들어줌
    public void ReleaseParticle(GameObject fx)
    {

        particlePool.Release(fx);

    }
}