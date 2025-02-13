using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Pistol : MonoBehaviour , IWeapon
{
    ScriptableWeapon weapon;
    [SerializeField] Transform muzzle;

    int maxAmmo;
    int currnetAmmo;
    int maxMagagineAmmo;
    int currentmagagineAmmo;


    private void OnEnable()
    {
        weapon = GetComponent<ScriptableWeapon>();
        //ReturnWeaopnSCO();
    }
    //private ScriptableWeapon ReturnWeaopnSCO() => weapon;

    public void Attack()
    {
        Instantiate(weapon.bullet, muzzle.position, Quaternion.identity);
    }
}
