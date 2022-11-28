using System.Collections.Generic;
using UnityEngine;

// 적 게임 오브젝트를 주기적으로 생성
public class EnemySpawner : MonoBehaviour
{
    //Enemy를 다루는 List
    //readonly : 한번 선언된 enemies는 재할당이 불가능
    private readonly List<Enemy> enemies = new List<Enemy>();

    public float damageMax = 40f;   //생성될 Enemy의 최대 데미지
    public float damageMin = 20f;   //생성될 Enemy의 최소 데미지
    public Enemy enemyPrefab;       //생성할 Enemy 원본오브젝트

    public float healthMax = 200f;  //생성될 Enemy의 최대 체력
    public float healthMin = 100f;  //생성될 Enemy의 최소 체력

    public Transform[] spawnPoints; //Enemy의 생성위치

    public float speedMax = 12f;    //생성될 Enemy의 최대 스피드
    public float speedMin = 3f;     //생성될 Enemy의 최소 스피드

    public Color strongEnemyColor = Color.red; //Enemy의 능력치에 따라 Red색상에 가깝게 생성
    private int wave;               //웨이브에 따라 생성될 Enemy의 능력치, 수량을 설정

    private void Update()
    {
        //게임매니저가 null이 아니고, 게임오버 상태일때
        if (GameManager.Instance != null && GameManager.Instance.isGameover)
        {
            //해당 스크립트 실행중지
            return;
        }

        //게임상에서 Enemy가 남아있지 않을 때
        if (enemies.Count <= 0)
        {
            //다음 Wave실행
            SpawnWave();
        }
        //UI갱신
        UpdateUI();
    }

    private void UpdateUI()
    {
        //wave와 Enemy의 수를 UI에 할당
        UIManager.Instance.UpdateWaveText(wave, enemies.Count);
    }
    
    /// <summary>
    /// 실제로 Enemy를 구현하는 함수
    /// </summary>
    private void SpawnWave()
    {
        wave++;

        //wave에 따라 생성되는 Enemy의 숫자를 반올림하여 할당 (2번째 wave = 10마리의 Enemy 생성)
        var spawnCount = Mathf.RoundToInt(wave * 5f);

        for(var i = 0; i < spawnCount; i++)
        {
            //생성되는 Enemy의 능력치의 %
            var enemyIntensity = Random.Range(0f, 1f); //0 ~ 100%

            CreateEnemy(enemyIntensity);
        }
    }
    
    /// <summary>
    /// intensity : 생성되는 Enemy의 능력치를 0 ~ 100%로 전달받음
    /// </summary>
    private void CreateEnemy(float intensity)
    {
        //생성될 Enemy의 능력치(최소값, 최대값, 랜덤으로 전달받은 능력치)
        var health = Mathf.Lerp(healthMin, healthMax, intensity);
        var damage = Mathf.Lerp(damageMin, damageMax, intensity);
        var speed = Mathf.Lerp(speedMin, speedMax, intensity);
        //생성될 Enemy의 색상
        var skinColor = Color.Lerp(Color.white, strongEnemyColor, intensity);
        //생성될 위치값(지정된 SpawnPoints의 랜덤)
        var spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        //게임상에 생성
        var enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
        //생성되는 Enemy의 초기값
        enemy.Setup(health, damage, speed, speed * 0.3f, skinColor);
        //
        enemies.Add(enemy);

        //Enemy가 사망상태일때 
        enemy.OnDeath += () => enemies.Remove(enemy);
        enemy.OnDeath += () => Destroy(enemy.gameObject, 10f);
        enemy.OnDeath += () => GameManager.Instance.AddScore(100);

    }
}