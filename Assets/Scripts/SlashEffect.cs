using UnityEngine;

/// <summary>
/// 발톱 참격 연출 (#38): 휘두른 방향으로 참격 스프라이트가 번쩍 나타났다 사라진다.
/// 코드로 생성 — 프리팹 불필요.
/// </summary>
public class SlashEffect : MonoBehaviour
{
    public float lifetime = 0.18f;

    SpriteRenderer sr;
    float t;
    Vector3 baseScale;

    public static void Spawn(Sprite sprite, Vector3 position, float angleDeg, bool flipY)
    {
        if (sprite == null) return;

        GameObject go = new GameObject("SlashEffect");
        go.transform.position = position;
        go.transform.rotation = Quaternion.Euler(0f, 0f, angleDeg);

        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.flipY = flipY;
        sr.sortingOrder = 200; // Y 정렬 캐릭터 층(-80~80) 위에 항상

        SlashEffect fx = go.AddComponent<SlashEffect>();
        fx.sr = sr;
        fx.baseScale = Vector3.one * 0.85f;
        go.transform.localScale = fx.baseScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = t / lifetime;

        if (p >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        // 빠르게 커지며 사라짐
        transform.localScale = baseScale * (1f + p * 0.45f);
        Color c = sr.color;
        c.a = 1f - p * p;
        sr.color = c;
    }
}
