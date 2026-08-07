using UnityEngine;

/// <summary>
/// 우상단 재화 패널 (#8) — 주머니(임시)와 금고(확정)를 함께 보여준다.
/// 사냥(WaveManager)과 마을(VillageController)이 같은 패널을 쓰기 때문에 한 곳에 모았다.
/// 마을에서는 이 금고 잔액이 곧 시설 수리의 재원이다 (#11).
///
/// 언제 그릴지(연출 중 숨김 등)는 부르는 쪽이 정한다 — 여기는 그리기만 한다.
/// 다른 임시 UI 와 마찬가지로 OnGUI 기반이며, Canvas 로 갈 때 함께 옮긴다.
/// </summary>
public static class GoldPanelUI
{
    // 주머니 카운트업 연출: 실제 잔액으로 숫자가 굴러 올라가고, 오르는 동안 반짝인다.
    // (줄어들 땐 — 사망 손실·금고 확정 — 즉시 반영)
    static float shownTemp; // 화면에 표시 중인 주머니 값
    static float gainFlash; // 획득 반짝임 (1 → 0)

    public static void Draw()
    {
        if (CurrencyManager.Instance == null) return;

        // OnGUI는 프레임당 여러 번 불리므로 상태 갱신은 Repaint에서만
        if (Event.current.type == EventType.Repaint)
        {
            int target = CurrencyManager.Instance.TempGold;
            if (target < shownTemp)
            {
                shownTemp = target;
            }
            else if (target > shownTemp)
            {
                gainFlash = 1f;
                float speed = Mathf.Max(12f, (target - shownTemp) * 8f); // 많이 벌수록 빨리 굴러감
                shownTemp = Mathf.Min(target, shownTemp + speed * Time.unscaledDeltaTime);
            }
            if (gainFlash > 0f) gainFlash -= Time.unscaledDeltaTime * 2.5f;
        }

        float pw = 190f, ph = 72f;
        float px = Screen.width - pw - 16f, py = 12f;

        // 금색 테두리
        GUI.color = new Color(1f, 0.75f, 0.15f, 0.75f);
        GUI.DrawTexture(new Rect(px - 2, py - 2, pw + 4, ph + 4), Texture2D.whiteTexture);
        // 어두운 배경
        GUI.color = new Color(0.06f, 0.05f, 0.12f, 0.9f);
        GUI.DrawTexture(new Rect(px, py, pw, ph), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle lbl = new GUIStyle { fontSize = 17, alignment = TextAnchor.MiddleLeft };
        GUIStyle val = new GUIStyle { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white } };

        // 주머니 (임시 — 금색). 획득 직후엔 숫자가 커지며 밝게 반짝
        float flash = Mathf.Clamp01(gainFlash);
        lbl.normal.textColor = new Color(1f, 0.82f, 0.2f);

        GUIStyle tempVal = new GUIStyle(val)
        {
            fontSize = 17 + Mathf.RoundToInt(5f * flash),
            normal = { textColor = Color.Lerp(Color.white, new Color(1f, 0.95f, 0.5f), flash) }
        };

        GUI.Label(new Rect(px + 10, py + 4, pw - 20, 28), "◈ 주머니", lbl);
        GUI.Label(new Rect(px + 10, py + 4, pw - 14, 28), $"{Mathf.RoundToInt(shownTemp)} G", tempVal);

        // 구분선
        GUI.color = new Color(1f, 0.75f, 0.15f, 0.25f);
        GUI.DrawTexture(new Rect(px + 8, py + 36, pw - 16, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 금고 (확정 — 하늘색)
        lbl.normal.textColor = new Color(0.55f, 0.85f, 1f);
        GUI.Label(new Rect(px + 10, py + 40, pw - 20, 28), "◈ 금고", lbl);
        GUI.Label(new Rect(px + 10, py + 40, pw - 14, 28), $"{CurrencyManager.Instance.ConfirmedGold} G", val);
    }
}
