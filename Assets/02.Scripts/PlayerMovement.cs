using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float mouseSensativity;
    [SerializeField] int maxHp;
    [SerializeField] int currentHp;
    [SerializeField] float speed;
    [SerializeField] float jumpPower;
    int jumpCount;
    [SerializeField] bool alive = true;
    public bool Alive
    {
        get { return alive; }
    }

    Rigidbody rigid;
    //CapsuleCollider bodyCol;
    //BoxCollider headCol; NoTime
    Transform camTr;
    float verticalRot;
    
    PlayerWeaponState currentState;
    [SerializeField] PlayerWeapon weapon;

    [SerializeField]float groundCheckDistance;
    bool isGrounded;
    //[SerializeField] LayerMask groundCheckMask;
    [SerializeField] Text playerHpUI;
    [SerializeField] RectTransform crosshair;
    [SerializeField] GameObject deadCanvus;


    private void Start()
    {
        rigid = GetComponent<Rigidbody>();
        
        

        camTr = Camera.main.transform;//메인캠 달아주시고
        Cursor.lockState = CursorLockMode.Locked;//커서 고정

        
        if(weapon == null)
        {
            weapon = GetComponentInChildren<PlayerWeapon>();
        }

        currentState = PlayerWeaponState.Pistol;
        weapon.EquipWeapon(currentState);

    }

    private void Update()
    {
        if(alive == true)
        {
            Move();
            LookAround();
            Jump();
            #region 구버전 무기 변경
            //if (Input.GetMouseButtonDown(0))
            //{
            //    weapon.Fire1();
            //}
            //if (Input.GetMouseButtonDown(1))
            //{
            //    weapon.Fire2();
            //}
            //if (Input.GetKeyDown(KeyCode.Alpha1))
            //{
            //    currentState = PlayerWeaponState.GravityGun;
            //    weapon.EquipWeapon(currentState);
            //}
            //if (Input.GetKeyDown(KeyCode.Alpha2))
            //{
            //    currentState = PlayerWeaponState.Pistol;
            //    weapon.EquipWeapon(currentState);
            //}
            //if (Input.GetKeyDown(KeyCode.Alpha3))
            //{
            //    currentState = PlayerWeaponState.Magnum;
            //    weapon.EquipWeapon(currentState);
            //}
            //
            //if (Input.GetKeyDown(KeyCode.Alpha4))
            //{
            //    currentState = PlayerWeaponState.SMG;
            //    weapon.EquipWeapon(currentState);
            //}
            //if (Input.GetKeyDown(KeyCode.Alpha5))
            //{
            //    currentState = PlayerWeaponState.AR;
            //    weapon.EquipWeapon(currentState);
            //}
            //if (Input.GetKeyDown(KeyCode.Alpha6))
            //{
            //    currentState = PlayerWeaponState.Shotgun;
            //    weapon.EquipWeapon(currentState);
            //}
            //if (Input.GetKeyDown(KeyCode.Alpha7))
            //{
            //    currentState = PlayerWeaponState.Sniper;
            //    weapon.EquipWeapon(currentState);
            //}
            //
            //if (Input.GetKeyDown(KeyCode.R))
            //{
            //    weapon.Reloading();
            //}
            //crosshair.position = Input.mousePosition;
            #endregion
            playerHpUI.text = currentHp + "";

            isGrounded = Physics.Raycast(transform.position, Vector3.down, groundCheckDistance);

            if (isGrounded)
            {
                jumpCount = 1; // 바닥에 닿으면 점프 횟수 초기화
            }
        }
        

    }

    

    void LookAround()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensativity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensativity;

        rigid.MoveRotation(rigid.rotation * Quaternion.Euler(0, mouseX, 0));

        verticalRot -= mouseY;
        verticalRot = Mathf.Clamp(verticalRot, -90, 90);
        camTr.localRotation = Quaternion.Euler(verticalRot, 0, 0);
    }

    void Move()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveZ = Input.GetAxisRaw("Vertical");

        Vector3 moveDir = transform.right * moveX + transform.forward * moveZ;
        Vector3 moveVelocity = moveDir.normalized * speed;

        //rigid.velocity = moveVelocity; 점프하고 움직이면 즉시 떨어지는 문제
        rigid.velocity = new Vector3(moveVelocity.x, rigid.velocity.y, moveVelocity.z);
    }

    void Jump()
    {
        if(Input.GetKeyDown(KeyCode.Space))
        {
            rigid.AddForce(Vector3.up * jumpPower, ForceMode.Impulse);
            jumpCount--;
        }
    }

    


    public void ChangeHp(int damage)
    {


        currentHp -= damage;
        if(currentHp <= 0)
        {
            currentHp = 0;
            alive = false;
            Dead();
        }
        if(currentHp > maxHp)
        {
            currentHp = maxHp;
        }
    }

    public void Dead()
    {
        if(alive == false)
        {
            if(!deadCanvus.activeSelf) deadCanvus.SetActive(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            weapon.gameObject.SetActive(false);
            crosshair.gameObject.SetActive(false);
        }
    }
     
     

}
