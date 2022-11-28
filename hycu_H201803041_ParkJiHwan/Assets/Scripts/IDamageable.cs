public interface IDamageable
{
    //ApplyDamage(공격을 한쪽이 받은쪽에게 전달하는 정보를 포함한 구조체)
    //DamageMessage에는 공격을 실행한 오브젝트, 공격의 양, 가해진 위치, 공격을 받은 표면의 노말벡터
    bool ApplyDamage(DamageMessage damageMessage);
}