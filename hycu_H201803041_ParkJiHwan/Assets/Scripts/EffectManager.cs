using UnityEngine;

/// <summary>
/// 외부에서 이펙트가 필요할때 해당 스크립트를 통해 원하는 위치에 이펙트를 생성
/// </summary>
public class EffectManager : MonoBehaviour
{
    private static EffectManager m_Instance;
    public static EffectManager Instance
    {
        //이펙트매니저 싱글톤화
        get
        {
            if (m_Instance == null) m_Instance = FindObjectOfType<EffectManager>();
            return m_Instance;
        }
    }

    public enum EffectType
    {





        Common, //일반적인 피탄효과
        Flesh   //적이 맞았을때 나는 피탄효과
    }
    
    public ParticleSystem commonHitEffectPrefab;    //일반피탄효과
    public ParticleSystem fleshHitEffectPrefab;     //적의피탄효과
    
    /// <summary>
    /// 이펙트가 생성되는 함수(재생위치, 재생방향(-), 이펙트 부모(기본값 없음)/*움직이는 대상의 부모를 따오기 위함*/, 사용할 이펙트타입(기본값 일반피탄효과))
    /// </summary>
    public void PlayHitEffect(Vector3 pos, Vector3 normal, Transform parent = null, EffectType effectType = EffectType.Common)
    {
        var targetPrefab = commonHitEffectPrefab;

        if(effectType == EffectType.Flesh)
        {
            targetPrefab = fleshHitEffectPrefab;
        } //else if문을 통해 또다른 이펙트 효과를 추가할수 있다.

        //이펙트 생성변수 Instantiate(원본오브젝트, 포지션, 회전)
        var effect = Instantiate(targetPrefab, pos, Quaternion.LookRotation(normal));

        //피격대상의 부모가 있다면
        if(parent != null)
        {
            effect.transform.SetParent(parent);
        }

        effect.Play();
    }
}