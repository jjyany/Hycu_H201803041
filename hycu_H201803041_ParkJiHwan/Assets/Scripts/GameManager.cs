using UnityEngine;

public class GameManager : MonoBehaviour
{
    //싱글톤
    private static GameManager instance;
    
    public static GameManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<GameManager>();
            }
            
            return instance;
        }
    }

    private int score;  //현재점수
    public bool isGameover { get; private set; } //자동생성 프로퍼티(외부에선 읽기만 가능)

    private void Awake()
    {
        if (Instance != this)
        {
            //해당 스크립트가 중복으로 생성되면 오브젝트파괴
            Destroy(gameObject);
        }
    }
    
    public void AddScore(int newScore)
    {
        if (!isGameover) //게임오버가 아니면
        {
            //스코어증가
            score += newScore;
            //스코어 UI 업데이트
            UIManager.Instance.UpdateScoreText(score);
        }
    }
    
    public void EndGame()
    {
        //게임오버
        isGameover = true;
        //게임오버 UI 활성화
        UIManager.Instance.SetActiveGameoverUI(true);
    }
}