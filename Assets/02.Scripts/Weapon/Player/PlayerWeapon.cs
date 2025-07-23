using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
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
    [SerializeField]PlayerMovement player;
    [SerializeField]PlayerScriptableWeapon[] weaponScripts;
    [SerializeField]GameObject[] weaponPrefabs;
    Dictionary<string, GameObject> weaponDictionary = new Dictionary<string, GameObject>();
    [SerializeField]GameObject activatedWeapon = null;
    Dictionary<string, PlayerScriptableWeapon> weaponScriptDictionary = new Dictionary<string, PlayerScriptableWeapon>();
    [SerializeField]PlayerScriptableWeapon activatedWeaponScript;
    [SerializeField] PlayerWeaponState currentWeaponState;
    [SerializeField]Transform muzzlePos;
    [SerializeField] BulletPooling pool;
    [SerializeField] Text currentAmmoUI;
    [SerializeField] Text currentMagUI;
    [SerializeField] GameObject muzzleFlashObj;
    [SerializeField] ParticleSystem muzzleFlashFX;
    static readonly WaitForSeconds ReloadingTime = new WaitForSeconds(1.2f);
    IEnumerator currentCor;
   [SerializeField] bool nowReloading = false;
   [SerializeField] bool fullAutoFiring = false;
   [SerializeField] bool semiAutoFiring = false;
    #region 하드 코딩할 뻔
    //[SerializeField] int pistolAmmo; //이게 현재 총알
    //[SerializeField] int maxPistolAmmo;//이게 최대치
    //[SerializeField] int pistolMag;//이게 여분의 총알 
    //[SerializeField] int maxPistolMag;//가질 수 있는 여분 최대치
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

    //이 둘은 무적권 한발씩임
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
    PlayerWeaponState[] allWeaponStates;
    [SerializeField]int currentMag;
    Dictionary<PlayerWeaponState, int> ammoDict = new Dictionary<PlayerWeaponState, int>();
    Dictionary<AmmoType, int> magaineDict = new Dictionary<AmmoType, int>();
    private PlayerWeaponState reloadingWeaponState;
    private AmmoType reloadingAmmoType;
    float scroll;
    [Header("초당 정확도 감소와 회복 속도")]
    private float currentSpread = 0f; // 현재 탄 퍼짐 값
    private float lastShotTime = 0f;  // 마지막으로 발사한 시간

    #region 중력건
    [SerializeField] Camera playerCam;
    [SerializeField] Transform holdPos;
    [SerializeField] float grabDistance = 30f;
    [SerializeField] float throwForce = 300f;
    [SerializeField] LayerMask grabbableLayer;
    [SerializeField] Rigidbody grabbedObj;
    [SerializeField] bool isHolding = false;
    [SerializeField] float radius;//원 범위

    #endregion



    private void Start()
    {
        allWeaponStates = System.Enum.GetValues(typeof(PlayerWeaponState)).Cast<PlayerWeaponState>()
            .Where(s => s != PlayerWeaponState.CrowBar).OrderBy(s => (int)s).ToArray();
        foreach (var weapon in weaponScripts)
        {
            if (!weaponScriptDictionary.ContainsKey(weapon.name))
            {
                weaponScriptDictionary.Add(weapon.name, weapon);

            }
            else
            {
                return;
            }

            if (!ammoDict.ContainsKey(weapon.state))
            {
                ammoDict[weapon.state] = weapon.maxAmmo;
            }
            if (!magaineDict.ContainsKey(weapon.ammoType))
            {
                magaineDict[weapon.ammoType] = weapon.maxMag;
            }
        }
        foreach(var obj in weaponPrefabs)
        {
            string name = obj.name;
            if (!weaponDictionary.ContainsKey(name))
            {
                weaponDictionary.Add(name, obj);
                Debug.Log($"{name} 등록");
                obj.SetActive(false);
            }
            else
            {
                Debug.Log($"{name} 이미 등록됨");
            }
            if (weaponScripts.Length > 0)
            {
                EquipWeapon(weaponScripts[0].state);
            }
            else
            {
                Debug.LogError("PlayerWeapon 할당된 weaponScripts 없음");
            }
        }
        muzzleFlashFX = muzzleFlashObj.GetComponent<ParticleSystem>();
    }

    private void Update()
    {
        if (!player.Alive) return;
        scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0)
        {
            ChangeWeaponWithScroll(scroll);
        }
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + (i - 1)))
            {
                EquipWeaponWithNumber(i);
                break;
            }
        }
        if (Input.GetMouseButton(0))
        {
            Fire1();
        }
        if (Input.GetMouseButtonUp(0) && currentWeaponState != PlayerWeaponState.GravityGun)
        {
            fullAutoFiring = false;
        }
        if (Input.GetMouseButtonDown(1))
        {
            Fire2();
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            Reloading();
        }
    }
    private void FixedUpdate()
    {
        if (Time.time - lastShotTime > 0.1f && activatedWeaponScript != null) // 발사 후 0.1초 이상 쉬었을 때
        {
            if(currentSpread != activatedWeaponScript.acurracyMin)
            {
                currentSpread -= activatedWeaponScript.spreadRecoveryRate * Time.deltaTime;
                currentSpread = Mathf.Clamp(currentSpread, activatedWeaponScript.acurracyMin, activatedWeaponScript.acurracyMax);
            }
            
        }
        if (isHolding == true && grabbedObj != null)
        {
            MoveObj();
        }
        if(playerCam.fieldOfView != 60 && currentWeaponState != PlayerWeaponState.Sniper)
        {
            playerCam.fieldOfView = 60;
        }
        if (player.Alive == false)
        {
            enabled = false;
        }
        UpdateAmmo();
    }
    void ChangeWeaponWithScroll(float scrollAmount)
    {
        int currentIndex = (int)currentWeaponState;
        int nextIndex = currentIndex;
        int currentWeaponArrayIndex = -1;
        for (int i = 0; i < allWeaponStates.Length; i++)
        {
            if (allWeaponStates[i] == currentWeaponState)
            {
                currentWeaponArrayIndex = i;
                break;
            }
        }  

        if (currentWeaponArrayIndex == -1) // 현재 무기가 올바르게 설정되지 않았거나, 배열에 없음
        {
            Debug.LogWarning($"현재 무기 상태 ({currentWeaponState}) 찾을 수 없음.");
            return;
        }

        if (scrollAmount > 0) // 위로 스크롤 (다음 무기)
        {
            nextIndex = (currentWeaponArrayIndex + 1) % allWeaponStates.Length;
        }
        else if (scrollAmount < 0) // 아래로 스크롤 (이전 무기)
        {
            nextIndex = (currentWeaponArrayIndex - 1 + allWeaponStates.Length) % allWeaponStates.Length;
        }

        // 다음/이전 무기로 전환
        EquipWeapon(allWeaponStates[nextIndex]);
    }
    void EquipWeaponWithNumber(int numberKey)
    {
        // 배열은 0부터 시작하므로 numberKey-1 사용
        // 이 방식은 weaponScripts 배열의 순서가 숫자 키의 순서와 일치한다고 가정함
        if (numberKey > 0 && numberKey <= weaponScripts.Length)
        {
            PlayerWeaponState targetWeaponState = weaponScripts[numberKey - 1].state;
            if (currentWeaponState == targetWeaponState)
            {
                return;
            }
            EquipWeapon(targetWeaponState);
        }
        else
        {
            Debug.LogWarning($"{numberKey}에 해당하는 무기를 찾을 수 없음");
        }
    }
   // void EquipWeaponWithNumber(int numberKey)
   // {
   //     // Enum의 값이 숫자 키와 직접 매핑되어 있으면 이 코드 작동
   //     // 만약 Enum 값이 순차적이지 않거나, 특정 무기를 특정 숫자 키에 할당하고 싶다면
   //     // 여기에 더 명확한 Dictionary<int, PlayerWeaponState>을 써야함
   //     PlayerWeaponState targetState = (PlayerWeaponState)(1 << (numberKey - 1)); // 기존 PlayerWeaponState가 비트 플래그 방식일 경우
   //
   //     // 비트 플래그 방식이 아니라 Enum 값이 순서대로 1, 2, 3 같이 정의되었다면 아래를 사용
   //     // PlayerWeaponState targetState = (PlayerWeaponState)numberKey;
   //     if (numberKey > 0 && numberKey <= weaponScripts.Length)
   //     {
   //         // weaponScripts 배열은 0부터 시작하므로 numberKey-1 사용
   //         PlayerWeaponState targetWeapon = weaponScripts[numberKey - 1].state;
   //         EquipWeapon(targetWeapon);
   //     }
   //     else
   //     {
   //         Debug.LogWarning($"숫자 키 {numberKey} 비었음");
   //     }
   // }

        void UpdateAmmo()
    {
        if(currentWeaponState == PlayerWeaponState.GravityGun)
        {
            currentMagUI.text = "";
            currentAmmoUI.text = "";
            return;
        }
        currentAmmoUI.text = currentAmmo + "";
        currentMagUI.text = currentMag + "";
    }
 
    public void Reloading()
    {
        if (nowReloading || currentMag <= 0 || (activatedWeaponScript != null && currentAmmo >= activatedWeaponScript.maxAmmo))
        {
            return;
        }
        if (currentCor != null)
        {
            StopCoroutine(currentCor);
        }
        reloadingWeaponState = currentWeaponState;
        reloadingAmmoType = activatedWeaponScript.ammoType;

        currentCor = Reload();
        StartCoroutine(currentCor);
    }
    IEnumerator Reload()
    {
        nowReloading = true;
        yield return ReloadingTime;
        if (weaponScriptDictionary.TryGetValue(currentWeaponState.ToString(), out PlayerScriptableWeapon data))
        {
            int currentWeaponAmmoInMag = ammoDict[reloadingWeaponState]; // 현재 장전된 총알 수
            int currentWeaponMagCount = magaineDict[reloadingAmmoType]; // 해당 탄약 타입의 여분 탄창 수

            int needAmmo = data.maxAmmo - currentWeaponAmmoInMag; // 필요한 총알 수
            int ammoToReload = Mathf.Min(needAmmo, currentWeaponMagCount); // 실제로 재장전할 총알 수

            // 탄약 업데이트
            ammoDict[reloadingWeaponState] += ammoToReload;
            magaineDict[reloadingAmmoType] -= ammoToReload;

            // 현재 활성화된 무기의 UI 업데이트를 위해 currentAmmo와 currentMag도 업데이트
            currentAmmo = ammoDict[currentWeaponState];
            currentMag = magaineDict[activatedWeaponScript.ammoType];
        }
        nowReloading = false;
        currentCor = null;
        #region 하드 코딩의 흔적
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
        //            else//장전이 풀로 안되는 경우
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
        //        case PlayerWeaponState.Shotgun: //한발씩 장전하는 거 구현 좀 해주세요 감사합니다.
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
        if (nowReloading && currentCor != null)
        {
            StopCoroutine(currentCor);
            currentCor = null;
            nowReloading = false;
        }
        if (!weaponDictionary.TryGetValue(newWeaponState.ToString(), out GameObject weapon))
        {
            Debug.Log($"'{newWeaponState}' 없다 ㅇㅇ");
            return;
        }

        if (activatedWeapon != null && activatedWeapon != weapon)
        {
            activatedWeapon.SetActive(false);
        }
        weapon.SetActive(true);
        activatedWeapon = weapon;
        
        if (!weaponScriptDictionary.TryGetValue(newWeaponState.ToString(), out PlayerScriptableWeapon weaponScript))
        {
            Debug.Log($"{weaponScript}");
            Debug.Log($"{newWeaponState}의 스크립터블 오브젝트 안보인다 이름 확인 좀 ㅇㅇ");
            return;
        }
        activatedWeaponScript = weaponScript;
        // 현재 무기 상태 업데이트
        currentWeaponState = newWeaponState;

        // 딕셔너리에서 해당 무기의 현재 총알 수를 가져옴
        currentAmmo = ammoDict.ContainsKey(newWeaponState) ? ammoDict[newWeaponState] : 0;
        // 딕셔너리에서 해당 탄약 타입의 여분 탄창 수를 가져옴
        currentMag = magaineDict.ContainsKey(activatedWeaponScript.ammoType) ? magaineDict[activatedWeaponScript.ammoType] : 0;

        if (pool != null && pool.bulletPrefab != null)
        {
            activatedWeaponScript.SetBullet(pool.bulletPrefab);
        }
        else
        {
            Debug.LogWarning("BulletPooling or bulletPrefab이 없음");
        }
        if (currentWeaponState != PlayerWeaponState.GravityGun)
        {
            Transform newMuzzlePos = activatedWeapon.transform.Find("muzzle");
            if (newMuzzlePos != null)
            {
                muzzlePos = newMuzzlePos;
            }
            else
            {
                Debug.LogWarning($"{activatedWeapon.name}에 머즐이 없어요");
                muzzlePos = activatedWeapon.transform; // 임시로 무기 위치를 총구로 설정
            }
        }
        else
        {
            muzzlePos = null; // 중력건은 총구가 필요 없으므로 null로 설정
        }
    }

    public void Fire1()
    {
        if (currentWeaponState == PlayerWeaponState.GravityGun && grabbedObj != null)
        {
            ThrowObj();
        }

        if (activatedWeapon == null || currentAmmo <= 0 || nowReloading == true) return;
        
        
        if (activatedWeaponScript.weaponType == WeaponType.FullAuto || activatedWeaponScript.weaponType == WeaponType.Melee)
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
                Shoot();
                semiAutoFiring = true;
                StartCoroutine(SemiAutoFire());
            }
        }

    }

    public void Fire2()
    {
       
        switch (currentWeaponState)
        {
            case PlayerWeaponState.GravityGun:
                if (grabbedObj == null)
                {
                    Debug.Log("잡아볼게");
                    TryGrab();
                }
                else
                {
                    ReleaseObj();
                }
                break;
            case PlayerWeaponState.Sniper:
                if(playerCam.fieldOfView != 25)
                {
                    playerCam.fieldOfView = 25;
                }
                else
                {
                    playerCam.fieldOfView = 60;
                }
                
                break;
            default: break;
        }
       

    }


    IEnumerator AutoFire()
    {
        while (Input.GetMouseButton(0) && currentAmmo > 0 && !nowReloading)
        {
            Shoot();
            yield return new WaitForSeconds(activatedWeaponScript.fireRate);
        }
        fullAutoFiring = false;
    }

    IEnumerator SemiAutoFire()
    {
        yield return new WaitForSeconds(activatedWeaponScript.fireRate);
        semiAutoFiring = false;
    }

    void Shoot()
    {
        currentAmmo--;
        ammoDict[currentWeaponState] = currentAmmo;

        Vector3 fireDir = Vector3.zero;

        if (currentWeaponState == PlayerWeaponState.Shotgun)
        {
            for(int i = 0; i < 12; i++)
            {
                activatedWeaponScript.bullet = pool.GetBullet();
                activatedWeaponScript.bulletScript = activatedWeaponScript.bullet.GetComponent<BulletCon>();
                activatedWeaponScript.bullet.transform.position = muzzlePos.transform.position;

                if (activatedWeaponScript.bulletScript.Damage != activatedWeaponScript.damage)
                {
                    activatedWeaponScript.bulletScript.Damage = activatedWeaponScript.damage;
                }
                fireDir = GetSpreadDir();
                activatedWeaponScript.bullet.transform.rotation = Quaternion.LookRotation(fireDir);
                
                activatedWeaponScript.bulletScript.Initialize(pool, true);
            }
        }
        else
        {
            activatedWeaponScript.bullet = pool.GetBullet();
            activatedWeaponScript.bulletScript = activatedWeaponScript.bullet.GetComponent<BulletCon>();
            activatedWeaponScript.bullet.transform.position = muzzlePos.transform.position;

            if (activatedWeaponScript.bulletScript.Damage != activatedWeaponScript.damage)
            {
                activatedWeaponScript.bulletScript.Damage = activatedWeaponScript.damage;
            }
            fireDir = GetSpreadDir();
            activatedWeaponScript.bullet.transform.rotation = Quaternion.LookRotation(fireDir);

            activatedWeaponScript.bulletScript.Initialize(pool, true);
        }

        muzzleFlashObj.transform.position = muzzlePos.transform.position;
        muzzleFlashFX.Play();
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
            Random.Range(-spreadAmout, spreadAmout),
            0f);

        return dir.normalized;
    }

    void TryGrab()
    {
        Ray ray = playerCam.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * grabDistance, Color.red, 5f, false);
        if (Physics.SphereCast(ray, radius, out RaycastHit hit, grabDistance, grabbableLayer))
        {
            Rigidbody rb = hit.collider.GetComponent<Rigidbody>();
            if(rb != null)
            {
                grabbedObj = rb;
                grabbedObj.useGravity = false;
                grabbedObj.drag = 10;
                isHolding = true;
                
            }
            else
            {
                Debug.Log("못잡음");
            }
        }

    }

    void MoveObj()
    {
        if (grabbedObj == null) return;
        Vector3 targetPos = holdPos.position;
       
        if(grabbedObj.transform.position != targetPos)
        {
            grabbedObj.MovePosition(Vector3.Lerp(grabbedObj.position, targetPos, Time.deltaTime * 30f));
        }
       
    }

    void ReleaseObj()
    {
        if(grabbedObj != null) 
        {
            {
                grabbedObj.useGravity = true;
                grabbedObj.drag = 1;
                grabbedObj = null;
                isHolding = false;
            } 
        }
    }

    void ThrowObj()
    {
        if(grabbedObj != null)
        {
            grabbedObj.useGravity = true;
            grabbedObj.drag = 1;
            grabbedObj.AddForce(playerCam.transform.forward * throwForce);
            grabbedObj = null;
            isHolding = false;
        }
    }

}
