/// <summary>
/// 라운드 한 바퀴가 어떻게 맞물리는가 (#78). 씬도 컴포넌트도 모르는 판정만 모았다.
///
/// 이 규칙들이 코드 안에 조건문으로 흩어져 있던 동안 두 가지가 조용히 어긋나 있었다 —
/// 사냥으로 끝낸 라운드가 정산되지 않았고, 라운드가 한 바퀴에 두 번 시작됐다.
/// 흩어진 조건문은 아무도 검사하지 않지만, 이름 붙은 규칙은 검사할 수 있다.
///
/// UnityEngine 에 의존하지 않아 WSL 에서 테스트한다.
/// </summary>
public static class RoundFlowRule
{
    /// <summary>
    /// 이 페이즈를 달고 정산 허브로 돌아왔다면, 라운드를 실제로 치르고 온 것인가.
    ///
    /// 사냥과 마을은 플레이어가 한 라운드를 보낸 곳이므로 임시 골드를 금고로 넘긴다.
    /// 그 밖의 페이즈로 허브에 있다는 건 아직 라운드를 시작하지 않았다는 뜻이라
    /// 정산할 것이 없다.
    /// </summary>
    public static bool ShouldSettle(RoundPhase phase)
        => phase == RoundPhase.Hunt || phase == RoundPhase.Village;

    /// <summary>
    /// 아직 아무도 라운드를 시작하지 않았는가.
    ///
    /// 라운드 시작의 주인은 흐름 제어(허브)다. 사냥 씬은 자기가 시작하지 않는다 —
    /// 단, 사냥 씬만 단독으로 재생하는 개발 상황에서는 시작해 줄 사람이 없으므로
    /// 그때만 스스로 시작한다. 그 "아무도 없음"을 라운드 번호로 판별한다.
    /// </summary>
    public static bool NeedsRoundStart(int roundNumber) => roundNumber <= 0;
}
