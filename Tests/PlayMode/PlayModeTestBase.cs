using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode 테스트 공용 베이스 (#137).
///
/// PlayMode 테스트는 한 도메인을 공유한다. DontDestroyOnLoad 로 살아남은 싱글턴이 다음 테스트로
/// 새어 들어가면 두 번째 테스트는 새 런이 아니게 된다 — Phase 가 Title 이 아니라서 TitleScreen 이
/// Awake 에서 스스로를 지워버리고, 엉뚱한 실패가 난다. SetUp 이 매 테스트를 첫 실행처럼 만든다.
///
/// 정리할 싱글턴 목록은 이 파일의 SingletonTypes 가 유일하다 — 새 DontDestroyOnLoad 싱글턴이
/// 생기면 여기에만 추가한다. 두 테스트 파일에 통째로 복제돼 있던 것을 모았다 (#137).
///
/// 이 파일도 Assets/ 안에 두면 안 된다 (#108) — 돌릴 때만 버리는 사본의 Assets/Tests/PlayMode/
/// 로 함께 복사된다 (cp -r 이 디렉토리째 가져간다). 실행 방법은 MoonRevealFlowTests.cs 머리 주석 참고.
/// </summary>
public abstract class PlayModeTestBase
{
    protected const float Timeout = 30f;

    /// <summary>씬을 넘어 살아남는(DontDestroyOnLoad) 싱글턴 — 테스트 사이에 정리할 전체 목록.</summary>
    static readonly System.Type[] SingletonTypes =
    {
        typeof(GameManager), typeof(SoundManager), typeof(CurrencyManager),
        typeof(WaveManager), typeof(VillageController), typeof(SkillSystem),
    };

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        foreach (Transform t in Object.FindObjectsByType<Transform>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t == null || t.parent != null) continue;
            if (t.gameObject.scene.name == "DontDestroyOnLoad")
                Object.DestroyImmediate(t.gameObject);
        }

        foreach (System.Type type in SingletonTypes)
            ClearStaticInstance(type);

        Time.timeScale = 1f;
        yield return null;
    }

    static void ClearStaticInstance(System.Type type)
    {
        const BindingFlags S = BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
        foreach (string name in new[] { "<Instance>k__BackingField", "_instance", "instance" })
        {
            FieldInfo f = type.GetField(name, S);
            if (f != null) { f.SetValue(null, null); return; }
        }
    }

    /// <summary>
    /// done 이 참이 될 때까지 프레임마다 기다린다 — 최대 Timeout 초. 시간이 다 돼도 던지지 않는다:
    /// 무엇이 잘못됐는지는 호출부가 자기 문맥으로 단언해야 실패 메시지가 쓸모 있기 때문이다.
    /// done 안에서 프레임마다 관찰(연출이 한 번이라도 켜졌는지 등)을 겸해도 된다.
    /// </summary>
    protected static IEnumerator WaitUntil(System.Func<bool> done)
    {
        float t = 0f;
        while (t < Timeout)
        {
            if (done()) yield break;
            t += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    protected static IEnumerator WaitForScene(string name)
    {
        yield return WaitUntil(() => SceneManager.GetActiveScene().name == name);

        Assert.AreEqual(name, SceneManager.GetActiveScene().name,
            $"{Timeout}초 안에 {name} 으로 넘어가지 않았다 (timeScale={Time.timeScale})");
        yield return null; // Awake 뒤 Start 가 도는 프레임을 하나 준다
    }

    // ── 리플렉션 헬퍼 — OnGUI 버튼은 배치모드에서 못 누르니 같은 진입점을 직접 부른다 ──

    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    protected static T Get<T>(object target, string field)
        => (T)target.GetType().GetField(field, Any).GetValue(target);

    protected static void Call(object target, string method)
        => target.GetType().GetMethod(method, Any).Invoke(target, null);

    protected static void CallStatic(System.Type type, string method)
        => type.GetMethod(method, BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public)
               .Invoke(null, null);
}
