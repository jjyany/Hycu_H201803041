using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerMovement : MonoBehaviour
{
    private CharacterController characterController;    //캐릭터 컨트롤러를 불러옴
    private PlayerInput playerInput;                    //사용자의 입력을 감지하기 위한 Input을 불러옴
    private PlayerShooter playerShooter;                //Aim상태에서 시간이 지나 자동으로 Idle 상태로 전의되기 위함
    private Animator animator;

    private Camera followCam;                           //카메라의 방향을 기준으로 이동 따라서 카메라의 위치를 알기위함
    
    public float speed = 6f; //이동속도
    public float jumpVelocity = 20f; //점프힘
    [Range(0.01f, 1f)] public float airControlPercent; // 공중에서 이동조작을 위함

    //플레이어의 움직임 속도변화와 회전하는 속도를 자연스럽게 하기위한 지연시간
    public float speedSmoothTime = 0.1f;
    public float turnSmoothTime = 0.1f;
    
    //값의 변화와 변화속도
    private float speedSmoothVelocity;
    private float turnSmoothVelocity;
    
    private float currentVelocityY; //중력

    //외부에서 플레이어가 어떤 속도로 이동하는지 알려주는 프로퍼티
    public float currentSpeed => new Vector2(characterController.velocity.x, characterController.velocity.z).magnitude;
    
    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        playerInput = GetComponent<PlayerInput>();
        playerShooter = GetComponent<PlayerShooter>();
        animator = GetComponent<Animator>();

        followCam = Camera.main;

        Cursor.lockState = CursorLockMode.Locked;
    }

    private void FixedUpdate()
    {
        //플레이어가 걷고있거나 쏘고있거나 조준상태이거나 
        if (currentSpeed > 0.2f || playerInput.fire || playerShooter.aimState == PlayerShooter.AimState.HipFire)
        {
            //카메라가 강제로 플레이어가 바라보는 방향으로 회전
            Rotate();
        }

        Move(playerInput.moveInput);

        if (playerInput.jump)
        {
            Jump();
        }
    }

    private void Update()
    {
        UpdateAnimation(playerInput.moveInput);
    }

    /// <summary>
    /// 매개변수 moveInput을 전달받아 실제 움직이는 함수
    /// </summary>
    public void Move(Vector2 moveInput)
    {
        var targetSpeed = speed * moveInput.magnitude; //이동속도
        var moveDirection = Vector3.Normalize(transform.forward * moveInput.y + transform.right * moveInput.x); //움직일 방향을 나타냄

        var smoothTime = characterController.isGrounded ? speedSmoothTime : speedSmoothTime / airControlPercent;

        targetSpeed = Mathf.SmoothDamp(currentSpeed, targetSpeed, ref speedSmoothVelocity, smoothTime);

        currentVelocityY += Physics.gravity.y * Time.deltaTime;

        //최종속도 = (이동방향 * 속도 + (Y축방향 * (방향+속도))
        var velocity = moveDirection * targetSpeed + Vector3.up * currentVelocityY;

        //
        characterController.Move(velocity * Time.deltaTime);

        if(characterController.isGrounded)
        {
            //currentVelocityY값이 점점커지기 때문에 땅에 있을땐 항상 0으로 초기화
            currentVelocityY = 0f;
        }

    }

    /// <summary>
    /// 현재 플레이어가 바라보는 방향을 카메라가 바라보는 방향으로 정렬
    /// </summary>
    public void Rotate()
    {
        //Y값에 대한 회전만 사용한다.
        var targetRotation = followCam.transform.eulerAngles.y;

        targetRotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetRotation, ref turnSmoothVelocity, turnSmoothTime);

        transform.eulerAngles = Vector3.up * targetRotation;
    }

    public void Jump()
    {
        //바닥에 있는지 체크하여 아니라면 사용불가
        if(!characterController.isGrounded)
        {
            return;
        }

        currentVelocityY = jumpVelocity;


    }

    /// <summary>
    /// 사용자 입력을 받아 입력에 맞게 애니메이션을 갱신
    /// </summary>
    private void UpdateAnimation(Vector2 moveInput)
    {
        var animationSpeedPercent = currentSpeed * speed;

        animator.SetFloat("Vertical Move", moveInput.y * animationSpeedPercent, 0.05f, Time.deltaTime);
        animator.SetFloat("Horizontal Move", moveInput.x * animationSpeedPercent, 0.05f, Time.deltaTime);
    }
}