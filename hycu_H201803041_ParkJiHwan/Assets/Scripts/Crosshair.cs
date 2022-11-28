using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{
    public Image aimPointReticle; //조준점
    public Image hitPointReticle; //실제타깃위치

    public float smoothTime = 0.2f; //실제타깃위치가 다시 정중앙으로 부드럽게 돌아오는 지연시간
    
    private Camera screenCamera;   //실제타깃위치점(월드좌표계)가 어디를 가르키는지 카메라를 통해 확인
    private RectTransform crossHairRectTransform; //실제타깃위치

    private Vector2 currentHitPointVelocity;
    private Vector2 targetPoint;

    private void Awake()
    {
        screenCamera = Camera.main;
        crossHairRectTransform = hitPointReticle.GetComponent<RectTransform>();
    }

    public void SetActiveCrosshair(bool active)
    {
        //게임시작시 크로스헤어 활성화
        aimPointReticle.enabled = active;
        hitPointReticle.enabled = active;
    }

    /// <summary>
    /// 월드좌표를 받아와 크로스헤어를 해당좌표로 위치시킨다
    /// </summary>
    public void UpdatePosition(Vector3 worldPoint)
    {
        targetPoint = screenCamera.WorldToScreenPoint(worldPoint);
    }

    private void Update()
    {
        //크로스헤어가 활성화되지 않았다면
        if(!hitPointReticle.enabled)
        {
            //해당 스크립트를 실행하지 않는다.
            return;
        }


        crossHairRectTransform.position = Vector2.SmoothDamp(crossHairRectTransform.position, targetPoint, ref currentHitPointVelocity, smoothTime);
    }
}