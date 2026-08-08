using NUnit.Framework;

[TestFixture]
public class FacilityCoreTests
{
    /// <summary>금고에 confirmed 만큼 확정 골드가 든 지갑</summary>
    static GoldWallet WalletWith(int confirmed)
    {
        var wallet = new GoldWallet();
        wallet.AddTemp(confirmed);
        wallet.Bank();
        return wallet;
    }

    [Test]
    public void 생성_직후에는_만피이고_파손도_파괴도_아니다()
    {
        var core = new FacilityCore(10);

        Assert.That(core.MaxHp, Is.EqualTo(10));
        Assert.That(core.Hp, Is.EqualTo(10));
        Assert.That(core.IsDestroyed, Is.False);
        Assert.That(core.IsDamaged, Is.False);
    }

    [Test]
    public void 최대체력이_1_미만이면_1로_보정된다()
    {
        var core = new FacilityCore(0);

        Assert.That(core.MaxHp, Is.EqualTo(1));
        Assert.That(core.Hp, Is.EqualTo(1));
    }

    [Test]
    public void TakeDamage는_체력을_깎는다()
    {
        var core = new FacilityCore(10);

        bool destroyed = core.TakeDamage(3);

        Assert.That(core.Hp, Is.EqualTo(7));
        Assert.That(destroyed, Is.False);
    }

    [Test]
    public void 체력이_0이_되면_파괴되고_그_호출이_true를_반환한다()
    {
        var core = new FacilityCore(3);

        bool destroyed = core.TakeDamage(3);

        Assert.That(destroyed, Is.True);
        Assert.That(core.IsDestroyed, Is.True);
        Assert.That(core.Hp, Is.EqualTo(0));
    }

    [Test]
    public void 초과_데미지는_체력을_0으로_고정한다()
    {
        var core = new FacilityCore(5);

        core.TakeDamage(999);

        Assert.That(core.Hp, Is.EqualTo(0));
    }

    [Test]
    public void 파괴된_뒤의_추가_피해는_무시된다()
    {
        var core = new FacilityCore(2);
        core.TakeDamage(2);

        bool destroyed = core.TakeDamage(5);

        Assert.That(destroyed, Is.False, "파괴 전이는 한 번만 보고한다");
        Assert.That(core.Hp, Is.EqualTo(0));
        Assert.That(core.IsDestroyed, Is.True);
    }

    [Test]
    public void 데미지가_0이하면_무시된다()
    {
        var core = new FacilityCore(10);

        core.TakeDamage(0);
        core.TakeDamage(-4);

        Assert.That(core.Hp, Is.EqualTo(10));
    }

    [Test]
    public void 체력이_최대보다_낮으면_파손_상태다()
    {
        var core = new FacilityCore(10);

        core.TakeDamage(1);

        Assert.That(core.IsDamaged, Is.True);
        Assert.That(core.IsDestroyed, Is.False);
    }

    [Test]
    public void 만피_시설은_수리할_수_없고_지갑도_그대로다()
    {
        var core = new FacilityCore(10);
        var wallet = WalletWith(100);

        RepairResult result = core.TryRepair(wallet, 30);

        Assert.That(result, Is.EqualTo(RepairResult.NotDamaged));
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(100));
        Assert.That(core.Hp, Is.EqualTo(10));
    }

    [Test]
    public void 금고_잔액이_부족하면_수리에_실패하고_체력과_지갑이_그대로다()
    {
        var core = new FacilityCore(10);
        core.TakeDamage(10);
        var wallet = WalletWith(29);

        RepairResult result = core.TryRepair(wallet, 30);

        Assert.That(result, Is.EqualTo(RepairResult.NotEnoughGold));
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(29));
        Assert.That(core.Hp, Is.EqualTo(0));
        Assert.That(core.IsDestroyed, Is.True);
    }

    [Test]
    public void 파괴된_시설을_수리하면_만피로_복구되고_비용만큼만_차감된다()
    {
        var core = new FacilityCore(10);
        core.TakeDamage(10);
        var wallet = WalletWith(100);

        RepairResult result = core.TryRepair(wallet, 30);

        Assert.That(result, Is.EqualTo(RepairResult.Success));
        Assert.That(core.Hp, Is.EqualTo(10));
        Assert.That(core.IsDestroyed, Is.False);
        Assert.That(core.IsDamaged, Is.False);
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(70));
    }

    [Test]
    public void 파괴되지_않은_파손_시설도_수리할_수_있다()
    {
        var core = new FacilityCore(10);
        core.TakeDamage(4);
        var wallet = WalletWith(50);

        RepairResult result = core.TryRepair(wallet, 30);

        Assert.That(result, Is.EqualTo(RepairResult.Success));
        Assert.That(core.Hp, Is.EqualTo(10));
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(20));
    }

    [Test]
    public void 잔액이_비용과_정확히_같으면_수리에_성공한다()
    {
        var core = new FacilityCore(10);
        core.TakeDamage(10);
        var wallet = WalletWith(30);

        RepairResult result = core.TryRepair(wallet, 30);

        Assert.That(result, Is.EqualTo(RepairResult.Success));
        Assert.That(core.Hp, Is.EqualTo(10));
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(0));
    }

    [Test]
    public void 수리_비용이_0이면_잔액이_없어도_성공한다()
    {
        var core = new FacilityCore(10);
        core.TakeDamage(10);
        var wallet = WalletWith(0);

        RepairResult result = core.TryRepair(wallet, 0);

        Assert.That(result, Is.EqualTo(RepairResult.Success));
        Assert.That(core.Hp, Is.EqualTo(10));
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(0));
    }
}
