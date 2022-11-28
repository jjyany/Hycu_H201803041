using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public string moveHorizontalAxisName = "Horizontal";
    public string moveVerticalAxisName = "Vertical";

    public string fireButtonName = "Fire1";
    public string jumpButtonName = "Jump";
    public string reloadButtonName = "Reload";

    public Vector2 moveInput { get; private set; } //외부에서 변경불가
    public bool fire { get; private set; }          //외부에서 변경불가
    public bool reload { get; private set; }        //외부에서 변경불가
    public bool jump { get; private set; }          //외부에서 변경불가
    
    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isGameover) //게임매니저가 없거나 게임오버일 경우 유저입력 무시
        {
            moveInput = Vector2.zero;
            fire = false;
            reload = false;
            jump = false;
            return;
        }

        moveInput = new Vector2(Input.GetAxis(moveHorizontalAxisName), Input.GetAxis(moveVerticalAxisName));

        if (moveInput.sqrMagnitude > 1) //대각선 이동시(1 보다 큼) 정규화
        {
            moveInput = moveInput.normalized;
        }

        jump = Input.GetButtonDown(jumpButtonName);
        fire = Input.GetButton(fireButtonName);
        reload = Input.GetButtonDown(reloadButtonName);
    }
}