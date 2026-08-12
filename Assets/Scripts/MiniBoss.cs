using System.Collections;
using UnityEngine;

/// <summary>
/// 중간보스 "무리의 우두머리" (#122). 5웨이브마다 등장.
/// 평소엔 EnemyChase로 추적하고, 주기적으로 패턴을 번갈아 쓴다:
///   ① 3연속 돌진 — 예비동작을 보고 옆으로 흘려야 한다
///   ② 하울링 소환 — 부하 늑대를 불러낸다
/// 넉백 면역은 프리팹의 EnemyChase.knockbackImmunity를 크게 잡아 처리.
/// HP바는 이 컴포넌트가 화면 상단에 그린다.
/// </summary>
public class MiniBoss : MonoBehaviour
{
    [Tooltip("보스 이름 (HP바에 표시)")]
    public string bossName = "무리의 우두머리";

    [Tooltip("패턴 사이 간격(초) — 그동안은 일반 추적")]
    public float patternInterval = 5f;

    [Header("돌진 패턴")]
    public float windupTime = 0.7f;
    public float chargeSpeed = 12f;
    public float chargeTime = 0.45f;

    [Header("소환 패턴")]
    [Tooltip("하울링으로 불러낼 부하 프리팹 (늑대)")]
    public GameObject minionPrefab;
    public int minionCount = 3;

    /// <summary>살아있는 보스들 — 중복 등장 허용(못 잡고 버틴 대가), HP바를 줄 세우는 데 사용</summary>
    public static readonly System.Collections.Generic.List<MiniBoss> All
        = new System.Collections.Generic.List<MiniBoss>();

    Rigidbody2D rb;
    EnemyChase chase;
    SpriteRenderer sr;
    EnemyHealth health;
    WalkWobble wobble;
    Transform player;
    float timer;
    bool acting;
    bool nextIsCharge = true; // 패턴 교대

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        chase = GetComponent<EnemyChase>();
        sr = GetComponent<SpriteRenderer>();
        health = GetComponent<EnemyHealth>();
        wobble = GetComponent<WalkWobble>();
        All.Add(this);
    }

    void OnDestroy()
    {
        All.Remove(this);
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        timer = patternInterval * 0.6f; // 첫 패턴은 조금 이르게
    }

    void Update()
    {
        if (acting || player == null) return;

        timer -= Time.deltaTime;
        if (timer > 0f) return;

        timer = patternInterval;
        StartCoroutine(nextIsCharge ? TripleCharge() : SummonMinions());
        nextIsCharge = !nextIsCharge;
    }

    /// <summary>① 3연속 돌진 — 첫 조준은 길게, 이어지는 돌진은 짧게 재조준</summary>
    IEnumerator TripleCharge()
    {
        acting = true;
        if (chase != null) chase.enabled = false;

        Color prev = sr != null ? sr.color : Color.white;

        for (int c = 0; c < 3; c++)
        {
            // 예비동작: 멈춰서 붉게 달아오른다 (2·3타는 짧게)
            float windup = c == 0 ? windupTime : windupTime * 0.5f;
            float t = 0f;
            while (t < windup)
            {
                t += Time.deltaTime;
                rb.linearVelocity = Vector2.zero;
                if (sr != null)
                    sr.color = Color.Lerp(prev, new Color(1f, 0.3f, 0.25f), t / windup);
                yield return null;
            }
            if (sr != null) sr.color = prev;

            if (player == null) break;

            Vector2 dir = ((Vector2)player.position - (Vector2)transform.position).normalized;
            if (sr != null && dir.x != 0f) sr.flipX = dir.x < 0f;

            t = 0f;
            while (t < chargeTime)
            {
                t += Time.fixedDeltaTime;
                rb.linearVelocity = dir * chargeSpeed;
                yield return new WaitForFixedUpdate();
            }
            rb.linearVelocity = Vector2.zero;
        }

        if (chase != null) chase.enabled = true;
        acting = false;
    }

    /// <summary>② 하울링 소환 — 몸을 부풀리며 부하 늑대를 불러낸다</summary>
    IEnumerator SummonMinions()
    {
        acting = true;
        if (chase != null) chase.enabled = false;
        rb.linearVelocity = Vector2.zero;

        // 하울링 연출: 부풀기 두 번
        if (wobble != null) { wobble.Punch(0.25f); }
        yield return new WaitForSeconds(0.4f);
        if (wobble != null) { wobble.Punch(0.25f); }
        yield return new WaitForSeconds(0.4f);

        if (minionPrefab != null)
        {
            for (int i = 0; i < minionCount; i++)
            {
                float a = (Mathf.PI * 2f / minionCount) * i;
                Vector3 offset = new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 1.6f;
                Instantiate(minionPrefab, transform.position + offset, Quaternion.identity);
            }
        }

        if (chase != null) chase.enabled = true;
        acting = false;
    }

    // 화면 상단 보스 HP바 (임시 OnGUI — Canvas 전환 때 함께 이동)
    void OnGUI()
    {
        if (health == null) return;
        if (Time.timeScale == 0f) return; // 스킬 선택·게임오버 화면 위엔 안 그린다

        // 보스가 여럿이면 위에서부터 줄줄이 쌓인다
        int row = All.IndexOf(this);
        float w = Screen.width * 0.46f, h = 16f;
        float x = (Screen.width - w) * 0.5f, y = 58f + row * 44f;

        // 이름
        GUIStyle name = new GUIStyle
        {
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.45f, 0.4f) }
        };
        GUI.Label(new Rect(0, y - 20, Screen.width, 20), bossName, name);

        // 테두리 + 배경 + 체력 채움
        GUI.color = new Color(0.6f, 0.1f, 0.12f, 0.9f);
        GUI.DrawTexture(new Rect(x - 2, y - 2, w + 4, h + 4), Texture2D.whiteTexture);
        GUI.color = new Color(0.08f, 0.05f, 0.06f, 0.95f);
        GUI.DrawTexture(new Rect(x, y, w, h), Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.2f, 0.2f);
        GUI.DrawTexture(new Rect(x, y, w * health.HpRatio, h), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
