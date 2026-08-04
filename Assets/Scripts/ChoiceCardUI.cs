using UnityEngine;

/// <summary>
/// 가로 3장 카드 선택 UI 공용 그리기 (임시 OnGUI — 추후 Canvas UI로 교체).
/// SkillSystem(웨이브 스킬)과 PlayerLevel(레벨업)이 함께 사용.
/// </summary>
public static class ChoiceCardUI
{
    /// <summary>
    /// 어두운 배경 + 제목 + 카드 3장을 그린다.
    /// 마우스로 카드를 클릭하면 해당 인덱스(0~2), 아니면 -1 반환.
    /// </summary>
    public static int Draw(string title, string[] names, string[] descriptions)
    {
        // 배경 어둡게
        GUI.color = new Color(0f, 0f, 0f, 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        GUI.color = Color.white;

        GUIStyle titleStyle = new GUIStyle
        {
            fontSize = 34,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.yellow }
        };
        GUI.Label(new Rect(0, Screen.height * 0.16f, Screen.width, 50), title, titleStyle);

        // 카드 배치 (화면 크기 비례)
        float cardW = Mathf.Min(260f, Screen.width * 0.26f);
        float cardH = cardW * 1.3f;
        float gap = cardW * 0.12f;
        float totalW = cardW * 3f + gap * 2f;
        float x0 = (Screen.width - totalW) * 0.5f;
        float y = (Screen.height - cardH) * 0.5f;

        int clicked = -1;

        for (int i = 0; i < 3; i++)
        {
            Rect card = new Rect(x0 + i * (cardW + gap), y, cardW, cardH);
            bool hover = card.Contains(Event.current.mousePosition);

            // 테두리 (호버 시 밝게)
            Rect border = new Rect(card.x - 3, card.y - 3, card.width + 6, card.height + 6);
            GUI.color = hover ? Color.yellow : new Color(0.7f, 0.6f, 0.3f);
            GUI.DrawTexture(border, Texture2D.whiteTexture);

            // 카드 배경
            GUI.color = hover ? new Color(0.22f, 0.2f, 0.28f) : new Color(0.13f, 0.12f, 0.18f);
            GUI.DrawTexture(card, Texture2D.whiteTexture);
            GUI.color = Color.white;

            GUIStyle nameStyle = new GUIStyle
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            GUI.Label(new Rect(card.x + 10, card.y + cardH * 0.12f, cardW - 20, 60), names[i], nameStyle);

            GUIStyle descStyle = new GUIStyle
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = new Color(0.8f, 0.8f, 0.8f) }
            };
            GUI.Label(new Rect(card.x + 12, card.y + cardH * 0.4f, cardW - 24, cardH * 0.3f), descriptions[i], descStyle);

            GUIStyle keyStyle = new GUIStyle
            {
                fontSize = 20,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
            GUI.Label(new Rect(card.x, card.y + cardH - 40, cardW, 30), $"[ {i + 1} ]", keyStyle);

            // 카드 전체 = 투명 버튼 (클릭 선택)
            if (GUI.Button(card, GUIContent.none, GUIStyle.none))
                clicked = i;
        }

        return clicked;
    }
}
