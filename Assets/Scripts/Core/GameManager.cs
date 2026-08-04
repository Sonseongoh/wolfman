using UnityEngine;

/// <summary>한 라운드의 진행 단계. 모듈 A/B/C가 공유하는 기준.</summary>
public enum RoundPhase
{
    MoonReveal,   // 달 공개 (C)
    ActionSelect, // 마을에 남기 vs 사냥 나가기 선택 (C)
    Village,      // 마을 디펜스/운영 (B)
    Hunt,         // 사냥 뱀서 전투 (A)
    Reward,       // 정산/보상 (C)
}

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
