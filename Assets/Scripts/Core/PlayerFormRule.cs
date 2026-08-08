/// <summary>플레이어가 취하는 겉모습과 전투 능력의 조합 (#53).</summary>
public enum PlayerForm
{
    /// <summary>마을(운영)에서의 모습. 전투 능력을 쓰지 않는다.</summary>
    Human,

    /// <summary>사냥에서의 모습. 근거리 발톱으로 싸운다.</summary>
    Werewolf,
}

/// <summary>
/// 형태를 무엇으로 결정하는가 (#53). 답: 지금이 어느 페이즈인가, 그것 하나다.
///
/// 씬마다 값을 따로 맞추지 않는다 — 마을 씬에 늑대를 꽂아두는 실수도,
/// 사냥 씬에 인간을 꽂아두는 실수도 여기 한 곳을 지나면 일어나지 않는다.
/// UnityEngine 에 의존하지 않아 WSL 에서 테스트할 수 있다.
/// </summary>
public static class PlayerFormRule
{
    /// <summary>
    /// 이 페이즈에서 플레이어는 어떤 형태인가.
    /// 마을이면 인간, 그 외에는 늑대인간.
    ///
    /// "사냥이면 늑대"가 아니라 "마을이 아니면 늑대"인 것에 주의 —
    /// 사냥 중 페이즈는 Hunt 가 아니라 라운드 시작이 남긴 MoonReveal 이다.
    /// </summary>
    /// <param name="phase">
    /// 현재 페이즈. null 이면 페이즈를 모르는 상태 —
    /// 공용 게임 상태 없이 마을 씬을 단독 재생하는 개발 중 상황이라 인간으로 본다.
    /// </param>
    public static PlayerForm For(RoundPhase? phase)
    {
        if (phase == null) return PlayerForm.Human;

        return phase.Value == RoundPhase.Village ? PlayerForm.Human : PlayerForm.Werewolf;
    }
}
