using UnityEngine;
using UnityEngine.AI;

public class ItemSpawner : MonoBehaviour
{
    public GameObject[] items;          //생성될 아이템(프리펩) 오브젝트 배열
    public Transform playerTransform;   //생성될 반경의 위치
    
    private float lastSpawnTime;        //마지막에 아이템이 생성된 시간
    public float maxDistance = 5f;      //플레이어를 중심으로 최대반경
    
    private float timeBetSpawn;         //아이템 생성후 다음생성까지 소요될 대기시간

    public float timeBetSpawnMax = 7f;  //스폰 대기시간의 최대 대기시간
    public float timeBetSpawnMin = 2f;  //스폰 대기시간의 최소 대기시간

    private void Start()
    {
        timeBetSpawn = Random.Range(timeBetSpawnMin, timeBetSpawnMax);
        lastSpawnTime = 0f;
    }

    private void Update()
    {
        if(Time.time >= lastSpawnTime + timeBetSpawn && playerTransform != null)
        {
            Spawn();
            lastSpawnTime = Time.time;
            timeBetSpawn = Random.Range(timeBetSpawnMin, timeBetSpawnMax);
        }
    }

    private void Spawn()
    {
        //(플레이어중심, 최대반경, 맵의 모든 부분)
        var spawnPosition = Utility.GetRandomPointOnNavMesh(playerTransform.position, maxDistance, NavMesh.AllAreas);

        //생성되는 위치값 수정
        spawnPosition += Vector3.up * 0.5f;

        //item에 Instantiate(items배열의 길이만큼 랜덤의 Index값으로 생성, 생성될 위치값, 회전값)
        var item = Instantiate(items[Random.Range(0, items.Length)], spawnPosition, Quaternion.identity);

        //생성된 아이템은 5초뒤에 삭제
        Destroy(item, 5f);
    }
}