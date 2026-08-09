using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleScreen : MonoBehaviour
{
    Texture2D texBg;
    Texture2D texWolf;
    Font fontCinzel;

    float elapsed;
    bool starting;
    float fadeAlpha;

    void Awake()
    {
        texBg     = Resources.Load<Texture2D>("TitleBg");
        texWolf   = Resources.Load<Texture2D>("TitleWolf");
        fontCinzel = Resources.Load<Font>("CinzelBold");
    }

    void Update()
    {
        elapsed += Time.unscaledDeltaTime;

        if (starting)
        {
            fadeAlpha = Mathf.MoveTowards(fadeAlpha, 1f, Time.unscaledDeltaTime * 1.2f);
            if (fadeAlpha >= 1f)
                SceneManager.LoadScene("SampleScene");
        }
    }

    void OnGUI()
    {
        float W = Screen.width, H = Screen.height;

        // 배경
        GUI.color = Color.white;
        if (texBg != null)
            GUI.DrawTexture(new Rect(0, 0, W, H), texBg, ScaleMode.ScaleAndCrop);
        else
        {
            GUI.color = new Color(0.04f, 0.05f, 0.08f, 1f);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // 늑대 실루엣 — 배경 바로 위, 텍스트보다 먼저 그려서 타이틀이 앞에 오게
        if (texWolf != null)
        {
            float wolfH = H * 0.58f;
            float wolfW = wolfH * ((float)texWolf.width / texWolf.height);
            float wx = (W - wolfW) * 0.5f;
            float wy = H - wolfH + H * 0.04f;
            GUI.color = new Color(0.10f, 0.12f, 0.17f, 0.92f);
            GUI.DrawTexture(new Rect(wx, wy, wolfW, wolfH), texWolf, ScaleMode.ScaleToFit);
            GUI.color = Color.white;
        }

        // WOLFMAN — 어두운 청회색 그림자 + 흰색
        int titleSize = Mathf.RoundToInt(H * 0.155f);
        GUIStyle shadow = new GUIStyle
        {
            font = fontCinzel,
            fontSize = titleSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.08f, 0.10f, 0.16f, 0.85f) }
        };
        GUIStyle title = new GUIStyle
        {
            font = fontCinzel,
            fontSize = titleSize,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.95f, 0.95f, 1f, 1f) }
        };
        GUI.Label(new Rect(5, H * 0.28f + 6, W, H * 0.22f), "WOLFMAN", shadow);
        GUI.Label(new Rect(0, H * 0.28f, W, H * 0.22f), "WOLFMAN", title);

        // 늑대인간 — 작은 한국어 서브 (Cinzel은 라틴 전용이라 서브는 기본 폰트)
        GUIStyle sub = new GUIStyle
        {
            fontSize = Mathf.RoundToInt(H * 0.028f),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(0.55f, 0.58f, 0.65f, 0.9f) }
        };
        GUI.Label(new Rect(0, H * 0.445f, W, H * 0.06f), "The village fears you. The night needs you.", sub);

        // 화면을 터치하세요
        if (!starting)
        {
            float alpha = (Mathf.Sin(elapsed * 1.4f) + 1f) * 0.3f + 0.15f;
            GUIStyle tap = new GUIStyle
            {
                font = fontCinzel,
                fontSize = Mathf.RoundToInt(H * 0.025f),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = new Color(0.55f, 0.57f, 0.62f, alpha) }
            };
            float btnW = W * 0.6f, btnH = H * 0.08f;
            float bx = (W - btnW) * 0.5f, by = H * 0.88f;
            if (GUI.Button(new Rect(bx, by, btnW, btnH), "화면을 터치하세요", tap))
            {
                starting = true;
                if (GameManager.Instance != null)
                    GameManager.Instance.StartNextRound();
            }
        }

        // 페이드 아웃
        if (starting)
        {
            GUI.color = new Color(0f, 0f, 0f, fadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
        }
    }
}
