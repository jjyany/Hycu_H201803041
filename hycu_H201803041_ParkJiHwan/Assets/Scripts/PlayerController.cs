using UnityEngine;
using UnityEngine.AI;

public class PlayerController : MonoBehaviour
{
    private Animator animator;
    private AudioSource playerAudioPlayer;
    private PlayerHealth playerHealth;
    private PlayerMovement playerMovement;
    private PlayerShooter playerShooter;

    public int lifeRemains = 3;
    public AudioClip itemPickupClip; //아이템 습득시 음향효과

    private void Start()
    {
        animator = GetComponent<Animator>();
        playerAudioPlayer = GetComponent<AudioSource>();
        playerHealth = GetComponent<PlayerHealth>();
        playerMovement = GetComponent<PlayerMovement>();
        playerShooter = GetComponent<PlayerShooter>();

        //죽었을때 Event를 추가
        playerHealth.OnDeath += HandleDeath;

        //UI에 Life 표기
        UIManager.Instance.UpdateLifeText(lifeRemains);

        //살아있다면 마우스커서 비활성화
        Cursor.visible = false;
    }
    
    /// <summary>
    /// 플레이어 사망시 
    /// </summary>
    private void HandleDeath()
    {
        playerMovement.enabled = false;
        playerShooter.enabled = false;

        //Life가 남아있다면
        if(lifeRemains > 0)
        {
            lifeRemains--;
            UIManager.Instance.UpdateLifeText(lifeRemains);

            //Life가 남아있다면 죽은뒤 3초뒤 리스폰
            Invoke("Respawn", 3f);
        }
        else
        {
            //Life가 남아있지 않다면 GameOver
            GameManager.Instance.EndGame();
        }

        //죽었다면 마우스커서 활성화
        Cursor.visible = true;
    }

    public void Respawn()
    {
        //초기화를 재설정하기 위해 껐다 킴
        gameObject.SetActive(false);
        //죽은 위치에서 30 반경에 랜덤한 지점에 리스폰
        transform.position = Utility.GetRandomPointOnNavMesh(transform.position, 30f, NavMesh.AllAreas);

        gameObject.SetActive(true);
        playerMovement.enabled = true;
        playerShooter.enabled = true;

        //다시 리스폰 된다면 탄알의 갯수를 채워줌
        playerShooter.gun.ammoRemain = 120;
        //리스폰된다면 다시 커서삭제
        Cursor.visible = false;
    }

    /// <summary>
    /// 생성된 아이템과 충돌시 실행되는 함수
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        //죽은상태라면 실행중지
        if(playerHealth.dead)
        {
            return;
        }

        //충돌된 오브젝트에 IItem 스크립트가 있는지 확인
        var item = other.GetComponent<IItem>();

        //스크립트가 존재한다면
        if(item != null)
        {
            //Use함수를 실행
            item.Use(this.gameObject);
            //아이템 습득 음향효과
            playerAudioPlayer.PlayOneShot(itemPickupClip);
        }
    }
}