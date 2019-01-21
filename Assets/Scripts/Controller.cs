using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
public class Controller : MonoBehaviour
{
    [Header("人物移动加速度")]
    public float moveAddSpeed = 1;
    [Header("人物移动速度")]
    public float moveSpeed = 0;
    [Header("人物转身角速度")]
    public float turnSpeed = 45;
    [Header("动画增加参数")]
    public float addSpeed = 0.5f;
    [Header("是否瞄准")]
    public bool isGun = false;
    [Header("瞄准位置")]
    public Transform aimPos;
    [Header("枪械")]
    public GameObject gun;
    /// <summary>
    /// 动画归零速度参数
    /// </summary>
    float slowSpeed = 7f;
    float speed = 0;
    Transform lookAT;

    GameObject cam;

    public Animator anim;
    FullBodyBipedIK ik;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        cam = GameObject.Find("Main Camera");
        lookAT = transform.parent.Find("LookAT").transform;
        ik = GetComponent<FullBodyBipedIK>();
        gun.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        lookAT.position = transform.position;
        Move();
        Aim();
        
        //Debug.Log(cam.transform.forward);
    }

    //人物移动
    private void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
       

        //移动时转向视线方向
        if (v != 0)
        {

            //Debug.DrawLine(cam.transform.position, lookAT.position + new Vector3(0, cam.transform.position.y, 0));
            //归一化向量
            Vector3 camView = ((lookAT.position + new Vector3(0, cam.transform.position.y - lookAT.position.y, 0)) - cam.transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, camView);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camView), Time.deltaTime * 20);
        }

        //移动
        if (Input.GetKey(KeyCode.W))
        {
            Mathf.Clamp(speed, 0, 3);
            speed = speed + addSpeed * Time.deltaTime * 5;
            speed = Mathf.Log((speed + 1), 2);
            moveSpeed = moveSpeed + moveAddSpeed * Mathf.Pow(Time.deltaTime * 10, 2);
            moveSpeed = Mathf.Clamp(moveSpeed, 0, 5);
        }
        else if (Input.GetKey(KeyCode.S))
        {
            speed = Mathf.Lerp(speed, -1, 0.6f);
            moveSpeed = moveSpeed + moveAddSpeed * Mathf.Pow(Time.deltaTime * 10, 2);
        }
        else
        {
            //当没有按下移动键的时候，动画传递参数减小到0，维持到Idle状态
            if (speed > 0)
            {
                speed -= slowSpeed * Time.deltaTime * 5;
                speed = Mathf.Clamp(speed, 0, 1);
            }
            if (speed < 0)
            {
                speed += slowSpeed * Time.deltaTime * 3;
                speed = Mathf.Clamp(speed, -1f, 0);
            }
            moveSpeed = 0;
        }
        //moveSpeed = moveSpeed + moveAddSpeed * Mathf.Pow(Time.deltaTime, 2);
        transform.position += transform.forward * v * moveSpeed * Time.deltaTime;
        //播放移动动画
        anim.SetFloat("Speed", speed);

        //转向
        transform.Rotate(transform.up * turnSpeed * Time.deltaTime * h);
        anim.SetFloat("TurnValue", h);
    }

    public void Aim()
    {
        //瞄准准星
        aimPos.position = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        //aimPos.position += new Vector3(0, 0, transform.position.z + 10);
        //Ray ray = Camera.main.ScreenPointToRay(aimPos.position);
        
        //aimPos.position = Camera.main.ScreenToWorldPoint(aimPos.position);
        //aimPos.position = new Vector3(aimPos.position.x, aimPos.position.y, transform.position.z + 5);
       
        

        if (Input.GetMouseButton(1))
        {
            anim.SetBool("IsAim", true);
            ik.solver.rightHandEffector.positionWeight = 1.0f;
            ik.solver.leftHandEffector.positionWeight = 1.0f;
            gun.SetActive(true);
            GetComponent<AimIK>().enabled = true;
            GetComponent<AimIK>().solver.target = aimPos;
        }
        else
        {
            //anim.SetBool("IsAim", false);
            //ik.solver.rightHandEffector.positionWeight = 0.0f;
            //ik.solver.leftHandEffector.positionWeight =  0.0f;
        }
    }
}
