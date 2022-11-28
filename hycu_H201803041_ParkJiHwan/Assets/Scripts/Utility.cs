using UnityEngine;
using UnityEngine.AI;

public static class Utility
{
    /// <summary>
    /// (중심, 반경거리, 검색할 areaMask)
    /// </summary>
    public static Vector3 GetRandomPointOnNavMesh(Vector3 center, float distance, int areaMask)
    {
        //(반경(반지름) 1을 가진 원안의 임의의 위치값 * 거리) + 자신
        //즉 자신의 위치에서 랜덤하게 생성된 원에 거리값을 더한 값
        var randomPos = Random.insideUnitSphere * distance + center;
        
        //raycastHit과 비슷한 기능
        NavMeshHit hit;
        
        //Back된 NavMesh의 정보를 바탕으로 위치를 지정한다(위치값, 받아온 Hit정보, 반경, areaMask)
        NavMesh.SamplePosition(randomPos, out hit, distance, areaMask);
        
        //위 SamplePosition에서 받아온 (랜덤한)위치값을 반환한다.
        return hit.position;
    }
    
    /// <summary>
    /// 정규분포랜덤(평균값, 표준편차값)
    /// 표준편차값이 높을수록 평균값의 범위가 넓어진다.(탄퍼짐이 심해짐)
    /// </summary>
    public static float GedRandomNormalDistribution(float mean, float standard)
    {
        var x1 = Random.Range(0f, 1f);
        var x2 = Random.Range(0f, 1f);
        return mean + standard * (Mathf.Sqrt(-2.0f * Mathf.Log(x1)) * Mathf.Sin(2.0f * Mathf.PI * x2));
    }
}