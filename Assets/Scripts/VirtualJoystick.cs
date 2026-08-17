using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 모바일 플로팅 가상 조이스틱 (#63).
/// 화면 아무 데나 터치하면 그 지점에 스틱이 생기고, 드래그 방향으로 이동. 떼면 사라진다.
/// PlayerMovement가 VirtualJoystick.Direction을 읽는다 (키보드 입력이 우선).
/// 에디터/PC에선 마우스 드래그로 테스트 가능. 씬 배치 불필요 — 자동 생성.
/// </summary>
public class VirtualJoystick : MonoBehaviour
{
    /// <summary>현재 스틱 방향 (길이 0~1, 아날로그). 스틱을 안 쓰는 중엔 zero</summary>
    public static Vector2 Direction { get; private set; }

    [Tooltip("스틱 최대 반경 (화면 높이 비례 — 0.09 = 화면 높이의 9%)")]
    public float radiusRatio = 0.09f;

    [Tooltip("데드존 — 이 비율 이하의 기울임은 무시 (손떨림 방지)")]
    public float deadZone = 0.15f;

    bool active;
    Vector2 originGui; // 스틱 중심 (GUI 좌표 — 위가 0)
    Vector2 knobGui;   // 노브 위치 (GUI 좌표)

    static Texture2D discTex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoSpawn()
    {
        if (FindAnyObjectByType<VirtualJoystick>() != null) return;

        GameObject go = new GameObject("VirtualJoystick");
        DontDestroyOnLoad(go);
        go.AddComponent<VirtualJoystick>();
    }

    void Update()
    {
        // 시간 정지(카드 선택·게임오버) 중엔 스틱 해제 — 카드 터치와 안 섞이게
        if (Time.timeScale == 0f)
        {
            active = false;
            Direction = Vector2.zero;
            return;
        }

        bool pressed = false;
        Vector2 pos = Vector2.zero; // 입력 좌표 (아래가 0)

        Touchscreen ts = Touchscreen.current;
        if (ts != null && ts.primaryTouch.press.isPressed)
        {
            pressed = true;
            pos = ts.primaryTouch.position.ReadValue();
        }
        else if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            pressed = true; // 에디터/PC 테스트용
            pos = Mouse.current.position.ReadValue();
        }

        if (!pressed)
        {
            active = false;
            Direction = Vector2.zero;
            return;
        }

        Vector2 gui = new Vector2(pos.x, Screen.height - pos.y);

        if (!active)
        {
            active = true;
            originGui = gui; // 누른 자리가 스틱 중심
        }

        float radius = Screen.height * radiusRatio;
        Vector2 delta = gui - originGui;
        if (delta.magnitude > radius) delta = delta.normalized * radius;
        knobGui = originGui + delta;

        // GUI 좌표는 y가 반대라 이동 방향으로 뒤집어서 내보낸다
        Vector2 dir = new Vector2(delta.x, -delta.y) / radius;
        float mag = dir.magnitude;
        Direction = mag < deadZone
            ? Vector2.zero
            : dir.normalized * Mathf.Clamp01((mag - deadZone) / (1f - deadZone));
    }

    void OnGUI()
    {
        if (!active) return;
        if (discTex == null) discTex = MakeDisc(64);

        float radius = Screen.height * radiusRatio;

        // 바닥 원 (은은하게)
        GUI.color = new Color(1f, 1f, 1f, 0.14f);
        GUI.DrawTexture(
            new Rect(originGui.x - radius, originGui.y - radius, radius * 2f, radius * 2f), discTex);

        // 노브 (진하게)
        float knobR = radius * 0.4f;
        GUI.color = new Color(1f, 1f, 1f, 0.45f);
        GUI.DrawTexture(
            new Rect(knobGui.x - knobR, knobGui.y - knobR, knobR * 2f, knobR * 2f), discTex);

        GUI.color = Color.white;
    }

    /// <summary>부드러운 원형 텍스처를 런타임에 생성 (에셋 불필요)</summary>
    static Texture2D MakeDisc(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float c = (size - 1) * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c)) / c;
                float a = 1f - Mathf.SmoothStep(0.85f, 1f, d); // 가장자리만 살짝 페더
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }
}
