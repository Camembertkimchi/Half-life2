using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;

public enum PlayerWeaponState
{
    CrowBar = 1 << 0,
    GravityGun = 1 << 1,
    Pistol = 1 << 2,
    Magnum = 1 << 3,
    SMG = 1 << 4,
    AR = 1 << 5,
    Shotgun = 1 << 6,
    Sniper = 1 << 7,
    Granade = 1 << 8,
    RPG = 1 << 9
}


public class PlayerWeapon : MonoBehaviour
{
    //PlayerMovement player; 

    [SerializeField]PlayerScriptableWeapon[] weaponScripts;
    [SerializeField]GameObject[] weaponPrefabs;
    Dictionary<string, GameObject> weaponDictionary = new Dictionary<string, GameObject>();
    GameObject activatedWeapon = null;
    Dictionary<string, PlayerScriptableWeapon> weaponScriptDictionary = new Dictionary<string, PlayerScriptableWeapon>();
    PlayerScriptableWeapon activatedWeaponScript;
    PlayerWeaponState currentWeaponState;
    [SerializeField]Transform muzzlePos;
    [SerializeField] int damage;
    [SerializeField] BulletPooling pool;
    
    [SerializeField] GameObject weaponStatusUI;
    [SerializeField] Animator anim;

    static readonly WaitForSeconds ReloadingTime = new WaitForSeconds(1.2f);
    IEnumerator currentCor;
   [SerializeField] bool nowReloading = false;
   [SerializeField] bool fullAutoFiring = false;
   [SerializeField] bool semiAutoFiring = false;
    #region �ϵ� �ڵ��� ��
    //[SerializeField] int pistolAmmo; //�̰� ���� �Ѿ�
    //[SerializeField] int maxPistolAmmo;//�̰� �ִ�ġ
    //[SerializeField] int pistolMag;//�̰� ������ �Ѿ� 
    //[SerializeField] int maxPistolMag;//���� �� �ִ� ���� �ִ�ġ
    //
    //[SerializeField] int smgAmmo;
    //[SerializeField] int maxSmgAmmo;
    //
    //[SerializeField] int arAmmo;
    //[SerializeField] int maxArAmmo;
    //[SerializeField] int arMag;
    //[SerializeField] int maxArMag;
    //
    //[SerializeField] int sgAmmo;
    //[SerializeField] int maxSgAmmo;
    //[SerializeField] int sgMag;
    //[SerializeField] int maxSgMag;
    //
    //[SerializeField] int sniperAmmo;
    //[SerializeField] int maxSniperAmmo;
    //[SerializeField] int sniperMag;
    //[SerializeField] int maxSniperMag;

    //�� ���� ������ �ѹ߾���
    //[SerializeField] int grande;
    //[SerializeField] int maxGranade;
    //[SerializeField] int rpgAmmo;
    //[SerializeField] int maxRPGAmmo;
    //
    //[SerializeField] int magnumAmmo;
    //[SerializeField] int maxMagnumAmmo;
    //[SerializeField] int magnumMag;
    //[SerializeField] int maxMagnumMag;
    #endregion
    [SerializeField]int currentAmmo;
    public int CurrentAmmo//UI�����
    {
        get { return currentAmmo; }
    }

    [SerializeField]int currentMag;
    Dictionary<PlayerWeaponState, int> ammoDict = new Dictionary<PlayerWeaponState, int>();
    Dictionary<PlayerWeaponState, int> magaineDict = new Dictionary<PlayerWeaponState, int>();

    

    [SerializeField] int granadeForSmg;
    public int GrandeForSmg
    {
        get { return granadeForSmg; }
        private set { granadeForSmg = value; }
    }
    [SerializeField] int coreForAR;//��� ������ ���� �𸣰��� ���� 

    [SerializeField] GameObject ScopeUI;

    [Header("�ʴ� ��Ȯ�� ���ҿ� ȸ�� �ӵ�")]


    private float currentSpread = 0f; // ���� ź ���� ��
    private float lastShotTime = 0f;  // ���������� �߻��� �ð�


    private void Start()
    {
        foreach(var weapon in weaponScripts)
        {
            if (!weaponScriptDictionary.ContainsKey(weapon.name))
            {
                weaponScriptDictionary.Add(weapon.name, weapon);

            }
            else
            {
                return;
            }

            ammoDict[weapon.state] = weapon.maxAmmo;
            magaineDict[weapon.state] = weapon.maxMag;
            Debug.Log(weapon);
        }
        foreach(var obj in weaponPrefabs)
        {
            string name = obj.name;
            if (!weaponDictionary.ContainsKey(name))
            {
                weaponDictionary.Add(name, obj);
                Debug.Log($"{name} ���");

            }
            else
            {
                Debug.Log($"{name} �̹� ��ϵ�");
            }
        }
    }


    private void Update()
    {
        if (Time.time - lastShotTime > 0.1f && activatedWeaponScript != null) // �߻� �� 0.1�� �̻� ������ ��
        {
            if(currentSpread != activatedWeaponScript.acurracyMin)
            {
                currentSpread -= activatedWeaponScript.spreadRecoveryRate * Time.deltaTime;
                currentSpread = Mathf.Clamp(currentSpread, activatedWeaponScript.acurracyMin, activatedWeaponScript.acurracyMax);
            }
            
        }
    }


 
    public void Reloading()
    {
        if (currentCor != null) currentCor = null;
        currentCor = Reload();
        StartCoroutine(currentCor);
    }
    IEnumerator Reload()
    {
        anim.SetTrigger("Reload");
        nowReloading = true;
        yield return ReloadingTime;
        if (weaponScriptDictionary.TryGetValue(currentWeaponState.ToString(), out PlayerScriptableWeapon data))
        {
            int needAmmo = data.maxAmmo - currentAmmo;
            int ammoToReload = Mathf.Min(needAmmo, currentMag);//20, 100�̸� 20�� ���� �Լ�
            //�̷��� �ǰ� �ݿ��ϸ� ��ź���� ���ڶ�� ���ڶ� ��ŭ�� ����
            //źâ�� ����ص� ���� ��ŭ�� �������� ������ �� ����

            currentAmmo += ammoToReload;
            currentMag -= ammoToReload;
            //��ųʸ��� �ݿ���
            ammoDict[currentWeaponState] = currentAmmo;
            magaineDict[currentWeaponState] = currentMag;

        }

        #region �ϵ� �ڵ��� ����
        //switch (currentWeaponState)
        //{
        //    
        //    case PlayerWeaponState.Pistol:
        //        if(pistolMag > 0 && pistolAmmo != maxPistolAmmo)
        //        {
        //            pistolMag -= maxPistolAmmo;
        //            if (0 <= pistolMag)
        //            {
        //                pistolAmmo = maxPistolAmmo;
        //
        //            }
        //            else//������ Ǯ�� �ȵǴ� ���
        //            {
        //                pistolAmmo = pistolMag + maxPistolAmmo;
        //                if (pistolMag < 0)
        //                {
        //                    pistolMag = 0;
        //                }
        //            }
        //        }
        //       
        //        break;
        //
        //        case PlayerWeaponState.Magnum:
        //        if(magnumMag > 0 &&  magnumAmmo != maxMagnumAmmo)
        //        {
        //            magnumMag -= maxMagnumAmmo;
        //
        //            if (0 <= magnumMag)
        //            {
        //                magnumAmmo = maxMagnumAmmo;
        //
        //            }
        //            else
        //            {
        //                magnumAmmo = magnumMag + maxMagnumAmmo;
        //                if (magnumMag < 0)
        //                {
        //                    magnumMag = 0;
        //                }
        //            }
        //        }
        //       
        //        break;
        //    case PlayerWeaponState.SMG:
        //        if(pistolMag > 0 && smgAmmo != maxSmgAmmo)
        //        {
        //            pistolMag -= maxSmgAmmo;
        //
        //            if (0 <= pistolMag)
        //            {
        //                smgAmmo = maxSmgAmmo;
        //            }
        //            else
        //            {
        //                smgAmmo = pistolMag + maxSmgAmmo;
        //                if (pistolMag < 0)
        //                {
        //                    pistolMag = 0;
        //                }
        //            }
        //        }
        //        
        //        break;
        //    case PlayerWeaponState.AR:
        //
        //        if(arMag > 0 && arAmmo != maxArAmmo)
        //        {
        //            arMag -= x;
        //            if (x >= arMag)
        //            {
        //                arAmmo = maxArAmmo;
        //
        //            }
        //            else
        //            {
        //                arAmmo = x;
        //                if (arMag < 0)
        //                {
        //                    arMag = 0;
        //                }
        //            }
        //        }
        //        
        //        break;
        //        case PlayerWeaponState.Shotgun: //�ѹ߾� �����ϴ� �� ���� �� ���ּ��� �����մϴ�.
        //        while(sgMag > 0 && sgAmmo >= maxSgAmmo)
        //        {
        //            sgAmmo++;
        //            sgMag--;
        //        }
        //        break;
        //    case PlayerWeaponState.Sniper:
        //        x = sniperMag - maxSniperAmmo;
        //        sniperMag -= x;
        //        if (x >= sniperMag)
        //        {
        //            sniperAmmo = maxSniperAmmo;
        //            
        //        }
        //        else
        //        {
        //            pistolAmmo = x;
        //            if (pistolMag < 0)
        //            {
        //                pistolMag = 0;
        //            }
        //        }
        //        break;
        //    case PlayerWeaponState.Granade:
        //        x = pistolMag - maxPistolAmmo;
        //        if (x >= pistolAmmo)
        //        {
        //            pistolAmmo = maxPistolAmmo;
        //        }
        //        else
        //        {
        //            pistolAmmo = x;
        //            if (pistolMag < 0)
        //            {
        //                pistolMag = 0;
        //            }
        //        }
        //        break;
        //    case PlayerWeaponState.RPG:
        //        x = pistolMag - maxPistolAmmo;
        //        if (x >= pistolAmmo)
        //        {
        //            pistolAmmo = maxPistolAmmo;
        //        }
        //        else
        //        {
        //            pistolAmmo = x;
        //            if (pistolMag < 0)
        //            {
        //                pistolMag = 0;
        //            }
        //        }
        //        break;
        //}
        #endregion


    }



    public void EquipWeapon(PlayerWeaponState newWeaponState)
    {
        if (!weaponDictionary.TryGetValue(newWeaponState.ToString(), out GameObject weapon))
        {
            Debug.Log($"'{newWeaponState}' ���� ����");
            return;
        }

        if (activatedWeapon != null && weapon != activatedWeapon)//���Ⱑ �������� ������
        {
            activatedWeapon.SetActive(false); // ���� ���� ��Ȱ��ȭ
            weapon.SetActive(true); // �� ���� Ȱ��ȭ
            
        }
        activatedWeapon = weapon;
        if (!weaponScriptDictionary.TryGetValue(newWeaponState.ToString(), out PlayerScriptableWeapon weaponScript))
        {
            Debug.Log($"{weaponScript}");
            Debug.Log($"{newWeaponState}�� ��ũ���ͺ� ������Ʈ �Ⱥ��δ� �̸� Ȯ�� �� ����");
            return;
        }
        else
        {
            // ���� ���� ���� �� �� ���� ����
            if (activatedWeaponScript != null) activatedWeaponScript = null;
            activatedWeaponScript = Instantiate(weaponScript);

            // �Ѿ� ������ ����
            //activatedWeaponScript.SetBullet(pool.bulletPrefab);
            //activatedWeaponScript.bulletScript = activatedWeaponScript.bullet.GetComponent<BulletCon>();
            //
            ////������ ����
            //activatedWeaponScript.bulletScript.ReflectDamage(activatedWeaponScript.damage);
            #region �ϵ� �ڵ��� ����,,,
            //switch (newWeaponState)
            //{
            //    case PlayerWeaponState.Pistol: 
            //        if(activatedWeaponScript.maxAmmo != maxPistolMag)
            //        {
            //            maxPistolAmmo = activatedWeaponScript.maxAmmo;
            //        }
            //       
            //        break;
            //
            //        case PlayerWeaponState.Magnum: 
            //        if(activatedWeaponScript.maxAmmo != maxMagnumMag)
            //        {
            //            maxMagnumMag = activatedWeaponScript.maxAmmo;
            //        }
            //        break;
            //
            //    case PlayerWeaponState.SMG:
            //        if(activatedWeaponScript.maxAmmo != maxPistolMag)
            //        {
            //            maxPistolMag = activatedWeaponScript.maxAmmo;
            //        }
            //        break;
            //
            //    case PlayerWeaponState.AR:
            //        if(activatedWeaponScript.maxAmmo != maxArMag)
            //        {
            //            maxArMag = activatedWeaponScript.maxAmmo;
            //        }
            //        break;
            //
            //    case PlayerWeaponState.Shotgun:
            //        if(activatedWeaponScript.maxAmmo != maxSgMag)
            //        {
            //            maxSgMag = activatedWeaponScript.maxAmmo;
            //        }
            //        break;
            //
            //    case PlayerWeaponState.Sniper:
            //        if(activatedWeaponScript.maxAmmo != maxSniperMag)
            //        {
            //            maxSniperMag = activatedWeaponScript.maxAmmo;
            //        }
            //        break;
            //
            //    case PlayerWeaponState.Granade:
            //        if(activatedWeaponScript.maxAmmo != maxGranade)
            //        {
            //            maxGranade = activatedWeaponScript.maxAmmo;
            //        }
            //        break;
            //
            //    case PlayerWeaponState.RPG: 
            //        if(activatedWeaponScript.maxAmmo != maxRPGAmmo)
            //        {
            //            maxRPGAmmo = activatedWeaponScript.maxAmmo;
            //        }
            //        break;
            //
            //    default: break;
            //}
            #endregion
        }




       
        if (weaponScriptDictionary.TryGetValue(newWeaponState.ToString(), out PlayerScriptableWeapon data))
        {
            currentWeaponState = newWeaponState;
            currentAmmo = ammoDict[newWeaponState];
            currentMag = magaineDict[newWeaponState];
        }
        //if (anim != null) anim = null;

        anim = activatedWeapon.GetComponent<Animator>();
        //currentWeaponState = newWeaponState; // ���� ���� ���� ������Ʈ
        
    }

    public void Fire1()
    {
        if (activatedWeapon == null || currentAmmo <= 0 || nowReloading == true) return;
        Debug.Log("���!");
        if(activatedWeaponScript.weaponType == WeaponType.FullAuto)
        {
            if (!fullAutoFiring)
            {
                fullAutoFiring = true;
                StartCoroutine(AutoFire());
            }
        }
        else
        {
            if (!semiAutoFiring)
            {
                semiAutoFiring = true;
                StartCoroutine(SemiAutoFire());
            }
        }

    }

    IEnumerator AutoFire()
    {
        while (Input.GetMouseButton(0) && currentAmmo > 0)
        {
            Shoot();
            yield return new WaitForSeconds(activatedWeaponScript.fireRate);
        }
        fullAutoFiring = false;
    }

    IEnumerator SemiAutoFire()
    {
        Shoot();
        yield return new WaitForSeconds(activatedWeaponScript.fireRate);
        semiAutoFiring = false;
    }

    void Shoot()
    {
        currentAmmo--;
        ammoDict[currentWeaponState] = currentAmmo;

        Vector3 fireDir = GetSpreadDir();

        activatedWeaponScript.bullet = pool.GetBullet();
        activatedWeaponScript.bullet.transform.position = muzzlePos.transform.position;
        activatedWeaponScript.bullet.transform.rotation = Quaternion.LookRotation(fireDir);
        activatedWeaponScript.bulletScript = activatedWeaponScript.bullet.GetComponent<BulletCon>();
        if (activatedWeaponScript.bulletScript.Damage != activatedWeaponScript.damage)
        {
            activatedWeaponScript.bulletScript.Damage = activatedWeaponScript.damage;
        }

        activatedWeaponScript.bulletScript.Initialize(pool, true);
        

        if (anim != null) anim.SetTrigger("Fire");
    }

    Vector3 GetSpreadDir()
    {
        float spreadAmout = activatedWeaponScript.acurracyMin;

        if(fullAutoFiring == true || semiAutoFiring == true)
        {
            spreadAmout += activatedWeaponScript.spreadIncreaseRate;
        }

        Vector3 dir = muzzlePos.transform.forward;

        dir += new Vector3(
            Random.Range(-spreadAmout, spreadAmout),
            Random.Range(-spreadAmout, spreadAmout));

        return dir.normalized;
    }

}
