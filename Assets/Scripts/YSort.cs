using UnityEngine;

/// <summary>
/// 탑다운 Y 정렬: 발이 화면에서 아래에 있을수록 앞에 그려진다.
/// 바위 위쪽에 서면 바위 뒤로 가려지고, 아래쪽에 서면 바위 앞에 나온다.
///
/// 발 위치 = 콜라이더 바닥(bounds.min.y). 카메라 기준 상대값이라 무한 맵 어디서든 동작.
/// 결과 범위는 [-80, 80] — 타일맵(-90 이하)과 이펙트(200 이상) 사이 층.
/// PlayerMovement·EnemyChase가 자동 장착, 장애물 프리팹은 HuntTerrain이 붙인다.
/// </summary>
public class YSort : MonoBehaviour
{
    SpriteRenderer sr;
    Collider2D col;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
    }

    void LateUpdate()
    {
        if (sr == null) return;

        float footY = col != null ? col.bounds.min.y : transform.position.y;
        float camY = Camera.main != null ? Camera.main.transform.position.y : 0f;

        sr.sortingOrder = Mathf.Clamp(Mathf.RoundToInt((camY - footY) * 5f), -80, 80);
    }
}
