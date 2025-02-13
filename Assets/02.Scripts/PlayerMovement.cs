using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float mouseSensativity;
    [SerializeField] int hp;
    [SerializeField] int armor;
    [SerializeField] float speed;
    [SerializeField] float jumpPower;

    Rigidbody rigid;
    CapsuleCollider bodyCol;
    BoxCollider headCol;
    Transform camTr;
    float verticalRot;

    [SerializeField]List<IWeapon> weapon = new List<IWeapon>();
    [SerializeField]int pistolAmmo;
    [SerializeField]int arAmmo;
    [SerializeField]int sgAmmo;
    [SerializeField]int grande;
    [SerializeField]int rpgAmmo;
    [SerializeField]int magnumAmmo;



    private void Start()
    {
        rigid = GetComponent<Rigidbody>();
        bodyCol = GetComponent<CapsuleCollider>();//������ �ݶ��̴�
        headCol = GetComponent<BoxCollider>();//�Ӹ� �ݶ��̴�

        camTr = Camera.main.transform;//����ķ �޾��ֽð�
        Cursor.lockState = CursorLockMode.Locked;//Ŀ�� ����
        
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

        //rigid.velocity = moveVelocity; �����ϰ� �����̸� ��� �������� ����
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
     
     

}
