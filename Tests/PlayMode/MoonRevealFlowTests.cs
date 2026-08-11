using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// 라운드 한 바퀴를 실제로 밟아보는 회귀 테스트.
///
/// 이 프로젝트의 도메인 테스트(Tests/Wolfman.Domain.Tests)는 UnityEngine 없이 도는 순수 C# 이라
/// "씬을 넘나들 때 무엇이 살아남는가" 를 볼 수 없다. 그런데 이 게임의 흐름 버그는 바로 거기서 났다:
/// 사냥 씬의 GameManager 오브젝트가 중복으로 통째로 파괴되면서, 같이 붙어 있던 SoundManager 까지
/// 끌려 죽었고, 파괴된 싱글턴을 부른 달 공개 코루틴이 예외로 죽어 사냥이 시작되지 않았다.
///
/// 씬 로드·컴포넌트 수명·코루틴이 다 얽힌 자리라 PlayMode 테스트만이 맞는 seam 이다.
///
/// ## 이 파일은 `Assets/` 안에 두면 안 된다 (#108)
///
/// asmdef 없이 Assembly-CSharp 의 게임 스크립트를 보려면 `playModeTestRunnerEnabled` 가
/// 켜져 있어야 한다. 유니티가 deprecated 처리한 설정이라 저장소에서는 꺼두는데,
/// **끈 채로 이 파일이 `Assets/` 안에 있으면 프로젝트가 아예 컴파일되지 않는다.**
///
///   -define:UNITY_INCLUDE_TESTS        ← 이 define 은 플래그와 무관하게 항상 켜져 있다
///   참조: nunit.framework.dll 뿐        ← UnityEngine.TestRunner.dll 은 빠진다
///
/// 즉 `#if UNITY_INCLUDE_TESTS` 로는 막을 수 없다 — 그 가드는 통과하고, 정작 `UnityTest`·
/// `UnitySetUp` 이 없어서 Assembly-CSharp 가 통째로 터진다. 한 번 그렇게 dev 를 깨뜨렸다.
///
/// 그래서 소스는 `Assets/` 바깥인 여기 둔다. 돌릴 때만 **버리는 사본**의 `Assets/Tests/PlayMode/`
/// 로 복사해 넣고, 그 사본에서만 플래그를 켠다:
///
///   cp -r Tests/PlayMode &lt;사본&gt;/Assets/Tests/PlayMode
///   sed -i 's/playModeTestRunnerEnabled: 0/playModeTestRunnerEnabled: 1/' &lt;사본&gt;/ProjectSettings/ProjectSettings.asset
///   Unity.exe -batchmode -nographics -projectPath &lt;사본&gt; -runTests -testPlatform PlayMode
///
/// 배치 규칙 쪽 회귀는 여기 말고 `Tests/Wolfman.Domain.Tests/SceneLifetimeCompositionTests.cs`
/// 가 지킨다 — 그쪽은 설정도 유니티도 없이 WSL 에서 매번 돈다.
///
/// 버튼은 OnGUI 라 배치모드에서 누를 수 없으니, 각 버튼 핸들러와 똑같은 상태 변경을 직접 일으킨다.
/// 지나가는 코드 경로는 손으로 누를 때와 같다.
/// </summary>
public class MoonRevealFlowTests
{
    const float Timeout = 30f;

    /// <summary>마을을 떠난 뒤 달이 몇 번 추첨됐는가 (#78 — 한 바퀴에 정확히 한 번이어야 한다)</summary>
    int moonDraws;

    /// <summary>
    /// PlayMode 테스트는 한 도메인을 공유한다. DontDestroyOnLoad 로 살아남은 싱글턴이 다음 테스트로
    /// 새어 들어가면 두 번째 테스트는 새 런이 아니게 된다 — Phase 가 Title 이 아니라서 TitleScreen 이
    /// Awake 에서 스스로를 지워버리고, 엉뚱한 실패가 난다. 매 테스트를 첫 실행처럼 만든다.
    /// </summary>
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

        foreach (System.Type type in new[]
                 {
                     typeof(GameManager), typeof(SoundManager), typeof(CurrencyManager),
                     typeof(WaveManager), typeof(VillageController), typeof(SkillSystem),
                 })
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
    /// 씬을 넘어 살아남아야 하는 싱글턴이 "파괴됐지만 참조는 남은" 상태로 방치되면 안 된다.
    ///
    /// C# 의 ?. 는 유니티가 오버로딩한 == 를 타지 않는다. 그래서 SoundManager.Instance?.PlaySlot()
    /// 처럼 얌전해 보이는 호출이 파괴된 오브젝트로 그대로 들어가 MissingReferenceException 을 던지고,
    /// 그게 코루틴 안이면 코루틴이 통째로 죽는다 — 게임은 아무 말 없이 멈춘다.
    /// </summary>
    [UnityTest]
    public IEnumerator 사냥_씬에_들어가도_사운드_싱글턴은_살아있다()
    {
        yield return StartRunAndReachHunt();

        Assert.IsFalse(ReferenceEquals(SoundManager.Instance, null),
            "SoundManager.Instance 가 아예 없다 — 사냥 씬에 사운드가 붙어 있지 않다");

        Assert.IsTrue(SoundManager.Instance != null,
            "SoundManager.Instance 가 파괴된 오브젝트를 가리킨다. " +
            "?. 는 이걸 걸러내지 못하므로 호출하는 쪽이 전부 MissingReferenceException 을 맞는다.");

        // ?. 없이 부른다. ?. 를 붙이면 Instance 가 null 을 돌려주는 순간 호출 자체가 사라져서
        // "예외가 안 났다" 가 공허한 참이 된다 — 소리가 죽어도 초록이 된다는 뜻이다.
        Assert.DoesNotThrow(() => SoundManager.Instance.PlaySlot(),
            "달 슬롯 효과음 호출이 예외를 던진다 — 이 호출은 달 공개 코루틴 안에 있어서 흐름을 죽인다");
    }

    /// <summary>
    /// 사용자가 손으로 밟은 경로를 그대로 밟는다:
    ///   타이틀 → 마을 → "라운드 종료" → 정산(클리어) → 사냥 씬 → 달 슬롯 → "사냥 나가기" → 스폰
    /// </summary>
    [UnityTest]
    public IEnumerator 마을에서_라운드를_끝내면_달이_슬롯으로_돌고_사냥이_시작된다()
    {
        yield return StartRunAndReachHunt();

        // ── 달 공개 연출을 프레임 단위로 관찰 ───────────────────────────
        WaveManager wm = null;
        var slotNames = new HashSet<string>();
        bool sawSpinning = false;
        bool sawBanner = false;
        float t = 0f;

        while (t < Timeout)
        {
            if (wm == null) wm = WaveManager.Instance;
            if (wm != null)
            {
                if (Get<bool>(wm, "moonSpinning")) sawSpinning = true;
                if (Get<float>(wm, "moonBannerTimer") > 0f) sawBanner = true;

                string shown = Get<string>(wm, "spinDisplayName");
                if (!string.IsNullOrEmpty(shown)) slotNames.Add(shown);

                if (wm.IsWaitingForAction) break;
            }
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Assert.NotNull(wm, "사냥 씬에 WaveManager 가 없다");
        Assert.IsTrue(wm.IsWaitingForAction,
            $"달 공개가 끝나고 사냥/마을 선택이 뜨지 않았다 " +
            $"(wave={wm.CurrentWave}, spinning={Get<bool>(wm, "moonSpinning")}, " +
            $"banner={Get<float>(wm, "moonBannerTimer"):0.00}, timeScale={Time.timeScale})");

        // 증상 1 — 달은 슬롯이 돌다가 1개로 확정돼야 한다
        Assert.IsTrue(sawSpinning, "달 슬롯 연출이 한 프레임도 켜지지 않았다");
        Assert.That(slotNames.Count, Is.GreaterThan(1),
            $"슬롯이 돌지 않았다 — 연출 동안 표시된 달 이름이 {slotNames.Count}종뿐이다 " +
            $"({string.Join(", ", slotNames)}). 슬롯 첫 칸에서 멈춘 것이다.");
        Assert.IsTrue(sawBanner, "확정된 달 배너가 뜨지 않았다");
        Assert.NotNull(GameManager.Instance.CurrentMoon, "확정된 달이 없다");

        // 슬롯이 도는 사이에 달을 다시 뽑지 않았는가 (#78). WaveManager 가 라운드를 한 번 더
        // 시작하면 앞서 확정된 달을 덮어쓴다 — 카드에 뜬 달과 실제로 적용되는 달이 갈라진다.
        Assert.That(moonDraws, Is.EqualTo(1),
            $"라운드 한 바퀴에 달이 정확히 한 번 추첨돼야 하는데 {moonDraws}번 뽑혔다 (#78)");

        // ── "사냥 나가기" (WaveManager.OnGUI 버튼과 동일) ─────────────────
        Set(wm, "waitingForAction", false);
        GameManager.Instance.SetPhase(RoundPhase.Hunt);

        // 증상 2 — 사냥이 진행돼야 한다 = 적이 실제로 스폰된다
        float t2 = 0f;
        while (t2 < Timeout && wm.AliveCount <= 0)
        {
            t2 += Time.unscaledDeltaTime;
            yield return null;
        }

        Assert.That(wm.AliveCount, Is.GreaterThan(0),
            $"'사냥 나가기' 를 눌렀는데 적이 한 마리도 스폰되지 않았다 — 사냥이 진행되지 않는다 " +
            $"(timeScale={Time.timeScale}, phase={GameManager.Instance.Phase}, wave={wm.CurrentWave})");
    }

    /// <summary>
    /// 그 밤의 달은 사냥터의 조명까지 정한다 (#25) — 블러드문이면 붉은 밤, 블랙문이면 칠흑.
    ///
    /// 그런데 조명은 씬을 넘어 살아남으면 안 된다. MoonEffects 가 붙잡는 Global Light 2D 는
    /// 그 씬의 것이라, 사냥 씬을 나가면 같이 사라져야 한다. 반대로 달은 씬보다 먼저 정해진다 —
    /// 정산 허브에서 뽑히고, 그 다음에 사냥 씬이 로드된다. 이 둘의 순서가 이 테스트의 핵심이다.
    /// </summary>
    [UnityTest]
    public IEnumerator 사냥터_조명이_그_밤의_달을_따른다()
    {
        yield return StartRunAndReachHunt();

        MoonEffects fx = Object.FindFirstObjectByType<MoonEffects>();
        Assert.IsTrue(fx != null,
            "사냥 씬에 MoonEffects 가 살아있지 않다 — 중복 GameManager 오브젝트에 얹혀 있다가 함께 파괴됐다");
        Assert.IsTrue(fx.globalLight != null,
            "MoonEffects 에 씬의 Global Light 2D 가 연결돼 있지 않다");

        MoonData moon = GameManager.Instance.CurrentMoon;
        Assert.NotNull(moon, "확정된 달이 없다");

        // 먼저 "달을 받았는가" 부터 본다. 조명이 수렴하기를 기다리면 두 가지 실패가 뭉개진다 —
        // 컴포넌트가 죽은 것과, 살아있지만 달을 못 받은 것.
        Color target = Get<Color>(fx, "targetColor");
        Assert.That(ColorGap(target, moon.ambientColor), Is.LessThan(0.01f),
            $"MoonEffects 가 이번 달({moon.moonName})을 받지 못했다 — 목표 색이 {target} 에 머물러 있다. " +
            "달은 사냥 씬이 로드되기 전에 뽑히므로, 로드된 뒤에 OnMoonRevealed 를 구독해봐야 이미 지나간 뒤다.");

        // 그 다음 실제 조명이 그 값으로 수렴하는지 (transitionTime 1.5초)
        float t = 0f;
        while (t < 8f)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }

        Assert.That(ColorGap(fx.globalLight.color, moon.ambientColor), Is.LessThan(0.05f),
            $"조명 색이 {moon.moonName} 의 분위기로 가지 않았다 " +
            $"(현재 {fx.globalLight.color}, 기대 {moon.ambientColor})");
        Assert.That(fx.globalLight.intensity, Is.EqualTo(moon.ambientIntensity).Within(0.05f),
            $"조명 밝기가 {moon.moonName} 의 값으로 가지 않았다");
    }

    /// <summary>알파는 조명에 쓰이지 않으므로 RGB 만 본다.</summary>
    static float ColorGap(Color a, Color b)
        => Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b);

    // ── 공통 경로 ─────────────────────────────────────────────────────────

    /// <summary>타이틀에서 새 런을 시작해 마을 → 라운드 종료 → 정산을 거쳐 사냥 씬까지 간다.</summary>
    IEnumerator StartRunAndReachHunt()
    {
        SceneManager.LoadScene("TitleScene");
        yield return WaitForScene("TitleScene");

        var title = Object.FindFirstObjectByType<TitleScreen>();
        Assert.NotNull(title, "TitleScene 에 TitleScreen 이 없다");

        // 아무 입력에나 불리는 진입점 — 배치모드엔 입력이 없으니 같은 함수를 직접 부른다
        Call(title, "StartGame");
        yield return WaitForScene("VillageScene");

        Assert.NotNull(GameManager.Instance, "GameManager 가 살아있지 않다");
        Assert.AreEqual(RoundPhase.Village, GameManager.Instance.Phase,
            "타이틀을 지나면 마을 페이즈여야 한다");
        int roundInVillage = GameManager.Instance.RoundNumber;

        // 이 지점부터 달이 몇 번 뽑히는지 센다. 한 바퀴에 정확히 한 번이어야 한다 (#78)
        moonDraws = 0;
        GameManager.Instance.OnMoonRevealed += CountMoonDraw;

        // "라운드 종료" (VillageController.OnGUI 버튼과 동일)
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainScene");
        yield return WaitForScene("MainScene");

        // 정산 허브가 "라운드 클리어!" 를 3초 보여준 뒤 사냥 씬으로 넘긴다
        yield return WaitForScene("HuntScene");

        Assert.That(GameManager.Instance.RoundNumber, Is.EqualTo(roundInVillage + 1),
            $"라운드 한 바퀴에 라운드 번호가 정확히 1 올라야 한다 (#78) — " +
            $"마을에서 {roundInVillage} 였는데 사냥 씬에서 {GameManager.Instance.RoundNumber} 다.");
    }

    void CountMoonDraw(MoonData _) => moonDraws++;

    static IEnumerator WaitForScene(string name)
    {
        float t = 0f;
        while (SceneManager.GetActiveScene().name != name && t < Timeout)
        {
            t += Time.unscaledDeltaTime;
            yield return null;
        }
        Assert.AreEqual(name, SceneManager.GetActiveScene().name,
            $"{Timeout}초 안에 {name} 으로 넘어가지 않았다 (timeScale={Time.timeScale})");
        yield return null; // Awake 뒤 Start 가 도는 프레임을 하나 준다
    }

    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static T Get<T>(object target, string field)
        => (T)target.GetType().GetField(field, Any).GetValue(target);

    static void Set(object target, string field, object value)
        => target.GetType().GetField(field, Any).SetValue(target, value);

    static void Call(object target, string method)
        => target.GetType().GetMethod(method, Any).Invoke(target, null);
}
