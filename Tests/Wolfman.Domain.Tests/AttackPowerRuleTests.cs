using NUnit.Framework;

/// <summary>
/// 한 번 휘두르거나 쏠 때 적이 실제로 받는 피해 (#38, #117 준비).
///
/// 이 규칙이 생기기 전에는 같은 식이 <c>MeleeAttack.Swing</c> 과 <c>PlayerAttack.Fire</c> 에
/// 복사돼 있었다. 두 경로는 동시에 도는 게 아니라 스킬 "달빛 참격"이 바꾸는 배타적 모드라서,
/// 한쪽에만 새 배율을 넣으면 그 스킬을 뽑은 런에서만 배율이 통째로 빠진다.
/// 굶주림 페널티(#117)를 붙이기 전에 식을 한 곳으로 모은 이유가 이것이다.
/// </summary>
[TestFixture]
public class AttackPowerRuleTests
{
    [Test]
    public void 배율이_없으면_기본_데미지_그대로다()
    {
        Assert.That(AttackPowerRule.Damage(10, 0f, 1f), Is.EqualTo(10));
    }

    [Test]
    public void 스킬_공격력_보너스는_합연산이다()
    {
        // 0.15 두 장을 얻으면 1.30 배지, 1.15² = 1.3225 배가 아니다.
        // 곱연산으로 바꾸면 스택이 쌓일수록 조용히 벌어진다.
        Assert.That(AttackPowerRule.Damage(100, 0.30f, 1f), Is.EqualTo(130));
    }

    [Test]
    public void 달_배율이_곱해진다()
    {
        Assert.That(AttackPowerRule.Damage(10, 0f, 1.5f), Is.EqualTo(15));
    }

    [Test]
    public void 스킬과_달은_함께_곱해진다()
    {
        // 100 × 1.2 × 1.5
        Assert.That(AttackPowerRule.Damage(100, 0.2f, 1.5f), Is.EqualTo(180));
    }

    [Test]
    public void 아무리_깎여도_최소_1은_들어간다()
    {
        // 0 이 되면 적이 절대 죽지 않아 "약해졌다"가 아니라 "고장났다"로 읽힌다.
        Assert.That(AttackPowerRule.Damage(1, 0f, 0.1f), Is.EqualTo(1));
        Assert.That(AttackPowerRule.Damage(10, -1f, 1f), Is.EqualTo(1));
    }

    [TestCase(5, 0.5f, 2, TestName = "반올림_2_5는_2로_내려붙는다")]
    [TestCase(7, 0.5f, 4, TestName = "반올림_3_5는_4로_올라붙는다")]
    public void 정확히_반이면_짝수쪽으로_붙는다(int baseDamage, float moon, int expected)
    {
        // Unity 의 Mathf.RoundToInt 와 같은 은행가 반올림. 0.5 를 항상 올림으로 바꾸면
        // 기존 전투 수치가 조용히 달라진다 — 이 규칙은 동작을 옮긴 것이지 바꾼 게 아니다.
        Assert.That(AttackPowerRule.Damage(baseDamage, 0f, moon), Is.EqualTo(expected));
    }

    [Test]
    public void 기본_데미지가_작으면_배율이_계단으로_뭉갠다()
    {
        // 달빛 참격(base 5)에서 실측된 것: −15% 와 −30% 가 같은 값 4 로 붙고,
        // −50% 는 2 로 떨어져 실제로는 −60% 가 된다.
        // 정수 데미지의 한계이지 버그가 아니다. #117 의 굶주림 단계를 이 위에 얹을 때
        // "단계는 넷인데 체감은 셋"이 되는 지점이라 여기 남긴다.
        Assert.That(AttackPowerRule.Damage(5, 0f, 0.85f), Is.EqualTo(4));
        Assert.That(AttackPowerRule.Damage(5, 0f, 0.70f), Is.EqualTo(4));
        Assert.That(AttackPowerRule.Damage(5, 0f, 0.50f), Is.EqualTo(2));
    }

    // 여기서 못 박을 수 없는 것: 딱 0.5 에 걸리는 극단 조합의 마지막 1 차이.
    // 유니티(Mono)는 곱셈 중간값을 확장 정밀도로 들고 있고 이 테스트가 도는 .NET 9 는
    // 매 단계 float 로 접는다. base=100·보너스 0.45·달 0.1 이면 에디터는 15, 여기서는 14 다.
    // 그래서 "옛 식과 같은가"는 유니티 안에서 1,452개 조합을 대조해 확인했다 (불일치 0).
    // 규칙 본문을 한 줄로 접으면 그 대조가 4개 깨진다 — AttackPowerRule 의 주석을 볼 것.

    [Test]
    public void 발톱과_참격은_같은_식을_지난다()
    {
        // 두 모드의 차이는 기본 데미지뿐이어야 한다. 배율이 한쪽에만 걸리면
        // "달빛 참격을 뽑으면 페널티를 면제받는다" 같은 구멍이 생긴다.
        const float skill = 0.2f;
        const float moon = 0.5f;

        int claw = AttackPowerRule.Damage(10, skill, moon);   // 발톱 base 10
        int wave = AttackPowerRule.Damage(5, skill, moon);    // 달빛 참격 base 5

        Assert.That(claw, Is.EqualTo(6));
        Assert.That(wave, Is.EqualTo(3));
    }
}
