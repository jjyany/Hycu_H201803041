using System;
using UnityEngine;

/// <summary>
/// 생명체로써 존재하는 모든 오브젝트(플레이어, Enemy)는 해당클래스를 가지고있는다
/// </summary>
public class LivingEntity : MonoBehaviour, IDamageable/*상속*/
{
    public float startingHealth = 100f; //초기 활성화 체력
    public float health { get; protected set; } //현재 체력 (해당클래스 및 상속클래스에서만 변경가능)
    public bool dead { get; protected set; } //사망상태 (해당클래스 및 상속클래스에서만 변경가능)

    public event Action OnDeath; //사망시 실행되는 (unity)이벤트
    
    private const float minTimeBetDamaged = 0.1f; //데미지와 데미지사이의 지연시간(같은시간에 중복공격을 받는것을 방지)
    private float lastDamagedTime;

    /// <summary>
    /// 무적상태(지연시간 체크)
    /// </summary>
    protected bool IsInvulnerabe //해당 클래스 내부 및 상속내에서만 변경이 가능
    {
        get
        {
            //현재시간이 마지막에 당한 공격 + 지연시간보다 큰 경우
            if (Time.time >= lastDamagedTime + minTimeBetDamaged)
            {
                //무적상태가 아님
                return false;
            }

            //그 외 무적상태
            return true;
        }
    }
    
    /// <summary>
    /// virtual = 해당클래스를 상속받는 자식클래스에서 해당 함수를 오버라이드 가능
    /// </summary>
    protected virtual void OnEnable()
    {
        dead = false;
        health = startingHealth;
    }

    public virtual bool ApplyDamage(DamageMessage damageMessage)
    {
        //무적상태 or 데미지가 본인에게 가하는 메세지일 경우 or 죽은상태
        if (IsInvulnerabe || damageMessage.damager == gameObject || dead)
        {
            //공격불가
            return false;
        }

        //마지막 공격시간 = 현재시간
        lastDamagedTime = Time.time;
        //현재 체력 - 데미지
        health -= damageMessage.amount;

        //체력이 0 보다 작거나 같을때
        if (health <= 0)
        {
            //죽음
            Die();
        }

        //공격성공
        return true;
    }
    
    /// <summary>
    /// 체력회복 함수
    /// </summary>
    public virtual void RestoreHealth(float newHealth)
    {
        //죽은 상태이면 회복불가능
        if (dead)
        {
            return;
        }
        
        //습득한 Item에 따라 체력회복
        health += newHealth;
    }
    
    public virtual void Die()
    {
        //죽은 상태가 아닐경우(이벤트에 1개 이상의 이벤트가 등록되어있다면
        if (OnDeath != null)
        {
            //사망 이벤트 실행
            OnDeath();
        }
        
        //사망상태
        dead = true;
    }
}