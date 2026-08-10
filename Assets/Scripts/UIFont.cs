using UnityEngine;

/// <summary>
/// 게임 전체가 쓰는 한글 폰트 (#83).
///
/// 유니티 내장 폰트(LegacyRuntime)에는 한글 글리프가 없다. 에디터와 데스크톱 빌드는
/// OS 폰트로 폴백해 이 사실을 가려주지만, **WebGL 에는 폴백할 OS 폰트가 없다** —
/// 유니티는 없는 글자를 대체 문자로 그리지 않고 건너뛰므로 글자가 통째로 사라진다.
///
/// IMGUI 는 <c>GUIStyle.font</c> 가 비어 있으면 <c>GUI.skin.font</c> 로 폴백한다.
/// 그래서 여기 한 곳만 물려두면 OnGUI 화면 전부가 함께 해결되고, 새 화면이 생겨도
/// 별도 조치가 필요 없다. 화면마다 폰트를 지정하는 방식은 쓰지 말 것 —
/// 새 OnGUI 가 추가될 때마다 같은 버그가 되살아난다.
///
/// IMGUI 가 아닌 곳(<c>TextMesh</c>, <c>Text</c> 등)은 <see cref="Font"/> 를 직접 가져다 쓴다.
/// </summary>
[DefaultExecutionOrder(-10000)] // 다른 OnGUI 보다 먼저 돌아야 스킨이 미리 물린다
public class UIFont : MonoBehaviour
{
    /// <summary>Resources 아래의 폰트 파일 이름 (확장자 제외)</summary>
    const string ResourceName = "Galmuri11";

    static Font cached;

    /// <summary>
    /// 한글이 섞일 수 있는 텍스트에 쓸 폰트. 로드에 실패하면 내장 폰트로 떨어진다
    /// (그 경우 한글은 안 보이지만 숫자·영문은 살아 있어 화면이 통째로 비지는 않는다).
    /// </summary>
    public static Font Font
    {
        get
        {
            if (cached == null)
            {
                cached = Resources.Load<Font>(ResourceName);
                if (cached == null)
                {
                    Debug.LogWarning($"[UIFont] Resources/{ResourceName} 를 찾지 못했다. 한글이 표시되지 않는다.");
                    cached = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                }
            }
            return cached;
        }
    }

    /// <summary>
    /// 어느 씬에서 시작하든 한 번만 설치된다. 씬마다 오브젝트를 심지 않아도 되고,
    /// 씬 구성이 바뀌어도 따라 고칠 필요가 없다.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        GameObject go = new GameObject(nameof(UIFont));
        go.AddComponent<UIFont>();
        DontDestroyOnLoad(go);
    }

    void OnGUI()
    {
        // GUI.skin 은 프레임/이벤트마다 기본 스킨으로 돌아올 수 있어 매번 물린다.
        GUI.skin.font = Font;
    }
}
