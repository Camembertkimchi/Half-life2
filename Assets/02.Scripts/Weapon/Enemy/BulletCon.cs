using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletCon : MonoBehaviour
{
    [SerializeField] float bulletSpeed = 20f;
    int damage;
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
        transform.position += Vector3.forward * bulletSpeed;
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
