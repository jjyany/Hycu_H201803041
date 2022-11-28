using UnityEngine;

/// <summary>
/// class = reference, struct = Value
/// 
/// class일 경우 메세지를 전달받은 측에서 데미지 메세지를 수정하면 같은 메세지를 받은 곳에서도 동일하게 적용되버린다.
/// 
/// struct의 경우 메세지를 전달받은 측에서 메세지값을 수정해도 적용되지 않는다.
/// </summary>
public struct DamageMessage 
{
    public GameObject damager; //가해자
    public float amount;       //공격의 데미지

    public Vector3 hitPoint;    //맞은부위
    public Vector3 hitNormal;   //공격받은 부위의 방향(공격의 반대방향) -> <-
}