using UnityEngine;

/// <summary>
/// 적 처치 연출: 잠깐 커지면서 투명해지다 사라진다.
/// EnemyHealth가 죽을 때 코드로 생성 — 프리팹 불필요.
/// </summary>
public class DeathPop : MonoBehaviour
{
    public float duration = 0.2f;
    public float scaleUp = 1.2f;

    SpriteRenderer sr;
    float t;
    Vector3 startScale;

    public static void Spawn(SpriteRenderer source)
    {
        if (source == null) return;

        GameObject go = new GameObject("DeathPop");
        go.transform.position = source.transform.position;
        go.transform.localScale = source.transform.localScale;

        SpriteRenderer popSr = go.AddComponent<SpriteRenderer>();
        popSr.sprite = source.sprite;
        popSr.color = source.color;
        popSr.flipX = source.flipX;
        popSr.flipY = source.flipY;
        popSr.sortingLayerID = source.sortingLayerID;
        popSr.sortingOrder = source.sortingOrder;

        go.AddComponent<DeathPop>();
    }

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startScale = transform.localScale;
    }

    void Update()
    {
        t += Time.deltaTime;
        float p = t / duration;

        if (p >= 1f)
        {
            Destroy(gameObject);
            return;
        }

        transform.localScale = startScale * Mathf.Lerp(1f, scaleUp, p);

        Color c = sr.color;
        c.a = 1f - p;
        sr.color = c;
    }
}
