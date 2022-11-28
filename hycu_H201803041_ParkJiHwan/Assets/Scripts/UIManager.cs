using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    
    public static UIManager Instance
    {
        //UIManager 스크립트를 싱클톤으로 구현(다른 스크립트가 쉽게 접근할수 있도록)
        get //접근
        {
            //UIManager가 할당되지 않았다면 강제로 할당
            if (instance == null) instance = FindObjectOfType<UIManager>();

            return instance;
        }
    }

    //외부에서 접근할 필요가 없기에 private로 선언
    [SerializeField] private GameObject gameoverUI;

    //크로스헤어는 별도로 관리(2가지 조준점을 표시해야하기 때문)
    [SerializeField] private Crosshair crosshair;

    [SerializeField] private Text healthText;
    [SerializeField] private Text lifeText;
    [SerializeField] private Text scoreText;
    [SerializeField] private Text ammoText;
    [SerializeField] private Text waveText;

    public void UpdateAmmoText(int magAmmo, int remainAmmo)
    {
        ammoText.text = magAmmo + "/" + remainAmmo;
    }

    public void UpdateScoreText(int newScore)
    {
        scoreText.text = "Score : " + newScore;
    }
    
    public void UpdateWaveText(int waves, int count)
    {
        waveText.text = "Wave : " + waves + "\nEnemy Left : " + count;
    }

    public void UpdateLifeText(int count)
    {
        lifeText.text = "Life : " + count;
    }

    public void UpdateCrossHairPosition(Vector3 worldPosition)
    {
        crosshair.UpdatePosition(worldPosition);
    }
    
    public void UpdateHealthText(float health)
    {
        healthText.text = Mathf.Floor(health).ToString();
    }
    
    public void SetActiveCrosshair(bool active)
    {
        crosshair.SetActiveCrosshair(active);
    }
    
    public void SetActiveGameoverUI(bool active)
    {
        gameoverUI.SetActive(active);
    }
    
    public void GameRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}