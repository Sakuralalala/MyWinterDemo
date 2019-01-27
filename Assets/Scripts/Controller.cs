using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RootMotion.FinalIK;
using Cinemachine;
using System;
public class Controller : MonoBehaviour
{
    /// <summary>
    /// 记录鼠标位置
    /// </summary>
    private RaycastHit raycastHit;
    private Vector3 raycastHitPoint;
    [Header("射线检测碰撞层级")][SerializeField]
    private LayerMask inputRayLayerMask;

    [Header("人物移动加速度")]
    public float moveAddSpeed = 1;
    [Header("人物移动速度")]
    public float moveSpeed = 0;
    [Header("人物转身角速度")]
    public float turnSpeed = 45;
    [Header("动画增加参数")]
    public float addSpeed = 0.5f;
    [Header("是否瞄准")]
    public bool isAim = false;
    [Header("瞄准位置")]
    public Transform aimPos;
    [Header("枪械")]
    public GameObject gun;
    [Header("开火口")]
    public Transform firePos;
    /// <summary>
    /// 动画归零速度参数
    /// </summary>
    float slowSpeed = 7f;
    float speed = 0;
    Transform lookAT;
    
    LineRenderer lineRenderer;
    GameObject cam;
    

    public Animator anim;
    FullBodyBipedIK ik;
    CinemachineFreeLook moveCamera;
    CinemachineFreeLook aimCamera;

    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<Animator>();
        cam = GameObject.Find("Main Camera");
        lineRenderer = GetComponent<LineRenderer>();
        moveCamera = GameObject.Find("MoveCamera").GetComponent<CinemachineFreeLook>();
        aimCamera = GameObject.Find("AimCamera").GetComponent<CinemachineFreeLook>();
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
        Shoot();
        Debug.Log(cam.transform.GetChild(0).transform.position);
    }

    //人物移动
    private void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
       

        //移动时转向视线方向
        if (v != 0 && isAim == false)
        {
            //归一化向量
            Vector3 camView = ((lookAT.position + new Vector3(0, cam.transform.position.y - lookAT.position.y, 0)) - cam.transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, camView);
            
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camView), Time.deltaTime * 20);
        }

        //移动
        //移动速度的计算以及动画传递参数的计算
        if (Input.GetKey(KeyCode.W))
        {
            Mathf.Clamp(speed, 0, 3);
            speed = speed + addSpeed * Time.deltaTime * 5;
            speed = Mathf.Log((speed + 1), 2);
            moveSpeed = moveSpeed + moveAddSpeed * Mathf.Pow(Time.deltaTime * 10, 2);
            moveSpeed = (1 - Convert.ToInt32(isAim)) * Mathf.Clamp(moveSpeed, 0, 5) + Convert.ToInt32(isAim) * 2.0f;
            
        }
        else if (Input.GetKey(KeyCode.S))
        {
            speed = Mathf.Lerp(speed, -1, 0.6f);
            moveSpeed = moveSpeed + moveAddSpeed * Mathf.Pow(Time.deltaTime * 10, 2);
            moveSpeed = (1 - Convert.ToInt32(isAim)) * Mathf.Clamp(moveSpeed, 0, 5) + Convert.ToInt32(isAim) * 2.0f;
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

        //人物移动的计算
        if (isAim)
        {
            //相机forward向量修正角度
            float angle = transform.eulerAngles.x - cam.transform.eulerAngles.x;
            Vector3 camNewForward = Quaternion.AngleAxis(angle, cam.transform.right.normalized) * cam.transform.forward.normalized;
            //cam.transform.forward = camNewForward;
            transform.position += (camNewForward * v * moveSpeed * Time.deltaTime);
        }
        else
        {
            
            transform.position += (transform.forward * v * moveSpeed * Time.deltaTime);
        }
        
        //播放移动动画
        anim.SetFloat("Speed", speed);

        //转向
        transform.Rotate(transform.up * turnSpeed * Time.deltaTime * h);
        anim.SetFloat("TurnValue", h);
    }

    public void Aim()
    {
        //记录下相机切换前的相机方向
        Vector3 oldCam = cam.transform.forward;
        
        if (Input.GetMouseButton(1))
        {
            isAim = true;
            //相机切换
            moveCamera.Priority = 10;
            aimCamera.Priority = 11;
            //切换后的方向指向切换前的方向
            //cam.transform.rotation = Quaternion.LookRotation(oldCam);

            //瞄准时人物转向
            Vector3 camView = ((lookAT.position + new Vector3(0, cam.transform.position.y - lookAT.position.y, 0)) - cam.transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(camView), Time.deltaTime * 20);

            //参数设置
            anim.SetBool("IsAim", true);
            ik.solver.rightHandEffector.positionWeight = 1.0f;
            ik.solver.leftHandEffector.positionWeight = 1.0f;
            gun.SetActive(true);
            GetComponent<AimIK>().enabled = true;
            GetComponent<AimIK>().solver.target = aimPos;
        }
        else
        {
            moveCamera.Priority = 11;
            aimCamera.Priority = 10;

            isAim = false;
            anim.SetBool("IsAim", false);
            ik.solver.rightHandEffector.positionWeight = 0.0f;
            ik.solver.leftHandEffector.positionWeight = 0.0f;
            gun.SetActive(false);
            GetComponent<AimIK>().enabled = false;
        }
    }

    public void Shoot()
    {
        
        if (isAim)
        {
            if (Input.GetMouseButtonDown(0))
            {
               
                lineRenderer.SetPosition(0, firePos.position);
                lineRenderer.SetPosition(1, aimPos.position);
            }
        }
    }
   
}
