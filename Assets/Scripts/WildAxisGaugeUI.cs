using UnityEngine;

/// <summary>
/// 야성·굶주림 축 게이지 (#117) — 좌측 상단 HP 라벨 바로 아래.
///
/// 값 하나에 임계선 둘이라 게이지도 하나다. 가운데가 안전(0), 왼쪽 끝이 굶주림 한계,
/// 오른쪽 끝이 야성 한계 — 어느 쪽으로 밀려나고 있는지가 마커 위치 하나로 읽혀야 한다.
///
/// 굶주림 단계의 페널티는 숫자로 적는다. 지금은 상점이 없어서(#13) "주민이 피한다"가
/// 아사에 들어섰다는 유일한 가시 신호다.
///
/// 언제 그릴지(연출 중 숨김 등)는 부르는 쪽이 정한다 — 여기는 그리기만 한다.
/// 다른 임시 UI 와 마찬가지로 OnGUI 기반이며, Canvas 로 갈 때 함께 옮긴다.
/// </summary>
public static class WildAxisGaugeUI
{
    // 기존 UI 팔레트: 굶주림은 붉은 계열(피격·경고), 야성은 보라 계열(포탈·달).
    static readonly Color StarveColor = new Color(1f, 0.4f, 0.3f);
    static readonly Color WildColor = new Color(0.8f, 0.6f, 1f);

    public static void Draw()
    {
        WildAxisManager axis = WildAxisManager.Instance;
        if (axis == null) return;

        const float x = 20f, y = 56f, w = 260f, h = 14f;

        // 어두운 바탕 + 옅은 테두리 (골드 패널과 같은 꼴)
        GUI.color = new Color(0.06f, 0.05f, 0.12f, 0.85f);
        GUI.DrawTexture(new Rect(x - 1, y - 1, w + 2, h + 2), Texture2D.whiteTexture);

        // 안전 쪽에서 멀어질수록 그 방향 색이 진해진다
        float t = Mathf.InverseLerp(WildAxisCore.StarveLimit, WildAxisCore.WildLimit, axis.Value);
        bool starving = axis.Value < 0f;
        float away = Mathf.Abs(axis.Value) / WildAxisCore.WildLimit;
        Color tint = Color.Lerp(new Color(0.5f, 0.5f, 0.55f), starving ? StarveColor : WildColor, away);

        // 채움: 중앙 눈금에서 지금 위치까지 — 어느 쪽으로 얼마나 밀렸는지가 길이로 보인다
        float centerX = x + w * 0.5f;
        float markerX = x + w * t;
        float fillLeft = Mathf.Min(centerX, markerX);
        GUI.color = new Color(tint.r, tint.g, tint.b, 0.55f);
        GUI.DrawTexture(new Rect(fillLeft, y, Mathf.Abs(markerX - centerX), h), Texture2D.whiteTexture);

        // 중앙 눈금 = 안전
        GUI.color = new Color(1f, 1f, 1f, 0.5f);
        GUI.DrawTexture(new Rect(centerX - 1, y - 2, 2, h + 4), Texture2D.whiteTexture);

        // 현재 위치 마커
        GUI.color = tint;
        GUI.DrawTexture(new Rect(markerX - 2, y - 3, 4, h + 6), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 단계 라벨 — 포식이면 적지 않는다 (아무 일도 없을 때 화면을 채우지 않는다)
        HungerStage stage = axis.Stage;
        if (stage == HungerStage.Sated) return;

        bool severe = stage == HungerStage.Starving || stage == HungerStage.Limit;
        string text = $"{StageName(stage)} — 공격 ×{axis.AttackMultiplier:0.##}";
        if (axis.MaxHpPenalty > 0) text += $", 최대 체력 −{axis.MaxHpPenalty}";
        if (severe) text += "   ◈ 주민이 피한다";

        GUIStyle label = new GUIStyle
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = severe ? StarveColor : new Color(0.85f, 0.8f, 0.8f) }
        };
        GUI.Label(new Rect(x, y + h + 2, 360f, 18f), text, label);
    }

    /// <summary>단계의 한국어 이름 (CONTEXT.md 용어집).</summary>
    static string StageName(HungerStage stage)
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
}
