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
    MoonRevealUI reveal; // 라운드 시작 달 공개 + 사냥/마을 선택 (#103)

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // 마을 = 라운드 시작 허브 (#103): 새 라운드의 달을 여기서 공개하고
        // 사냥을 나갈지 마을에 남을지 고른다. 달이 없으면(마을 씬 단독 재생) 연출 생략
        if (GameManager.Instance != null && GameManager.Instance.CurrentMoon != null)
            reveal = gameObject.AddComponent<MoonRevealUI>();

        SoundManager.Instance?.PlayBGM(SoundManager.Instance.bgmVillage);
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

        // 달 공개가 끝나고 선택이 내려오면 처리 (#103)
        if (reveal != null && reveal.Result != MoonRevealUI.Choice.None)
        {
            MoonRevealUI.Choice pick = reveal.Result;
            Destroy(reveal);
            reveal = null;

            if (pick == MoonRevealUI.Choice.Hunt)
            {
                // 사냥을 고른 것을 페이즈에 남긴다 — 귀환 시 정산이 이 값으로 갈린다 (#78)
                GameManager.Instance?.SetPhase(RoundPhase.Hunt);
                Time.timeScale = 1f;
                SceneManager.LoadScene("HuntScene");
            }
            // Stay: Phase는 이미 Village — 그대로 마을 라운드 진행 (수리·라운드 종료)
        }
    }

    // 임시 UI (WaveManager와 같은 OnGUI 방식 — Canvas 기반으로 교체 예정)
    void OnGUI()
    {
        // 달 공개·선택 중엔 마을 UI를 비운다 (연출은 MoonRevealUI가 그림)
        if (reveal != null) return;

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

        // 수리 재원인 금고 잔액이 항상 보여야 한다 (#11)
        GoldPanelUI.Draw();

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

}
