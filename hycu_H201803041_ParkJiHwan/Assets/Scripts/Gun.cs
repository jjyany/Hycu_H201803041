using System;
using System.Collections;
using UnityEngine;

public class Gun : MonoBehaviour
{
    /// <summary>
    /// 총의 상태를 표현
    /// </summary>
    public enum State
    {
        Ready,      //발사준비완료
        Empty,      //탄알없음
        Reloading   //재장전
    }
    public State state { get; private set; } // 외부에선 값을 가져오는것만 가능
    
    private PlayerShooter gunHolder;            //총의 주인이 누구인지
    private LineRenderer bulletLineRenderer;    //총알의 궤적
    
    private AudioSource gunAudioPlayer;         //해당 오브젝트의 오디오 클립을 재생하기 위한 소스
    public AudioClip shotClip;                  //발사소리
    public AudioClip reloadClip;                //재장전소리
    
    public ParticleSystem muzzleFlashEffect;    //총구발사 효과
    public ParticleSystem shellEjectEffect;     //탄피배출 효과
    
    public Transform fireTransform;             //총알이 나가는 위치와 방향
    public Transform leftHandMount;             //왼손이 자리할 위치값

    public float damage = 25;                   //데미지
    public float fireDistance = 100f;           //총알의 발사가능한 최대거리

    public int ammoRemain = 100;                //남은 탄약
    public int magAmmo;                         //현재 탄창에 있는 탄
    public int magCapacity = 30;                //탄창 용량

    public float timeBetFire = 0.12f;           //총알발사간의 거리
    public float reloadTime = 1.8f;             //재장전 시간
    
    [Range(0f, 10f)] public float maxSpread = 3f; //탄착군의 최대범위(값이 크면 탄퍼짐이 더 넓어짐)
    [Range(1f, 10f)] public float stability = 1f; //안전성(반동증가 속도) 높을수록 반동이 적음(안정석이 높음)
    [Range(0.01f, 3f)] public float restoreFromRecoilSpeed = 2f; //연사중단시 탄퍼짐이 다시 초기화되기까지 필요한 속도(높을수록 빠름)
    private float currentSpread;                //현재 탄퍼짐의 정도
    private float currentSpreadVelocity;        //현재 탄퍼짐의 반경의 변화량

    private float lastFireTime;                 //가장 최근에 발사한 시점

    private LayerMask excludeTarget;            //총알이 맞으면 안되는 Layer체크

    private void Awake()
    {
        //불러올 컴포넌트
        gunAudioPlayer = GetComponent<AudioSource>();
        bulletLineRenderer = GetComponent<LineRenderer>();

        //라인렌더러의 점의 갯수(시작점, 끝점 = 2)
        bulletLineRenderer.positionCount = 2;
        //비활성화를 대비하여
        bulletLineRenderer.enabled = false;
    }

    /// <summary>
    /// 총의 주인이(슈터) 총의 초기화(플레이어에서 실행) + 총의 주인이 누구인지 확인(총의 입장에서)
    /// </summary>
    public void Setup(PlayerShooter gunHolder)
    {
        //총의 주인이 누구인지? = 플레이어
        this.gunHolder = gunHolder;

        //타겟처리를 하지않을 오브젝트를 gun내부에 저장
        excludeTarget = gunHolder.excludeTarget;
    }

    /// <summary>
    /// 총이 활성화 될때마다 매번 총의 상태를 초기화
    /// </summary>
    private void OnEnable()
    {
        //총알의 최대용량으로 설정
        magAmmo = magCapacity;
        //총의 상태를 준비상태로 설정
        state = State.Ready;
    }

    /// <summary>
    /// 총이 비활성화 된다면 내부에 실행되는 코루틴을 모두 종료
    /// </summary>
    private void OnDisable()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// 건클래스 외부에서 총을 사용할때 발사를 시도하기위해 만들어진 함수(발사가 가능한 상태인지 확인)
    /// </summary>
    public bool Fire(Vector3 aimTarget)
    {
        //먼저 발사가능한 상태인지 확인
        //현재 상태가 래디상태이며 현재시간이 마지막 발사시점에서 발사간격을 더한것보다 시간이 더 흘렀다면(연사속도)
        if (state == State.Ready && Time.time >= lastFireTime + timeBetFire)
        {
            //총알의 방향 구하기(타겟 - 총구위치 = 타겟방향)
            var fireDirection = aimTarget - fireTransform.position;

            //정규분포랜덤(탄퍼짐 오차)
            var xError = Utility.GedRandomNormalDistribution(0f, currentSpread); //x방향의 탄퍼짐
            var yError = Utility.GedRandomNormalDistribution(0f, currentSpread); //y방향의 탄퍼짐
            //위 랜덤값을 활용하여 게임월드상에 총구방향을 적용시킨다.
            fireDirection = Quaternion.AngleAxis(yError, Vector3.up) * fireDirection;       //y축(Vector3.up)을 기준으로 yError만큼 회전한다.
            fireDirection = Quaternion.AngleAxis(xError, Vector3.right) * fireDirection;    //x축(Vector3.right)을 기준으로 xError만큼 회전한다.

            //발사실행시 다음회차에 반동이 커지고 정확도가 떨어지기 위함
            currentSpread += 1f / stability; //stability값이 커질수록 덧붙는 값이 낮아지기에 반동이 줄어듬

            //마지막으로 발사한 시점을 현시간으로 다시 갱신
            lastFireTime = Time.time;

            Shot(fireTransform.position, fireDirection);

            return true;
        }
        return false;
    }
    
    /// <summary>
    /// 실제 발사되는 함수(발사지점, 방향을 입력받음)
    /// </summary>
    private void Shot(Vector3 startPoint, Vector3 direction)
    {
        //레이캐스트 충돌정보를 저장
        RaycastHit hit;
        Vector3 hitPosition = Vector3.zero;

        //Raycast(시작점, 방향, 충돌오브젝트의 정보를 저장, 레이캐스트의 사정거리, 특정레이어에 대해서만 실행)
        if(Physics.Raycast(startPoint, direction, out hit, fireDistance, ~excludeTarget/*충돌제외(플립 오퍼레이터) 오브젝트를 반전시킴*/))
        {
            //충돌한 레이어가 데미지를 입힐수있는 타겟인지 확인
            var target = hit.collider.GetComponent<IDamageable>();

            //타겟이 데미지를 입는 레이어라면
            if(target != null)
            {
                DamageMessage damageMessage; //value 타입

                //초기화
                damageMessage.damager = gunHolder.gameObject;   //가해자(플레이어)
                damageMessage.amount = damage;                  //데미지값
                damageMessage.hitPoint = hit.point;             //타격위치
                damageMessage.hitNormal = hit.normal;           //공격방향(반대방향)

                //IDamageable을 가지고있는 오브젝트는 반드시 ApplyDamage를 실행해야 한다(인터페이스)
                target.ApplyDamage(damageMessage);
            }
            else
            {
                //일반오브젝트의 경우
                EffectManager.Instance.PlayHitEffect(hit.point, hit.normal, hit.transform);
            }

            //충돌 위치의 포지션을 저장
            hitPosition = hit.point;
        }
        else//아무것도 충돌되지 않았다면 최대 사정거리까지 날아간다.
        {
            hitPosition = startPoint + direction * fireDistance;
        }

        //Shot 실행시 조건없이 이펙트를 실행하는 코루틴 실행
        StartCoroutine(ShotEffect(hitPosition));

        //Shot 실행될때마다 탄약이 1씩 줄어듬
        magAmmo--;

        if(magAmmo <= 0) //탄약이 0
        {
            //Gun의 상태가 Empty(총알없음)상태로 변경
            state = State.Empty;
        }
    }

    /// <summary>
    /// 공격받은 지점을 입력받아 해당 위치에 이펙트효과
    /// </summary>
    private IEnumerator ShotEffect(Vector3 hitPosition)
    {
        muzzleFlashEffect.Play();
        shellEjectEffect.Play();

        //연사속도에 맞춰 연달아 실행하기 위해 PlayOneShot함수 사용
        gunAudioPlayer.PlayOneShot(shotClip);
        //Play를 사용한 AudioSouce사용(반복재생을 위해 사용x)
        /*
         * gunAudioPlayer.clip = shotClip;
         * gunAudioPlayer.Play();
         */

        //라인렌더러를 활성화, 잠깐키고 다시대기상태로 가기위해 코루틴 사용
        bulletLineRenderer.enabled = true;

        //라인렌더러의 Size를 설정해두었기에 Index값에 따라 위치값 표기
        bulletLineRenderer.SetPosition(0, fireTransform.position); //첫번째 점(총구위치값)
        bulletLineRenderer.SetPosition(1, hitPosition);            //두번째 점(hitPosition)

        //대기 시간이 없다면 라인렌더러는 바로 없어짐(뿅!)
        yield return new WaitForSeconds(0.03f);

        //라인렌더러가 0.03초 뒤에 다시 비활성화
        bulletLineRenderer.enabled = false;
    }
    
    /// <summary>
    /// 외부에서 재장전 처리를 위한 함수
    /// </summary>
    public bool Reload()
    {
        //재장전중 이거나 또는 남은탄알이 없거나 또는 이미 탄알집에 가득 차있거나
        if(state == State.Reloading || ammoRemain <= 0 || magAmmo >= magCapacity)
        {
            //실행불가
            return false;
        }

        StartCoroutine(ReloadRoutine());
        return true;
    }

    /// <summary>
    /// 실제로 내부에서 재장전 기능을 처리하는 코루틴함수
    /// </summary>
    private IEnumerator ReloadRoutine()
    {
        //Gun의 상태부터 재장전 상태로변경 (재장전 도중 발사 및 재장전 방지)
        state = State.Reloading;
        //재장전 음향
        gunAudioPlayer.PlayOneShot(reloadClip);
        //reloadTime동안 대기상태
        yield return new WaitForSeconds(reloadTime);

        //탄알집의 빈자리를 구함(탄알집용량 - 현재탄창, 0 , 가지고있는 총 탄의 갯수)
        //Clamp : 현재 보유중인 총 탄알의 갯수만큼 채워주어야 한다. (범위값, 0부터, max값)
        var ammoToFill = Mathf.Clamp(magCapacity - magAmmo, 0, ammoRemain);

        //실제 총알갯수만큼 탄알집에 채워준다
        magAmmo += ammoToFill;
        //보유중인 탄알의 갯수를 채운 탄알갯수만큼 빼준다
        ammoRemain -= ammoToFill;

        state = State.Ready;
    }

    private void Update()
    {
        //총의 반동값을 매프레임마다 실행

        //maxSpread값을 넘기지 못하게끔 현재탄퍼짐의 정도의 범위를 정한다.
        currentSpread = Mathf.Clamp(currentSpread, 0f, maxSpread);

        //매 프레임마다 스무스하게 0으로 줄어든다(탄퍼짐, 0(목표값), 현재값의 변화량, 1 / 지연시간(안정성 증가))
        currentSpread = Mathf.SmoothDamp(currentSpread, 0f, ref currentSpreadVelocity, 1f / restoreFromRecoilSpeed);
    }
}