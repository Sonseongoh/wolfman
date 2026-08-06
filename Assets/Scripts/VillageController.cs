using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 마을 씬 흐름 제어 (#11). HUD(금고 잔액)와 "라운드 종료" 버튼을 담당한다.
/// 시설 수리 실패 같은 안내 메시지도 여기로 모인다.
/// </summary>
public class VillageController : MonoBehaviour
{
    public static VillageController Instance { get; private set; }

    [Tooltip("안내 메시지가 화면에 남아있는 시간(초)")]
    public float messageDuration = 2.5f;

    string message;
    float messageTimer;

    void Awake()
    {
        Instance = this;
    }

    /// <summary>화면 상단에 붉은 안내 메시지 표시 (수리 실패 등). 마을 밖에서 부르면 조용히 무시된다.</summary>
    public static void ShowMessage(string msg)
    {
        if (Instance == null) return;

        Instance.message = msg;
        Instance.messageTimer = Instance.messageDuration;
    }

    void Update()
    {
        if (messageTimer > 0f) messageTimer -= Time.deltaTime;
    }

    // 임시 UI (WaveManager와 같은 OnGUI 방식 — Canvas 기반으로 교체 예정)
    void OnGUI()
    {
        GUIStyle title = new GUIStyle
        {
            fontSize = 26,
            alignment = TextAnchor.UpperCenter,
            normal = { textColor = Color.white }
        };
        GUI.Label(new Rect(0, 16, Screen.width, 40), "마을", title);

        // 안내 메시지 (수리 실패 등) — 끝날 때쯤 흐려진다
        if (messageTimer > 0f)
        {
            GUIStyle msgStyle = new GUIStyle
            {
                fontSize = 22,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter,
                normal = { textColor = new Color(1f, 0.4f, 0.35f, Mathf.Clamp01(messageTimer)) }
            };
            GUI.Label(new Rect(0, 84, Screen.width, 34), message, msgStyle);
        }

        DrawGoldPanel();

        // 라운드 종료 → MainScene. Phase는 Village 그대로 둔다 —
        // RoundController(#6)가 그걸 보고 "마을에서 무사히 라운드를 마쳤다"로 인식해
        // 임시 골드를 금고로 확정(#8)하고 정산 화면을 띄운 뒤 다음 라운드로 넘긴다
        float btnW = 200f, btnH = 55f;
        if (GUI.Button(new Rect(Screen.width * 0.5f - btnW * 0.5f, Screen.height * 0.82f, btnW, btnH), "라운드 종료"))
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("MainScene");
        }
    }

    /// <summary>우상단 재화 패널 (#8) — 수리 재원인 금고 잔액을 항상 보여준다</summary>
    void DrawGoldPanel()
    {
        if (CurrencyManager.Instance == null) return;

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

        // 주머니 (임시 — 금색)
        lbl.normal.textColor = new Color(1f, 0.82f, 0.2f);
        GUI.Label(new Rect(px + 10, py + 4, pw - 20, 28), "◈ 주머니", lbl);
        GUI.Label(new Rect(px + 10, py + 4, pw - 14, 28), $"{CurrencyManager.Instance.TempGold} G", val);

        // 구분선
        GUI.color = new Color(1f, 0.75f, 0.15f, 0.25f);
        GUI.DrawTexture(new Rect(px + 8, py + 36, pw - 16, 1), Texture2D.whiteTexture);
        GUI.color = Color.white;

        // 금고 (확정 — 하늘색). 시설 수리는 이 잔액에서만 나간다 (#11)
        lbl.normal.textColor = new Color(0.55f, 0.85f, 1f);
        GUI.Label(new Rect(px + 10, py + 40, pw - 20, 28), "◈ 금고", lbl);
        GUI.Label(new Rect(px + 10, py + 40, pw - 14, 28), $"{CurrencyManager.Instance.ConfirmedGold} G", val);
    }
}
