using NUnit.Framework;

/// <summary>
/// 게이지가 무엇을 보여줄지 (#117).
///
/// 이 판단이 <c>WildAxisGaugeUI.Draw()</c> 안에 있었을 때는 검사할 방법이 없었다 —
/// 배치모드 유니티에서는 그래픽을 켜도 <c>OnGUI</c> 가 아예 돌지 않아
/// (실측: 0.5초 동안 호출 0회) 라벨 문구도 마커 위치도 확인할 수 없었다.
/// 그리기에서 판단을 떼어내니 여기서 볼 수 있게 됐다.
/// </summary>
[TestFixture]
public class WildAxisGaugeTextTests
{
    [TestCase(HungerStage.Sated, "포식")]
    [TestCase(HungerStage.Hungry, "허기")]
    [TestCase(HungerStage.Famished, "주림")]
    [TestCase(HungerStage.Starving, "아사")]
    [TestCase(HungerStage.Limit, "한계")]
    public void 단계_이름은_용어집을_따른다(HungerStage stage, string expected)
    {
        // CONTEXT.md 용어집의 다섯 단어. 여기서 어긋나면 화면과 문서가 서로 다른 말을 한다.
        Assert.That(WildAxisGaugeText.StageName(stage), Is.EqualTo(expected));
    }

    [Test]
    public void 포식일_때는_아무것도_적지_않는다()
    {
        // 아무 일도 없을 때 화면을 채우지 않는다.
        Assert.That(WildAxisGaugeText.ShowsLabel(HungerStage.Sated), Is.False);
        Assert.That(WildAxisGaugeText.Label(HungerStage.Sated, 1f, 0), Is.Empty);
    }

    [TestCase(HungerStage.Hungry)]
    [TestCase(HungerStage.Famished)]
    [TestCase(HungerStage.Starving)]
    [TestCase(HungerStage.Limit)]
    public void 포식이_아니면_적는다(HungerStage stage)
    {
        Assert.That(WildAxisGaugeText.ShowsLabel(stage), Is.True);
    }

    [Test]
    public void 아사부터_붉게_경고한다()
    {
        // 상점이 아직 없어서(#13) 이 경고가 아사에 들어섰다는 유일한 가시 신호다.
        Assert.That(WildAxisGaugeText.IsSevere(HungerStage.Sated), Is.False);
        Assert.That(WildAxisGaugeText.IsSevere(HungerStage.Hungry), Is.False);
        Assert.That(WildAxisGaugeText.IsSevere(HungerStage.Famished), Is.False);
        Assert.That(WildAxisGaugeText.IsSevere(HungerStage.Starving), Is.True);
        Assert.That(WildAxisGaugeText.IsSevere(HungerStage.Limit), Is.True);
    }

    [Test]
    public void 허기는_공격력만_적는다()
    {
        Assert.That(WildAxisGaugeText.Label(HungerStage.Hungry, 0.85f, 0),
            Is.EqualTo("허기 — 공격 ×0.85"));
    }

    [Test]
    public void 최대_체력이_깎이면_함께_적는다()
    {
        Assert.That(WildAxisGaugeText.Label(HungerStage.Famished, 0.70f, 1),
            Is.EqualTo("주림 — 공격 ×0.7, 최대 체력 −1"));
    }

    [Test]
    public void 아사는_주민이_피한다까지_적는다()
    {
        // 상점이 닫혔다는 것을 문장으로 알린다 — 규칙으로 "나가라"고 하지 않는다.
        Assert.That(WildAxisGaugeText.Label(HungerStage.Starving, 0.50f, 2),
            Is.EqualTo("아사 — 공격 ×0.5, 최대 체력 −2   ◈ 주민이 피한다"));
        Assert.That(WildAxisGaugeText.Label(HungerStage.Limit, 0.50f, 2),
            Is.EqualTo("한계 — 공격 ×0.5, 최대 체력 −2   ◈ 주민이 피한다"));
    }

    [Test]
    public void 배율은_소수점_불필요한_0을_떼고_적는다()
    {
        // "×1" 이지 "×1.00" 이 아니다. 소수 구분자가 지역 설정을 타면 안 되므로 불변 문화권으로 찍는다.
        Assert.That(WildAxisGaugeText.Label(HungerStage.Hungry, 1f, 0), Does.Contain("×1"));
        Assert.That(WildAxisGaugeText.Label(HungerStage.Hungry, 1f, 0), Does.Not.Contain("×1.0"));
        Assert.That(WildAxisGaugeText.Label(HungerStage.Hungry, 0.85f, 0), Does.Contain("×0.85"));
    }

    [Test]
    public void 마커는_굶주림_한계에서_0_안전에서_반_야성_한계에서_1()
    {
        Assert.That(WildAxisGaugeText.MarkerPosition(WildAxisCore.StarveLimit), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(WildAxisGaugeText.MarkerPosition(0f), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(WildAxisGaugeText.MarkerPosition(WildAxisCore.WildLimit), Is.EqualTo(1f).Within(0.0001f));
    }

    [Test]
    public void 마커는_바_밖으로_나가지_않는다()
    {
        // 축은 코어가 클램프하지만, 게이지가 그걸 믿고 계산하면 나중에 클램프가 풀릴 때
        // 마커만 화면 밖으로 사라진다. 여기서 한 번 더 자른다.
        Assert.That(WildAxisGaugeText.MarkerPosition(-500f), Is.EqualTo(0f));
        Assert.That(WildAxisGaugeText.MarkerPosition(500f), Is.EqualTo(1f));
    }

    [Test]
    public void 마커가_안전의_어느_쪽인지로_색을_고른다()
    {
        // 음수면 굶주림 쪽(붉은 계열), 그 외는 야성 쪽(보라 계열).
        Assert.That(WildAxisGaugeText.LeansStarving(-0.1f), Is.True);
        Assert.That(WildAxisGaugeText.LeansStarving(0f), Is.False);
        Assert.That(WildAxisGaugeText.LeansStarving(0.1f), Is.False);
    }

    [Test]
    public void 안전에서_멀수록_색이_진해진다()
    {
        // 0 = 회색, 1 = 그 방향의 완전한 색.
        Assert.That(WildAxisGaugeText.DistanceFromSafe(0f), Is.EqualTo(0f).Within(0.0001f));
        Assert.That(WildAxisGaugeText.DistanceFromSafe(-50f), Is.EqualTo(0.5f).Within(0.0001f));
        Assert.That(WildAxisGaugeText.DistanceFromSafe(WildAxisCore.WildLimit), Is.EqualTo(1f).Within(0.0001f));
        Assert.That(WildAxisGaugeText.DistanceFromSafe(WildAxisCore.StarveLimit), Is.EqualTo(1f).Within(0.0001f));
    }
}
