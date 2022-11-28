using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; //Navigation을 불러오기 위한 네임스페이스

#if UNITY_EDITOR
//최종빌드에는 실행되지 않는다
using UnityEditor; //Enemy가 Player를 인식하기 위한 표식(Unity 내부기능)
#endif

/// <summary>
/// Enemy 상태를 관리하는 스크립트
/// </summary>
public class Enemy : LivingEntity
{
    /// <summary>
    /// Enemy의 상태
    /// </summary>
    private enum State
    {
        Patrol,         //순찰
        Tracking,       //추격
        AttackBegin,    //공격시작
        Attacking       //공격중
    }
    
    private State state;
    
    private NavMeshAgent agent; 
    private Animator animator;

    public Transform attackRoot;    //공격을 할때의 반경
    public Transform eyeTransform;  //좀비가 적을 감지할 수 있는 시야의 기준점
    
    private AudioSource audioPlayer;
    public AudioClip hitClip;       //공격음향
    public AudioClip deathClip;     //사망음향
    
    private Renderer skinRenderer;  //공격력에 따른 피부색 변경

    public float runSpeed = 10f;    //이동속도
    [Range(0.01f, 2f)] public float turnSmoothTime = 0.1f; //방향전환시 지연시간(클수록 늦게 회전)
    private float turnSmoothVelocity;   //현재 회전의 실시간 변화량
    
    public float damage = 30f;      //공격력
    public float attackRadius = 2f; //공격의 반경
    private float attackDistance;   //공격을 시도하는 거리
    
    public float fieldOfView = 50f;     //각도값
    public float viewDistance = 10f;    //식별 거리
    public float patrolSpeed = 3f;      //순찰상태일때 속도
    
    [HideInInspector]public LivingEntity targetEntity; //Enemy가 추격할 대상(플레이어)
    public LayerMask whatIsTarget;    //적을 감시할때 사용할 필터(레이어검사를 통해 Player만 추격)

    //범위기반의 RaycastHit 공격
    //여러개의 충돌포인트
    private RaycastHit[] hits = new RaycastHit[10];
    //??
    private List<LivingEntity> lastAttackedTargets = new List<LivingEntity>();
    
    //Target이 Null이 아니고, 죽지않은 상태일때 hastarget
    private bool hasTarget => targetEntity != null && !targetEntity.dead;
    

#if UNITY_EDITOR

    /// <summary>
    /// Editor를 이용한 Unity 내부실행 함수
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if(attackRoot != null) //공격범위
        {
            //Gizmos = Scene창에서만 보여지는 도구들
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
            //원형모양의 Gizmos(원형의 중심위치, 원형의 반지름) 
            Gizmos.DrawSphere(attackRoot.position, attackRadius); //공격범위
        }

        if (eyeTransform != null)
        {
            //Arc회전반경(각도 * 반시계방향(절반값(중심))으로 회전, 회전축)
            var leftEyeRotation = Quaternion.AngleAxis(fieldOfView * -0.5f, Vector3.up);
            //시야방향(회전값 * 앞방향)
            var leftRayDirection = leftEyeRotation * transform.forward;
            Handles.color = new Color(1f, 1f, 1f, 0.2f);
            //아크모양의 Gizmos(중심, 회전축, 아크의 시작점, 회전각도, 중심으로부터의 아크거리)
            Handles.DrawSolidArc(eyeTransform.position, Vector3.up, leftRayDirection, fieldOfView, viewDistance);
        }
        
    }
    
#endif
    
    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        audioPlayer = GetComponent<AudioSource>();
        skinRenderer = GetComponentInChildren<Renderer>();

        //Enemy의 높이값과 attackRoot의 높이값을 일치
        var attackPivot = attackRoot.position;
        attackPivot.y = transform.position.y;

        //공격시도의 거리값(높이를 고려하지않고 수평값으로만)을 초기화
        attackDistance = Vector3.Distance(transform.position, attackPivot) + attackRadius;

        agent.stoppingDistance = attackDistance;
        agent.speed = patrolSpeed;
    }

    /// <summary>
    /// 적의 능력치를 초기화
    /// 이후 자동생성에서 불러오기위해 Public 생성자
    /// </summary>
    public void Setup(float health, float damage, float runSpeed, float patrolSpeed, Color skinColor)
    {
        //외부에서 설정된 Enemy의 능력값
        this.startingHealth = health;
        this.health = health;
        this.damage = damage;
        this.runSpeed = runSpeed;
        this.patrolSpeed = patrolSpeed;

        //능력치에 따른 스킨컬러를 변경
        skinRenderer.material.color = skinColor;
        //초기에 설정된 이동속도에서 다시 변경되었으니 재할당
        agent.speed = patrolSpeed;
    }

    private void Start()
    {
        StartCoroutine(UpdatePath());
    }

    private void Update()
    {
        //현재 상태에 따라 애니메이션을 재생 and 추적대상과의 거리를 매번 검사하여 공격을 구현
        if(dead) //죽은상태라면 즉시 종료
        {
            return;
        }

        //만약 추격중이라면
        if(state == State.Tracking)
        {
            //타겟과의 거리
            var distance = Vector3.Distance(targetEntity.transform.position, transform.position);
            //타겟과의 거리가 공격범위보다 작거나 같을때
            if(distance <= attackDistance)
            {
                //공격시작하는 함수실행
                BeginAttack();
            }
        }
        //desiredVelocity = 현재속도로 설정하고 싶은값
        //(장애물에 가로막혔을때 오브젝트의 속도값은 변화가 없지만 실제로는 이동하지않기에 0값이다.)
        animator.SetFloat("Speed", agent.desiredVelocity.magnitude);
    }

    /// <summary>
    /// 현재 상태에 따라 공격범위에 걸친 target에 데미지를 줌
    /// </summary>
    private void FixedUpdate()
    {
        if (dead)
        {
            return;
        }

        if(state == State.AttackBegin || state == State.Attacking)
        {
            //공격이 시작되면 Enemy의 회전을 강제로 target을 바라보게 한다.
            var lookRotation = Quaternion.LookRotation(targetEntity.transform.position - transform.position);
            //y축만 회전
            var targetAngleY = lookRotation.eulerAngles.y;

            //부드럽게 y축 회전
            targetAngleY = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngleY, ref turnSmoothVelocity, turnSmoothTime);
            transform.eulerAngles = Vector3.up * targetAngleY;
        }

        //EnableAttack()함수가 실행되면 true
        if (state == State.Attacking)
        {
            var direction = transform.forward;
            var deltaDistance = agent.velocity.magnitude * Time.fixedDeltaTime;

            //원형모양의 구역을 통해서 시작점과 끝점까지의 체크되는 오브젝트(콜라이더)를 배열에 할당 (스위핑테스트(휘두르는))
            //SphereCastNonAlloc(원의 중심, 반지름, 원이 이동방향, 결과를 받아올 배열, 방향으로 이동한 거리, 필터링)
            var size = Physics.SphereCastNonAlloc(attackRoot.position, attackRadius, direction, hits, deltaDistance, whatIsTarget); /*배열(ref) hits를 ref로 받지않음*/

            for (int i = 0; i < size; i++)
            {
                //받아온 hits의 정보에 LivingEntity가 있는지 for문을 통해 확인
                var attackTargetEntity = hits[i].collider.GetComponent<LivingEntity>();
                if(attackTargetEntity != null && !lastAttackedTargets.Contains(attackTargetEntity))
                {
                    var message = new DamageMessage();
                    message.amount = damage;
                    message.damager = gameObject;

                    //휘두르자마자(움직이기도 전에) 오브젝트가 걸린다면 hits[i].point는 0이 된다 따라서 hits[i].distance는 0이 되는 오류 발생
                    if (hits[i].distance <=0)
                    {
                        message.hitPoint = attackRoot.position;
                    }
                    //휘두르는 중간에 오브젝트가 걸린다면
                    else
                    {
                        message.hitPoint = hits[i].point;
                    }

                    message.hitNormal = hits[i].normal;

                    attackTargetEntity.ApplyDamage(message);
                    lastAttackedTargets.Add(attackTargetEntity);
                    break;

                }
            }
        }
    }

    /// <summary>
    /// 죽지않았다면 계속실행되는 코루틴함수
    /// </summary>
    private IEnumerator UpdatePath()
    {
        while (!dead)
        {
            if (hasTarget) //추격대상을 찾았을때
            {
                //추격대상을 발견즉시 상태변경 및 이동속도 변경
                if(state == State.Patrol)
                {
                    state = State.Tracking;
                    agent.speed = runSpeed;
                    animator.SetTrigger("Run");
                }
                //타겟의 위치로 이동
                agent.SetDestination(targetEntity.transform.position);
            }
            else //추격대상을 찾지못했을 때(Patrol 상태)
            {
                if (targetEntity != null)
                {
                    targetEntity = null;
                }

                if(state != State.Patrol)
                {
                    state = State.Patrol;
                    agent.speed = patrolSpeed;
                }


                if (agent.remainingDistance <= 1f) //목표지점까지의 남은거리
                {
                    //현재 위치에서 랜덤한 반경으로 수색
                    var patrolTargetPosition = Utility.GetRandomPointOnNavMesh(transform.position, 20f, NavMesh.AllAreas);
                    agent.SetDestination(patrolTargetPosition);
                }

                //Enemy의 원형에 있는 모든 오브젝트를 가져오고 시야각(Arc)내에 Raycast가 정확하게 Player를 인지하는지 확인
                var colliders = Physics.OverlapSphere(eyeTransform.position, viewDistance, whatIsTarget);

                //모든 collider를 찾아와 foreach문을 통해 살아있는 생명체인지 && 보이는 생명체인지(오브젝트뒤에 숨어있다면 제외)
                foreach(var collider in colliders)
                {
                    //충돌되는 target이 없다면
                    if(!IsTargetOnSight(collider.transform))
                    {
                        break;
                    }

                    //시야각내에 존재한다면 살아있는지 확인
                    var livingEntity = collider.GetComponent<LivingEntity>();
                    //livingEntity가 존재하고, 죽은상태가 아니라면
                    if(livingEntity != null && !livingEntity.dead)
                    {
                        //타깃에 할당
                        targetEntity = livingEntity;
                        break;
                    }

                }
            }
            
            //0.05초마다 대상을 검색
            yield return new WaitForSeconds(0.005f);
        }
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        //base의 ApplyDamage를 실행하지 못했다면 false 반환
        if (!base.ApplyDamage(damageMessage))
        {
            return false;
        }
        
        //수색상태에서 정상적으로 데미지가 들어온다면
        if(targetEntity == null)
        {
            //강제로 타겟을 지정하여 추격
            targetEntity = damageMessage.damager.GetComponent<LivingEntity>();
        }

        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint, damageMessage.hitNormal, transform, EffectManager.EffectType.Flesh);
        audioPlayer.PlayOneShot(hitClip);
        return true;
    }

    /// <summary>
    /// 공격을 실행하는 시작시점(데미지x)
    /// </summary>
    public void BeginAttack()
    {
        state = State.AttackBegin;

        //공격이 시작되면 추격을 중지
        agent.isStopped = true;
        //공격이 시작되면 Attack Trigger실행
        animator.SetTrigger("Attack");
    }

    /// <summary>
    /// 실제로 데미지가 적용(Animator Event를 통해 실행)
    /// </summary>
    public void EnableAttack()
    {
        state = State.Attacking;
        
        //직전까지 공격대상이였던 List를 비움
        lastAttackedTargets.Clear();
    }
    /// <summary>
    /// 공격이 끝나는 시점
    /// </summary>
    public void DisableAttack()
    {
        if(hasTarget)
        {
            state = State.Tracking;
        }
        else
        {
            state = State.Patrol;
        }
        //공격이 종료되면 다시 추격모드
        agent.isStopped = false;
    }

    /// <summary>
    /// 시야내에 존재하는 target인지 확인하는 함수
    /// </summary>
    private bool IsTargetOnSight(Transform target)
    {
        //레이캐스트의 광선이 시야각 내에서 이루어져야한다.
        //Target까지의 가로막는 오브젝트가 없어야한다

        //direction = 눈의 위치에서 타겟의 위치까지의 거리값
        var direction = target.position - eyeTransform.position;
        //타겟의 위치와 Enemy의 눈앞쪽의 y값을 동일처리
        direction.y = eyeTransform.forward.y;

        //광선의 시야각
        if(Vector3.Angle(direction, eyeTransform.forward) > fieldOfView * 0.5f)
        {
            return false;
        }

        direction = target.position - eyeTransform.position;

        RaycastHit hit;

        //중간에 가로막는 오브젝트 확인
        if(Physics.Raycast(eyeTransform.position, direction, out hit, viewDistance, whatIsTarget))
        {
            if(hit.transform == target)
            {
                return true;
            }
        }

        return false;
    }
    
    public override void Die()
    {
        base.Die();
        //죽었다면 Collider를 비활성화(방해하지 않도록)
        GetComponent<Collider>().enabled = false;

        agent.enabled = false;

        //사망시 해당오브젝트의 위치를 애니메이션이 통제
        animator.applyRootMotion = true;
        animator.SetTrigger("Die");

        audioPlayer.PlayOneShot(deathClip);
    }
}