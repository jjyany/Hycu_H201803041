using UnityEngine;


/// <summary>
/// 캐릭터의 체력을 관리하는 LivingEntity를 상속받는 클래스
/// </summary>
public class PlayerHealth : LivingEntity
{
    private Animator animator;              //사망효과를 나타내기 위한 애니메이터
    private AudioSource playerAudioPlayer;  //사망 및 피격시 음향효과를 나타낼 오디오소스

    public AudioClip deathClip;             //사망
    public AudioClip hitClip;               //피격


    private void Awake()
    {
        playerAudioPlayer = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 상속받은 LivingEntity 클래스의 해당 함수를 override
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable(); //체력 초기화

        UpdateUI(); //체력 UI 초기화
    }
    
    public override void RestoreHealth(float newHealth)
    {
        base.RestoreHealth(newHealth);
        UpdateUI(); //체력회복시 UI에 반영
    }

    /// <summary>
    /// 체력 UI를 업데이트(갱신)
    /// </summary>
    private void UpdateUI()
    {
        //사망일 경우 0, 그외 체력값
        UIManager.Instance.UpdateHealthText(dead ? 0f : health);
    }
    
    public override bool ApplyDamage(DamageMessage damageMessage)
    {
        if (!base.ApplyDamage(damageMessage))
        {
            //공격실패
            return false;
        }

        //공격 성공시 이펙트매니저를 통해 해당위치에 피격이펙트효과
        EffectManager.Instance.PlayHitEffect(damageMessage.hitPoint, damageMessage.hitNormal, transform, EffectManager.EffectType.Flesh);
        //피격음향효과 
        playerAudioPlayer.PlayOneShot(hitClip);
        //데미지에 따라 UI 업데이트
        UpdateUI();

        return true;
    }
    
    public override void Die()
    {
        //기본 사망효과
        base.Die();

        //사망 음향효과
        playerAudioPlayer.PlayOneShot(deathClip);
        //사망 애니메이션
        animator.SetTrigger("Die");

        //UI업데이트
        UpdateUI();
    }
}