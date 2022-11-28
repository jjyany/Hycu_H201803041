using UnityEngine;
using UnityEngine.AI;

public class PlayerShooter : MonoBehaviour
{
    public enum AimState
    {
        Idle,
        HipFire
    }

    public AimState aimState { get; private set; }

    public Gun gun;                     //사용할 Gun컴포넌트
    public LayerMask excludeTarget;     //조준에서 제외할 레이어마스크
    
    private PlayerInput playerInput;    //움직임을 입력을 전달하는 컨포넌트
    private Animator playerAnimator;    //애니메이션
    private Camera playerCamera;        //현재 메인 카메라

    private float waitingTimeForReleasingAim = 2.5f; // HipFire 상태에서 입력이 없다면 다시 Idle로 돌아가는 시간
    private float lastFireInputTime;  //마지막 발사한 시간

    //실제로 조준하고있는 대상(FPS(1인칭)은 필요없음(정중앙을 조준하기 때문)
    //TPS 게임의 경우 2가지의 조준점을 사용한다
    //1. 카메라의 정조준 조준점
    //2. 플레이어의 위치에서 실제로 탄알이 발사되는 조준점
    private Vector3 aimPoint; //2. 조준점

    //플레이어가 바라보는 방향과 카메라의 방향이 일치하지 않으면 (!) 총을 발사하지않고 플레이어를 회전시킨다.
    private bool linedUp => !(Mathf.Abs( playerCamera.transform.eulerAngles.y - transform.eulerAngles.y) > 1f);
    private bool hasEnoughDistance => !Physics.Linecast(transform.position + Vector3.up * gun.fireTransform.position.y,gun.fireTransform.position, ~excludeTarget);
    
    void Awake()
    {
        //플레이어 레이어를 추가하기 위한 비트연산자
        if (excludeTarget != (excludeTarget | (1 << gameObject.layer)))
        {
            //레이어가 추가되지않았을 경우를 대비하여 게임오브젝트의 레이어를 excludeTarget Layer로 추가한다.
            excludeTarget |= 1 << gameObject.layer;
        }
    }

    private void Start()
    {
        playerCamera = Camera.main;
        playerInput = GetComponent<PlayerInput>();
        playerAnimator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        aimState = AimState.Idle;
        //플레이어가 활성화되면 Gun오브젝트 활성화
        gun.gameObject.SetActive(true);
        //etUp함수에 플레이어오브젝트를 초기화
        gun.Setup(this);
    }

    private void OnDisable()
    {
        aimState = AimState.Idle;
        //플레이어가 비활성화되면 Gun오브젝트도 비활성화
        gun.gameObject.SetActive(false);
    }

    /// <summary>
    /// 고정 프레임
    /// </summary>
    private void FixedUpdate()
    {
        if (playerInput.fire) //fire입력에 따라 Shoot() 실행
        {
            lastFireInputTime = Time.time; //마지막 발사시간을 현재시간으로 설정
            Shoot();
        }
        else if (playerInput.reload) //reload입력에 따라 Reload() 실행
        {
            Reload();
        }
    }

    private void Update()
    {
        UpdateAimTarget();

        //플레이어가 하늘과 바닥을 바라볼때의 회전(x축) 값
        var angle = playerCamera.transform.eulerAngles.x;

        //270도와 -90도는 같은 방향이기에 방지하기위함
        if(angle > 270f)
        {
            angle -= 360f;
        }

        //정면이 0도(0.5) 아래를 볼때 90도(0.0) 하늘을 볼때 -90도(1.0)
        angle = (angle / -180f) + 0.5f;
        //위 각도값에 따라 애니메이션을 재생한다.
        playerAnimator.SetFloat("Angle", angle);

        //입력이 없거나 현재시간이 (마지막 발사시간 + 대기시간)보다 크다면
        if(!playerInput.fire && Time.time >= lastFireInputTime + waitingTimeForReleasingAim)
        {
            //다시 aim상태를 Idle로 변경
            aimState = AimState.Idle;
        }

        //UIManager
        UpdateUI();
    }

    public void Shoot()
    {
        if(aimState == AimState.Idle)
        {
            if(linedUp)
            {
                aimState = AimState.HipFire;
            }
        }
        else if (aimState == AimState.HipFire)
        {
            if (hasEnoughDistance)
            {
                if (gun.Fire(aimPoint))
                {
                    playerAnimator.SetTrigger("Shoot");
                }
            }
            else
            {
                aimState = AimState.Idle;
            }
        }
    }

    public void Reload()
    {
        if(gun.Reload())
        {
            playerAnimator.SetTrigger("Reload");
        }
    }

    /// <summary>
    /// aimPoint의 값을 플레이어가 조준하는 값으로 갱신
    /// 1차적으로 카메라중앙을 AimTarget을 지정
    /// 2차적으로 실제 플레이어에서 조준되는 AimTarget
    /// </summary>
    private void UpdateAimTarget()
    {
        RaycastHit hit;


        //카메라 정중앙으로 생성되는 광선을 ray에 저장
        var ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 1f));

        //1차적으로 Raycast 생성
        if(Physics.Raycast(ray, out hit, gun.fireDistance, ~excludeTarget))
        {
            //충돌된 오브젝트가 있다면
            aimPoint = hit.point;

            //2차적으로 카메라에 충돌된 오브젝트와 플레이어의 총구사이에 오브젝트가 있는지 확인
            if (Physics.Linecast(gun.fireTransform.position, hit.point, out hit, ~excludeTarget))
            {
                aimPoint = hit.point;
            }
        }
        else //충돌되는게 없다면
        {
            //조준점은 카메라 중심에서 앞쪽으로 최대사정거리만큼
            aimPoint = playerCamera.transform.position + playerCamera.transform.forward * gun.fireDistance;
        }
 
    }

    private void UpdateUI()
    {
        //UIManager가 존재하지 않는다면 해당 함수는 실행하지 않는다.
        if (gun == null || UIManager.Instance == null)
        {
            return;
        }
        
        UIManager.Instance.UpdateAmmoText(gun.magAmmo, gun.ammoRemain);
        
        UIManager.Instance.SetActiveCrosshair(hasEnoughDistance);
        UIManager.Instance.UpdateCrossHairPosition(aimPoint);
    }

    private void OnAnimatorIK(int layerIndex)
    {
        //총이 없거나 재장전 중일때 IK를 Override 하지 않는다.
        if(gun == null || gun.state == Gun.State.Reloading)
        {
            return;
        }

        //총을 들고있다면 왼손의 위치와 회전값을 오버라이딩한다.
        playerAnimator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 1.0f);
        playerAnimator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 1.0f);

        //오버라이딩 된 IK아바타를 게임월드에 실제 적용한다.
        playerAnimator.SetIKPosition(AvatarIKGoal.LeftHand, gun.leftHandMount.position);
        playerAnimator.SetIKRotation(AvatarIKGoal.LeftHand, gun.leftHandMount.rotation);
    }
}