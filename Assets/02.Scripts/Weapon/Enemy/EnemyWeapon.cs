using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyWeapon : MonoBehaviour, IEnemyWeapon
{
    [SerializeField] ScriptableWeapon weaponInfo;
    EnemyAI enemyAI;
    [SerializeField] Transform muzzlePos;
    [SerializeField] int fireTimes;//���� �ֱ�
    static readonly WaitForSeconds weaponDelay = new WaitForSeconds(1f);
    [SerializeField] int maxFireTimes;//�� �ֱ� �� �߻��ϴ� �Ѿ� ����
    Weapons type;



    Weapons IEnemyWeapon.Type 
    { 
        get { return type; } 
        set { type = value; } 
    }

    private void OnEnable()
    {
        enemyAI = GetComponentInParent<EnemyAI>();
        weaponInfo.bulletScript = weaponInfo.bullet.GetComponent<BulletCon>();

        weaponInfo.bulletScript.Damage = weaponInfo.damage;
        maxFireTimes = weaponInfo.oneCyleFireAmmo;
        fireTimes = maxFireTimes;
        type = weaponInfo.weapon;

        
    }

    public void FireWeapon()
    {
        StartCoroutine(Fire());
        Debug.Log("�Լ� �ҷ���");
    }

    public IEnumerator Fire()
    {
        Debug.Log("���� ��������");
        while(enemyAI != null && enemyAI.AliveState == true && enemyAI.AttackTime > 0)
        {
            Debug.Log("�հ�");
            while(fireTimes > 0)
            {
                if(type == Weapons.Shotgun)
                {
                    for(int i = 0; i < 12;  i++)
                    {
                        Instantiate(weaponInfo.bullet, muzzlePos.position, muzzlePos.rotation);
                    }
                }
                else
                {
                    Instantiate(weaponInfo.bullet, muzzlePos.position, muzzlePos.rotation);
                }
                
                fireTimes--;
                Debug.Log(fireTimes);
                yield return new WaitForSeconds(weaponInfo.fireRate);
                Debug.Log("�� �ֱ� ��");
            }

            fireTimes = maxFireTimes;
            enemyAI.AttackTime--;
            yield return weaponDelay;

        }
        yield break;
    }
}
