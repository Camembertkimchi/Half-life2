using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float mouseSensativity;
    [SerializeField] int maxHp;
    [SerializeField] int currentHp;
    [SerializeField] int armor;
    [SerializeField] float speed;
    [SerializeField] float jumpPower;
    [SerializeField] bool dead = false;

    Rigidbody rigid;
    CapsuleCollider bodyCol;
    BoxCollider headCol;
    Transform camTr;
    float verticalRot;

    [SerializeField]List<ScriptableWeapon> weapon = new List<ScriptableWeapon>();
    GameObject currentWeapon;
    [SerializeField]int pistolAmmo;
    [SerializeField]int arAmmo;
    [SerializeField]int sgAmmo;
    [SerializeField]int grande;
    [SerializeField]int rpgAmmo;
    [SerializeField]int magnumAmmo;


    [SerializeField] Canvas playerUI;



    private void Start()
    {
        rigid = GetComponent<Rigidbody>();
        bodyCol = GetComponent<CapsuleCollider>();//몸뚱이 콜라이더
        headCol = GetComponent<BoxCollider>();//머리 콜라이더

        camTr = Camera.main.transform;//메인캠 달아주시고
        Cursor.lockState = CursorLockMode.Locked;//커서 고정
        
    }

    private void Update()
    {
        Move();
        LookAround();
        Jump();
       
       

        

        

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
        }
    }

    void Crouch()
    {
        if(Input.GetKeyDown(KeyCode.LeftControl))
        {

        }
    }

    void Attack1()
    {
        if(weapon != null)
        {
            
        }
    }


    public void ChangeHp(int damage)
    {

        currentHp += damage;
        if(currentHp <= 0)
        {
            dead = true;
            Dead();
        }
        if(currentHp > maxHp)
        {
            currentHp = maxHp;
        }
    }

    public void Dead()
    {
        if(dead == true)
        {

        }
    }
     
     

}
