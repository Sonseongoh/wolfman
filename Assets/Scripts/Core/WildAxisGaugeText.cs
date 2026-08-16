using System;
using System.Globalization;

/// <summary>
/// 게이지가 무엇을 보여줄지 정하는 규칙 (#117). 그리기는 <c>WildAxisGaugeUI</c> 가 한다.
///
/// 판단을 그리기에서 떼어낸 이유는 검증이다 — 배치모드 유니티에서는 그래픽을 켜도
/// <c>OnGUI</c> 가 돌지 않아서(실측: 0.5초 동안 호출 0회) 라벨 문구가 맞는지,
/// 마커가 바 안에 있는지를 확인할 방법이 없었다. 여기로 빼두면 WSL 에서 검사할 수 있고,
/// UI 쪽에는 좌표와 색만 남아 Canvas 로 옮길 때 가져갈 것도 줄어든다.
///
/// UnityEngine 에 의존하지 않는다 — <c>Mathf</c> 대신 <c>System.Math</c> 를 쓰는 이유가 그것이다.
/// </summary>
public static class WildAxisGaugeText
{
    /// <summary>단계의 한국어 이름 (CONTEXT.md 용어집).</summary>
    public static string StageName(HungerStage stage)
    {
        switch (stage)
        {
            case HungerStage.Hungry: return "허기";
            case HungerStage.Famished: return "주림";
            case HungerStage.Starving: return "아사";
            case HungerStage.Limit: return "한계";
            default: return "포식";
        }
    }

    /// <summary>라벨을 적을 때인가. 포식이면 아무 일도 없으므로 화면을 채우지 않는다.</summary>
    public static bool ShowsLabel(HungerStage stage) => stage != HungerStage.Sated;

    /// <summary>
    /// 붉게 경고할 때인가. 아사부터는 주민이 피한다 —
    /// 상점(#13)이 아직 없어서 이 경고가 그 단계에 들어섰다는 유일한 가시 신호다.
    /// </summary>
    public static bool IsSevere(HungerStage stage)
        => stage == HungerStage.Starving || stage == HungerStage.Limit;

    /// <summary>
    /// 게이지 아래에 적는 한 줄. 포식이면 빈 문자열이다.
    /// 페널티를 숫자로 적는 이유는 "느낌"이 아니라 "얼마나"가 결정을 바꾸기 때문이다.
    /// </summary>
    public static string Label(HungerStage stage, float attackMultiplier, int maxHpPenalty)
    {
        if (!ShowsLabel(stage)) return string.Empty;

        // 소수 구분자가 지역 설정을 타면 한국어 윈도우와 테스트가 다른 문자열을 만든다.
        string mult = attackMultiplier.ToString("0.##", CultureInfo.InvariantCulture);
        string text = StageName(stage) + " — 공격 ×" + mult;

        if (maxHpPenalty > 0) text += ", 최대 체력 −" + maxHpPenalty;
        if (IsSevere(stage)) text += "   ◈ 주민이 피한다";

        return text;
    }

    /// <summary>
    /// 축 값을 바 위의 0..1 위치로. 0 = 굶주림 한계(왼쪽 끝), 0.5 = 안전(가운데), 1 = 야성 한계.
    /// 코어가 이미 클램프하지만 여기서 한 번 더 자른다 — 클램프가 언젠가 풀리면
    /// 게이지만 조용히 화면 밖으로 나가기 때문이다.
    /// </summary>
    public static float MarkerPosition(float value)
    {
        float span = WildAxisCore.WildLimit - WildAxisCore.StarveLimit;
        float t = (value - WildAxisCore.StarveLimit) / span;

        return Clamp01(t);
    }

    /// <summary>안전을 기준으로 굶주림 쪽에 있는가 (색을 고르는 데 쓴다).</summary>
    public static bool LeansStarving(float value) => value < 0f;

    /// <summary>안전에서 얼마나 멀어졌는가 (0 = 중앙, 1 = 어느 쪽이든 한계). 색의 진하기.</summary>
    public static float DistanceFromSafe(float value)
        => Clamp01(Math.Abs(value) / WildAxisCore.WildLimit);

    static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
}
