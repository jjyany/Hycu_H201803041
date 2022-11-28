using UnityEngine;

//Item의 경우 해당 인터페이스를 컴포넌트해야한다.
public interface IItem
{
    //아이템이 동작할 target을 강제구현
    void Use(GameObject target);
}