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

    PlayerScriptableWeapon[] weaponScripts;
    GameObject[] weaponPrefabs;
    Dictionary<string, GameObject> weaponDictionary = new Dictionary<string, GameObject>();
    GameObject activatedWeapon = null;
    Dictionary<string, PlayerScriptableWeapon> weaponScriptDictionary = new Dictionary<string, PlayerScriptableWeapon>();
    PlayerScriptableWeapon activatedWeaponScript;
    PlayerWeaponState currentWeaponState;
    Vector3 muzzlePos;
    [SerializeField] int damage;
    [SerializeField] BulletPooling pool;

    [SerializeField] GameObject weaponStatusUI;
    [SerializeField] Animator anim;

    static readonly WaitForSeconds ReloadingTime = new WaitForSeconds(1.2f);
    IEnumerator currentCor;
    bool nowReloading = false;

    [SerializeField] int pistolAmmo; //이게 현재 총알
    [SerializeField] int maxPistolAmmo;//이게 최대치
    [SerializeField] int pistolMag;//이게 여분의 총알 
    [SerializeField] int maxPistolMag;//가질 수 있는 여분 최대치

    [SerializeField] int smgAmmo;
    [SerializeField] int maxSmgAmmo;

    [SerializeField] int arAmmo;
    [SerializeField] int maxArAmmo;
    [SerializeField] int arMag;
    [SerializeField] int maxArMag;

    [SerializeField] int sgAmmo;
    [SerializeField] int maxSgAmmo;
    [SerializeField] int sgMag;
    [SerializeField] int maxSgMag;

    [SerializeField] int sniperAmmo;
    [SerializeField] int maxSniperAmmo;
    [SerializeField] int sniperMag;
    [SerializeField] int maxSniperMag;

    int currentAmmo;
    public int CurrentAmmo
    {
        get { return currentAmmo; }
    }
    int maxAmmo;


    //이 둘은 무적권 한발씩임
    [SerializeField] int grande;
    [SerializeField] int maxGranade;
    [SerializeField] int rpgAmmo;
    [SerializeField] int maxRPGAmmo;
   
    [SerializeField] int magnumAmmo;
    [SerializeField] int maxMagnumAmmo;
    [SerializeField] int magnumMag;
    [SerializeField] int maxMagnumMag;

    [SerializeField] int granadeForSmg;
    [SerializeField] int coreForAR;//얘는 만들지 말지 모르겠음 ㅇㅇ 

    [SerializeField] GameObject ScopeUI;

    [Header("초당 정확도 감소와 회복 속도")]
    [SerializeField] private float spreadIncreaseRate = 0.05f; // 초당 정확도 감소 속도
    [SerializeField] private float spreadRecoveryRate = 0.1f; // 초당 정확도 회복 속도

    private float currentSpread = 0f; // 현재 탄 퍼짐 값
    private float lastShotTime = 0f;  // 마지막으로 발사한 시간



    private void Update()
    {
        if (Time.time - lastShotTime > 0.1f && activatedWeaponScript != null) // 발사 후 0.1초 이상 쉬었을 때
        {
            if(currentSpread != activatedWeaponScript.acurracyMin)
            {
                currentSpread -= spreadRecoveryRate * Time.deltaTime;
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


        #region 하드 코딩의 흔적
        switch (currentWeaponState)
        {
            
            case PlayerWeaponState.Pistol:
                if(pistolMag > 0 && pistolAmmo != maxPistolAmmo)
                {
                    pistolMag -= maxPistolAmmo;
                    if (0 <= pistolMag)
                    {
                        pistolAmmo = maxPistolAmmo;

                    }
                    else//장전이 풀로 안되는 경우
                    {
                        pistolAmmo = pistolMag + maxPistolAmmo;
                        if (pistolMag < 0)
                        {
                            pistolMag = 0;
                        }
                    }
                }
               
                break;
        
                case PlayerWeaponState.Magnum:
                if(magnumMag > 0 &&  magnumAmmo != maxMagnumAmmo)
                {
                    magnumMag -= maxMagnumAmmo;

                    if (0 <= magnumMag)
                    {
                        magnumAmmo = maxMagnumAmmo;

                    }
                    else
                    {
                        magnumAmmo = magnumMag + maxMagnumAmmo;
                        if (magnumMag < 0)
                        {
                            magnumMag = 0;
                        }
                    }
                }
               
                break;
            case PlayerWeaponState.SMG:
                if(pistolMag > 0 && smgAmmo != maxSmgAmmo)
                {
                    pistolMag -= maxSmgAmmo;

                    if (0 <= pistolMag)
                    {
                        smgAmmo = maxSmgAmmo;
                    }
                    else
                    {
                        smgAmmo = pistolMag + maxSmgAmmo;
                        if (pistolMag < 0)
                        {
                            pistolMag = 0;
                        }
                    }
                }
                
                break;
            case PlayerWeaponState.AR:

                if(arMag > 0 && arAmmo != maxArAmmo)
                {
                    arMag -= x;
                    if (x >= arMag)
                    {
                        arAmmo = maxArAmmo;

                    }
                    else
                    {
                        arAmmo = x;
                        if (arMag < 0)
                        {
                            arMag = 0;
                        }
                    }
                }
                
                break;
                case PlayerWeaponState.Shotgun: //한발씩 장전하는 거 구현 좀 해주세요 감사합니다.
                while(sgMag > 0 && sgAmmo >= maxSgAmmo)
                {
                    sgAmmo++;
                    sgMag--;
                }
                break;
            case PlayerWeaponState.Sniper:
                x = sniperMag - maxSniperAmmo;
                sniperMag -= x;
                if (x >= sniperMag)
                {
                    sniperAmmo = maxSniperAmmo;
                    
                }
                else
                {
                    pistolAmmo = x;
                    if (pistolMag < 0)
                    {
                        pistolMag = 0;
                    }
                }
                break;
            case PlayerWeaponState.Granade:
                x = pistolMag - maxPistolAmmo;
                if (x >= pistolAmmo)
                {
                    pistolAmmo = maxPistolAmmo;
                }
                else
                {
                    pistolAmmo = x;
                    if (pistolMag < 0)
                    {
                        pistolMag = 0;
                    }
                }
                break;
            case PlayerWeaponState.RPG:
                x = pistolMag - maxPistolAmmo;
                if (x >= pistolAmmo)
                {
                    pistolAmmo = maxPistolAmmo;
                }
                else
                {
                    pistolAmmo = x;
                    if (pistolMag < 0)
                    {
                        pistolMag = 0;
                    }
                }
                break;
        }
        #endregion


    }



    public void ActivateWeapon(PlayerWeaponState newWeaponState)
    {
        if (!weaponDictionary.TryGetValue(newWeaponState.ToString(), out GameObject weapon))
        {
            Debug.Log($"'{newWeaponState}' 없다 ㅇㅇ");
            return;
        }

        if (activatedWeapon != null && weapon != activatedWeapon)//무기가 동일하지 않으면
        {
            activatedWeapon.SetActive(false); // 이전 무기 비활성화
        }

        if(!weaponScriptDictionary.TryGetValue(newWeaponState.ToString(), out PlayerScriptableWeapon weaponScript))
        {
            Debug.Log($"{newWeaponState}의 스크립터블 오브젝트 안보인다 이름 확인 좀 ㅇㅇ");
            return;
        }
        else
        {
            if (activatedWeaponScript != null)
            {
                activatedWeaponScript = null;
            }
            activatedWeaponScript = Instantiate(weaponScript); // 복사본 안쓰면 골 때리게도 총알을 못가져옴,,,
            activatedWeaponScript.SetBullet(pool.bulletPrefab);
            activatedWeaponScript.bulletCon = activatedWeaponScript.bullet.GetComponent<BulletCon>();
            activatedWeaponScript.bulletCon.ReflectDamage(activatedWeaponScript.damage);
            #region 최대 탄창 값 초기화
            switch (newWeaponState)
            {
                case PlayerWeaponState.Pistol: 
                    if(activatedWeaponScript.maxAmmo != maxPistolMag)
                    {
                        maxPistolAmmo = activatedWeaponScript.maxAmmo;
                    }
                   
                    break;

                    case PlayerWeaponState.Magnum: 
                    if(activatedWeaponScript.maxAmmo != maxMagnumMag)
                    {
                        maxMagnumMag = activatedWeaponScript.maxAmmo;
                    }
                    break;

                case PlayerWeaponState.SMG:
                    if(activatedWeaponScript.maxAmmo != maxPistolMag)
                    {
                        maxPistolMag = activatedWeaponScript.maxAmmo;
                    }
                    break;

                case PlayerWeaponState.AR:
                    if(activatedWeaponScript.maxAmmo != maxArMag)
                    {
                        maxArMag = activatedWeaponScript.maxAmmo;
                    }
                    break;

                case PlayerWeaponState.Shotgun:
                    if(activatedWeaponScript.maxAmmo != maxSgMag)
                    {
                        maxSgMag = activatedWeaponScript.maxAmmo;
                    }
                    break;

                case PlayerWeaponState.Sniper:
                    if(activatedWeaponScript.maxAmmo != maxSniperMag)
                    {
                        maxSniperMag = activatedWeaponScript.maxAmmo;
                    }
                    break;

                case PlayerWeaponState.Granade:
                    if(activatedWeaponScript.maxAmmo != maxGranade)
                    {
                        maxGranade = activatedWeaponScript.maxAmmo;
                    }
                    break;

                case PlayerWeaponState.RPG: 
                    if(activatedWeaponScript.maxAmmo != maxRPGAmmo)
                    {
                        maxRPGAmmo = activatedWeaponScript.maxAmmo;
                    }
                    break;

                default: break;
            }
            #endregion
        }




        weapon.SetActive(true); // 새 무기 활성화
        activatedWeapon = weapon;
        if(anim != null) anim = null;

        anim = activatedWeapon.GetComponent<Animator>();
        currentWeaponState = newWeaponState; // 현재 무기 상태 업데이트
    }


    


}
