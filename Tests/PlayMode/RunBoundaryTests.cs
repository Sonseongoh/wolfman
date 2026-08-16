using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// 런 경계 회귀 테스트 (#121).
///
/// 도메인 테스트(Tests/Wolfman.Domain.Tests)는 GoldWallet.ResetRun 의 산수만 본다.
/// "타이틀을 다시 지나면 정말 지워지는가", "죽음은 정말 지우지 않는가"는
/// DontDestroyOnLoad 수명과 씬 전환이 얽힌 자리라 PlayMode 만이 맞는 seam 이다.
///
/// 이 파일은 Assets/ 안에 두면 안 된다 (#108) — 돌릴 때만 버리는 사본의
/// Assets/Tests/PlayMode/ 로 복사한다. 실행 방법은 MoonRevealFlowTests.cs 머리 주석 참고.
/// 버튼은 OnGUI 라 배치모드에서 못 누르니, 각 버튼·죽음 경로와 동일한 진입점을 직접 부른다.
/// </summary>
public class RunBoundaryTests : PlayModeTestBase
{
    /// <summary>
    /// AC 2·3 (#121): 타이틀 → 진행(밤 2, 금고 300G) → 런 종료 → 타이틀 → 새 시작이
    /// 이전 런을 물려받지 않는다. "런 종료 → 타이틀"은 #12 가 밟게 될 경로 그대로 —
    /// Phase 를 Title 로 되돌리고 타이틀 씬을 올리는 것뿐, 지우기는 다음 StartRun 이 한다.
    /// </summary>
    [UnityTest]
    public IEnumerator 타이틀에서_다시_시작하면_이전_런의_금고와_라운드를_물려받지_않는다()
    {
        yield return StartRunToVillage();

        // 이전 런의 흔적을 만든다: 금고 300G + 밤 2까지 진행
        CurrencyManager.Instance.AddTempGold(300);
        CurrencyManager.Instance.BankGold();
        GameManager.Instance.StartNextRound(); // 밤 하나 소모 (죽음·정산이 쓰는 것과 같은 진입점)
        GameManager.Instance.SetPhase(RoundPhase.Village);

        Assert.That(GameManager.Instance.RoundNumber, Is.EqualTo(2), "전제가 깨졌다 — 밤 2여야 한다");
        Assert.That(CurrencyManager.Instance.ConfirmedGold, Is.EqualTo(300), "전제가 깨졌다 — 금고 300G 여야 한다");

        // 런 종료 → 타이틀 복귀
        GameManager.Instance.SetPhase(RoundPhase.Title); // Title 이 아니면 TitleScreen 이 Awake 에서 스스로를 지운다
        SceneManager.LoadScene("TitleScene");
        yield return WaitForScene("TitleScene");

        // 새 시작
        yield return StartRunToVillage();

        Assert.That(GameManager.Instance.RoundNumber, Is.EqualTo(1),
            "새 런은 밤 1부터 세야 한다 — 이전 런의 라운드 번호를 물려받았다 (#121)");
        Assert.That(CurrencyManager.Instance.ConfirmedGold, Is.EqualTo(0),
            "이전 런의 금고가 새 런으로 넘어왔다 — 런이 끝나면 금고 골드는 사라져야 한다 (#121)");
        Assert.That(CurrencyManager.Instance.TempGold, Is.EqualTo(0),
            "이전 런의 주머니가 새 런으로 넘어왔다 (#121)");
    }

    /// <summary>
    /// AC 4 (#121): 죽음(#113)은 런을 끝내지 않는다 — 주머니만 잃고 밤이 넘어가며 금고는 남는다.
    /// PlayerHealth 의 죽음 처리(LoseTempGold)와 "마을로 돌아가기" 버튼(ReturnToVillage)이
    /// 부르는 진입점을 그대로 부른다. 리셋이 StartNextRound 쪽에 잘못 들어가면 여기서 잡힌다.
    /// </summary>
    [UnityTest]
    public IEnumerator 죽음은_런을_끝내지_않는다_금고는_남고_밤만_소모된다()
    {
        yield return StartRunToVillage();

        CurrencyManager.Instance.AddTempGold(100);
        CurrencyManager.Instance.BankGold();       // 금고 100G
        CurrencyManager.Instance.AddTempGold(40);  // 그 밤의 주머니 40G
        int nightBeforeDeath = GameManager.Instance.RoundNumber;

        // 죽음의 결과 (#113): 주머니 손실 → "마을로 돌아가기"
        CurrencyManager.Instance.LoseTempGold();
        CallStatic(typeof(PlayerHealth), "ReturnToVillage");
        yield return WaitForScene("VillageScene");

        Assert.That(CurrencyManager.Instance.ConfirmedGold, Is.EqualTo(100),
            "죽음이 금고를 지웠다 — 죽음은 런의 끝이 아니다 (ADR 0004, #121 AC 4)");
        Assert.That(CurrencyManager.Instance.TempGold, Is.EqualTo(0),
            "그 밤의 주머니는 잃어야 한다 (#113)");
        Assert.That(GameManager.Instance.RoundNumber, Is.EqualTo(nightBeforeDeath + 1),
            "죽음은 밤을 소모한다 — 라운드가 정확히 하나 넘어가야 한다 (#113)");
    }

    // ── 공통 경로 (MoonRevealFlowTests 와 같은 방식) ─────────────────────────

    /// <summary>타이틀 씬을 올리고 TitleScreen.StartGame(모든 입력의 공용 진입점)으로 마을까지 간다.</summary>
    IEnumerator StartRunToVillage()
    {
        SceneManager.LoadScene("TitleScene");
        yield return WaitForScene("TitleScene");

        var title = Object.FindFirstObjectByType<TitleScreen>();
        Assert.NotNull(title, "TitleScene 에 TitleScreen 이 없다");

        Call(title, "StartGame");
        yield return WaitForScene("VillageScene");

        Assert.NotNull(GameManager.Instance, "GameManager 가 살아있지 않다");
        Assert.AreEqual(RoundPhase.Village, GameManager.Instance.Phase, "타이틀을 지나면 마을 페이즈여야 한다");
    }
}
