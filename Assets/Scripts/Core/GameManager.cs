using UnityEngine;

/// <summary>
/// 게임 전체 흐름의 중심. 어느 씬에서든 GameManager.Instance로 접근.
/// - 현재 달, 라운드 번호, 진행 페이즈를 보관
/// - 모듈 간 통신은 이벤트 구독으로 (직접 참조 금지)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("데이터")]
    public MoonTable moonTable;

    [Header("설정")]
    [Tooltip("데미지 숫자 표시 여부 — 추후 설정 메뉴(#33)에서 조작")]
    public bool showDamageNumbers = true;

    public MoonData CurrentMoon { get; private set; }
    public RoundPhase Phase { get; private set; }
    public int RoundNumber { get; private set; }

    [Header("디버그")]
    [Tooltip("테스트용: 여기에 달을 꽂으면 확률 무시하고 그 달만 뜬다. 평소엔 비워둘 것!")]
    public MoonData debugForceMoon;

    /// <summary>라운드 시작, 달이 추첨됐을 때 (UI 연출, 룰셋 적용 등)</summary>
    public event System.Action<MoonData> OnMoonRevealed;

    /// <summary>페이즈가 바뀔 때마다 (씬 전환, 모드 시작/종료 등)</summary>
    public event System.Action<RoundPhase> OnPhaseChanged;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Phase = RoundPhase.Title;
    }

    /// <summary>
    /// 새 런 시작 (#121): 이전 런의 상태를 지우고 밤 1부터 다시 센다.
    /// 런 경계의 유일한 진입점 — 타이틀 새 시작이 부르고, 런을 끝내는 쪽(#12 시설 전멸)은
    /// Phase 를 Title 로 되돌려 타이틀로 보내기만 하면 다음 시작이 여기를 지난다.
    /// 죽음(#113)은 런의 끝이 아니므로 이 함수를 부르지 않는다 — StartNextRound 만 부른다.
    /// </summary>
    public void StartRun()
    {
        RoundNumber = 0; // StartNextRound 가 1로 올린다 — 새 런은 밤 1부터
        if (CurrencyManager.Instance != null) CurrencyManager.Instance.ResetRun();
        // 야성·굶주림(#117)이 들어오면 여기서 함께 리셋한다 (#121 AC 5)
        StartNextRound();
    }

    /// <summary>다음 라운드 시작: 달 추첨 → MoonReveal 페이즈로</summary>
    public void StartNextRound()
    {
        RoundNumber++;

        // 디버그 강제 달이 꽂혀 있으면 확률 무시 (테스트용)
        CurrentMoon = debugForceMoon != null
            ? debugForceMoon
            : (moonTable != null ? moonTable.Draw() : null);

        SetPhase(RoundPhase.MoonReveal);
        if (CurrentMoon != null) OnMoonRevealed?.Invoke(CurrentMoon);
    }

    /// <summary>페이즈 전환. 흐름 제어(#6)에서 호출 규칙을 잡는다.</summary>
    public void SetPhase(RoundPhase phase)
    {
        Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }
}
