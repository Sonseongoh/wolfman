using NUnit.Framework;

[TestFixture]
public class GoldWalletTests
{
    GoldWallet wallet;

    [SetUp]
    public void SetUp()
    {
        wallet = new GoldWallet();
    }

    [Test]
    public void 새_지갑은_주머니와_금고가_모두_0이다()
    {
        Assert.That(wallet.TempGold, Is.EqualTo(0));
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(0));
    }

    [Test]
    public void AddTemp는_주머니에_누적된다()
    {
        wallet.AddTemp(3);
        wallet.AddTemp(5);

        Assert.That(wallet.TempGold, Is.EqualTo(8));
    }

    [Test]
    public void AddTemp는_음수를_무시한다()
    {
        wallet.AddTemp(10);
        wallet.AddTemp(-4);

        Assert.That(wallet.TempGold, Is.EqualTo(10));
    }

    [Test]
    public void LoseTemp는_주머니만_비우고_금고는_건드리지_않는다()
    {
        wallet.AddTemp(12);
        wallet.Bank();
        wallet.AddTemp(7);

        wallet.LoseTemp();

        Assert.That(wallet.TempGold, Is.EqualTo(0));
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(12));
    }

    [Test]
    public void Bank는_주머니와_보너스를_금고로_확정한다()
    {
        wallet.AddTemp(15);

        wallet.Bank(20);

        Assert.That(wallet.ConfirmedGold, Is.EqualTo(35));
        Assert.That(wallet.TempGold, Is.EqualTo(0));
    }

    [Test]
    public void Bank의_보너스_기본값은_0이다()
    {
        wallet.AddTemp(9);

        wallet.Bank();

        Assert.That(wallet.ConfirmedGold, Is.EqualTo(9));
        Assert.That(wallet.TempGold, Is.EqualTo(0));
    }

    [Test]
    public void TrySpendConfirmed는_잔액이_충분하면_차감하고_true를_반환한다()
    {
        wallet.AddTemp(100);
        wallet.Bank();

        bool spent = wallet.TrySpendConfirmed(30);

        Assert.That(spent, Is.True);
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(70));
    }

    [Test]
    public void TrySpendConfirmed는_잔액과_비용이_같으면_성공하고_잔액이_0이_된다()
    {
        wallet.AddTemp(30);
        wallet.Bank();

        bool spent = wallet.TrySpendConfirmed(30);

        Assert.That(spent, Is.True);
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(0));
    }

    [Test]
    public void TrySpendConfirmed는_잔액이_부족하면_실패하고_잔액을_유지한다()
    {
        wallet.AddTemp(29);
        wallet.Bank();

        bool spent = wallet.TrySpendConfirmed(30);

        Assert.That(spent, Is.False);
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(29));
    }

    [Test]
    public void TrySpendConfirmed는_비용이_0이면_성공하고_잔액이_그대로다()
    {
        wallet.AddTemp(10);
        wallet.Bank();

        bool spent = wallet.TrySpendConfirmed(0);

        Assert.That(spent, Is.True);
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(10));
    }

    [Test]
    public void TrySpendConfirmed는_비용이_음수면_실패하고_잔액이_그대로다()
    {
        wallet.AddTemp(10);
        wallet.Bank();

        bool spent = wallet.TrySpendConfirmed(-5);

        Assert.That(spent, Is.False);
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(10));
    }

    [Test]
    public void TrySpendConfirmed는_주머니를_재원으로_쓰지_않는다()
    {
        wallet.AddTemp(500); // 주머니는 넉넉하지만 금고는 비어있다

        bool spent = wallet.TrySpendConfirmed(30);

        Assert.That(spent, Is.False);
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(0));
        Assert.That(wallet.TempGold, Is.EqualTo(500));
    }

    [Test]
    public void ResetRun은_금고와_주머니를_모두_비운다()
    {
        wallet.AddTemp(120);
        wallet.Bank();
        wallet.AddTemp(35);

        wallet.ResetRun();

        Assert.That(wallet.ConfirmedGold, Is.EqualTo(0));
        Assert.That(wallet.TempGold, Is.EqualTo(0));
    }

    [Test]
    public void ResetRun_후에는_새_런의_뱅킹이_0에서_시작한다()
    {
        wallet.AddTemp(50);
        wallet.Bank();

        wallet.ResetRun();
        wallet.AddTemp(10);
        wallet.Bank();

        Assert.That(wallet.ConfirmedGold, Is.EqualTo(10));
        Assert.That(wallet.TempGold, Is.EqualTo(0));
    }

    [Test]
    public void ResetRun은_빈_지갑에서도_안전하다()
    {
        wallet.ResetRun();

        Assert.That(wallet.TempGold, Is.EqualTo(0));
        Assert.That(wallet.ConfirmedGold, Is.EqualTo(0));
    }
}
