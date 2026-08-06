using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 마을 시설의 체력·파괴·수리 (#11). 실제 판정은 순수 C# FacilityCore 가 한다.
/// 파괴돼도 오브젝트는 남는다 — 어두워진 채 그 자리에 서 있고, 금고 골드로 다시 세운다.
/// </summary>
public class FacilityHealth : MonoBehaviour
{
    [Tooltip("HUD에 표시할 시설 이름")]
    public string facilityName = "시설";

    [Tooltip("최대 체력")]
    public int maxHp = 10;

    [Tooltip("수리 비용 (금고에서 차감 — 남은 체력과 무관한 고정값)")]
    public int repairCost = 30;

    [Tooltip("플레이어가 이 거리 안에 들어와야 상호작용할 수 있다")]
    public float interactRange = 2.5f;

    [Tooltip("시작부터 파괴된 상태로 — 파괴·수리 사이클 검증용 임시 옵션")]
    public bool startDestroyed;

    [Tooltip("T 키로 피해를 주는 디버그 입력 — 습격(#12)이 들어오면 제거")]
    public bool enableDebugDamage = true;

    [Tooltip("파괴됐을 때 덮어씌울 색")]
    public Color destroyedTint = new Color(0.3f, 0.28f, 0.33f);

    FacilityCore core;
    SpriteRenderer sr;
    Color originalColor;
    Transform player;

    /// <summary>습격(#12) 등 외부에서 상태를 읽는 계약</summary>
    public bool IsDestroyed => core.IsDestroyed;

    /// <summary>파괴 여부에 따른 평상시 색 — 섬광이 끝나면 이 색으로 돌아온다</summary>
    Color BaseColor => core.IsDestroyed ? destroyedTint : originalColor;

    void Awake()
    {
        core = new FacilityCore(maxHp);
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) originalColor = sr.color;
    }

    void Start()
    {
        GameObject p = GameObject.FindWithTag("Player");
        if (p != null) player = p.transform;

        if (startDestroyed)
        {
            core.TakeDamage(core.MaxHp);
            ApplyBaseColor();
        }
    }

    void Update()
    {
        if (Keyboard.current == null || player == null) return;
        if (Vector2.Distance(transform.position, player.position) > interactRange) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            Repair();

        if (enableDebugDamage && Keyboard.current.tKey.wasPressedThisFrame)
            TakeDamage(1);
    }

    /// <summary>습격(#12)의 적이 호출할 진입점. 체력이 0이 돼도 오브젝트는 파괴하지 않는다.</summary>
    public void TakeDamage(int amount)
    {
        // 이미 파괴된 시설은 더 깎이지 않는다 — 연출도 내보내지 않는다
        if (amount <= 0 || core.IsDestroyed) return;

        bool justDestroyed = core.TakeDamage(amount);

        DamageNumber.Spawn(transform.position, amount.ToString(), new Color(1f, 0.55f, 0.35f));

        // 흰색 섬광
        if (sr != null)
        {
            StopAllCoroutines();
            StartCoroutine(FlashWhite());
        }

        if (justDestroyed)
        {
            ApplyBaseColor();
            DamageNumber.Spawn(transform.position, $"{facilityName} 파괴!", new Color(1f, 0.35f, 0.3f), 1.3f);
            CameraFollow.Shake(0.15f, 0.2f);
        }
    }

    /// <summary>금고 골드를 내고 만피로 복구. 잔액이 모자라면 아무것도 바뀌지 않는다.</summary>
    public void Repair()
    {
        if (CurrencyManager.Instance == null) return;

        RepairResult result = core.TryRepair(CurrencyManager.Instance.Wallet, repairCost);

        switch (result)
        {
            case RepairResult.Success:
                ApplyBaseColor();
                DamageNumber.Spawn(transform.position, "+수리 완료", new Color(0.4f, 1f, 0.5f), 1.2f);
                break;

            case RepairResult.NotEnoughGold:
                VillageController.ShowMessage($"금고 잔액 부족! (수리 비용 {repairCost}G)");
                DamageNumber.Spawn(transform.position, "골드 부족", new Color(1f, 0.35f, 0.3f), 1.1f);
                break;

            case RepairResult.NotDamaged:
                // 멀쩡한 시설 — 수리할 게 없으니 조용히 무시
                break;
        }
    }

    void ApplyBaseColor()
    {
        if (sr != null) sr.color = BaseColor;
    }

    System.Collections.IEnumerator FlashWhite()
    {
        sr.color = Color.white;
        yield return new WaitForSeconds(0.08f);
        sr.color = BaseColor;
    }

    // 임시 UI — 시설 머리 위에 상태와 상호작용 안내
    void OnGUI()
    {
        if (Camera.main == null) return;

        Vector3 sp = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 0.8f);
        float x = sp.x - 100f;
        float y = Screen.height - sp.y; // WorldToScreenPoint는 아래가 0, GUI는 위가 0

        GUIStyle style = new GUIStyle
        {
            fontSize = 16,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = core.IsDestroyed ? new Color(1f, 0.45f, 0.4f) : Color.white }
        };
        string state = core.IsDestroyed ? "파괴됨" : $"HP {core.Hp}/{core.MaxHp}";
        GUI.Label(new Rect(x, y, 200, 24), $"{facilityName}  {state}", style);

        bool inRange = player != null &&
            Vector2.Distance(transform.position, player.position) <= interactRange;
        if (!inRange) return;

        if (core.IsDamaged)
        {
            GUIStyle prompt = new GUIStyle
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, 0.85f, 0.3f) }
            };
            GUI.Label(new Rect(x, y + 22, 200, 24), $"[E] 수리 ({repairCost}G)", prompt);
        }

        if (enableDebugDamage)
        {
            GUIStyle hint = new GUIStyle
            {
                fontSize = 13,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(0.7f, 0.7f, 0.75f) }
            };
            GUI.Label(new Rect(x, y + 44, 200, 22), "[T] 피해", hint);
        }
    }
}
