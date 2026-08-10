using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 전역 일시정지 (#33 확장 — 어느 씬에서든 항상).
/// 우상단 ⏸ 버튼 또는 ESC로 토글. 씬 배치 불필요 — 자동 생성.
/// 다른 시스템이 시간을 멈춘 상태(스킬 선택·게임오버)나 달 연출 중엔 개입하지 않는다.
/// UI 디자인은 #33(PlayerHealth에 있던 것)을 그대로 옮겨왔다.
/// </summary>
public class PauseSystem : MonoBehaviour
{
    /// <summary>일시정지 중인지 (HitStop 등이 시간 정지 유지 판단에 사용)</summary>
    public static bool IsPaused { get; private set; }

    static Texture2D icon; // Resources/PauseBtn (없으면 텍스트 폴백)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindFirstObjectByType<PauseSystem>() != null) return;

        GameObject go = new GameObject("PauseSystem");
        DontDestroyOnLoad(go);
        go.AddComponent<PauseSystem>();
        icon = Resources.Load<Texture2D>("PauseBtn");
    }

    /// <summary>버튼·ESC를 받을 수 있는 상태인가 — 다른 UI가 시간을 멈췄으면 양보</summary>
    static bool CanPauseNow()
    {
        if (Time.timeScale == 0f) return false; // 스킬 선택·게임오버·히트스톱이 멈춘 상태
        if (WaveManager.Instance != null && WaveManager.Instance.IsInMoonReveal) return false;

        // 타이틀 화면에선 일시정지 개념이 없다 (아무곳-터치 시작과도 겹침)
        if (GameManager.Instance != null && GameManager.Instance.Phase == RoundPhase.Title) return false;

        return true;
    }

    static void Toggle()
    {
        if (!IsPaused)
        {
            if (!CanPauseNow()) return;
            IsPaused = true;
            Time.timeScale = 0f;
        }
        else
        {
            IsPaused = false;
            Time.timeScale = 1f;
        }
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Toggle();
    }

    void OnGUI()
    {
        if (IsPaused)
        {
            DrawPauseOverlay();
            return;
        }

        if (!CanPauseNow()) return;

        // ⏸ 버튼 — 우상단 골드 패널 왼쪽 (골드 패널이 없는 씬에서도 같은 자리)
        float bSize = 26f;
        float bx = Screen.width - 190f - 16f - bSize - 24f;
        float by = 12f;

        GUIContent content = icon != null ? new GUIContent(icon) : new GUIContent("II");
        if (GUI.Button(new Rect(bx, by, bSize, bSize), content, GUIStyle.none))
            Toggle();
    }

    static void DrawPauseOverlay()
    {
        // 거의 검정 오버레이
        GUI.color = new Color(0.04f, 0.04f, 0.06f, 0.9f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        // 상하 차가운 회색 라인
        GUI.color = new Color(0.5f, 0.5f, 0.55f, 0.5f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, 3), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(0, Screen.height - 3, Screen.width, 3), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 버튼 — 옵션 | 계속하기 (화면 비례로 모바일 대응)
        float btnW = Screen.width * 0.28f, btnH = Screen.height * 0.1f, gap = Screen.width * 0.04f;
        float totalW = btnW * 2 + gap;
        float bx = (Screen.width - totalW) * 0.5f;
        float by = (Screen.height - btnH) * 0.5f;

        GUIStyle btnStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = Mathf.RoundToInt(btnH * 0.38f),
            fontStyle = FontStyle.Bold
        };

        GUI.Button(new Rect(bx, by, btnW, btnH), "옵션", btnStyle);
        if (GUI.Button(new Rect(bx + btnW + gap, by, btnW, btnH), "계속하기", btnStyle))
            Toggle();
    }
}
