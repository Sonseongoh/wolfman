using System.Collections;
using UnityEngine;

/// <summary>
/// 원거리 적의 발사 (#28). EnemyChase(keepDistance)로 거리를 유지하면서,
/// 사거리 안이면 주기적으로 플레이어를 향해 투사체를 쏜다.
/// </summary>
public class EnemyRangedAttack : MonoBehaviour
{
    [Tooltip("발사할 투사체 프리팹 (EnemyProjectile)")]
    public GameObject projectilePrefab;

    [Tooltip("발사 간격(초)")]
    public float fireInterval = 2.2f;

    [Tooltip("이 거리 안에 플레이어가 있어야 발사")]
    public float fireRange = 11f;

    [Tooltip("발사 직전 예비 동작(반짝임) 시간 — 보고 피할 틈")]
    public float windup = 0.25f;

    Transform player;
    SpriteRenderer sr;
    float timer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        // 같은 웨이브에 나온 개체들이 일제사격하지 않게 첫 발 타이밍을 흩뜨린다
        timer = Random.Range(0.8f, fireInterval);
    }

    void Update()
    {
        if (player == null || projectilePrefab == null) return;
        if (Vector2.Distance(player.position, transform.position) > fireRange) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = fireInterval;
        StartCoroutine(FireRoutine());
    }

    IEnumerator FireRoutine()
    {
        // 예비 동작: 잠깐 밝게 반짝여서 "쏜다"를 예고
        Color prev = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = new Color(1f, 1f, 0.6f);
        yield return new WaitForSeconds(windup);
        if (sr != null) sr.color = prev;

        if (player == null) yield break;

        GameObject go = Instantiate(projectilePrefab, transform.position, Quaternion.identity);
        EnemyProjectile proj = go.GetComponent<EnemyProjectile>();
        if (proj != null) proj.SetDirection(player.position - transform.position);
    }
}
