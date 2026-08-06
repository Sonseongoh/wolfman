/// <summary>
/// 재화 뱅킹의 순수 로직 (#8, #11). UnityEngine 에 의존하지 않아 WSL 에서 테스트할 수 있다.
/// TempGold: 사냥 중 쌓이는 임시 주머니 — 사망 시 손실.
/// ConfirmedGold: 금고로 확정된 재화 — 시설 수리·상점 등 소비의 유일한 재원.
/// </summary>
public class GoldWallet
{
    public int TempGold { get; private set; }
    public int ConfirmedGold { get; private set; }

    public void AddTemp(int amount)
    {
        if (amount <= 0) return;
        TempGold += amount;
    }

    /// <summary>사망 시 임시 주머니 손실</summary>
    public void LoseTemp()
    {
        TempGold = 0;
    }

    /// <summary>살아서 귀환 시 임시 주머니 + 클리어 보너스를 금고로 확정</summary>
    public void Bank(int clearBonus = 0)
    {
        ConfirmedGold += TempGold + clearBonus;
        TempGold = 0;
    }

    /// <summary>
    /// 금고에서 cost 만큼 차감. 잔액이 모자라면 아무것도 바꾸지 않고 false.
    /// 주머니(TempGold)는 재원으로 쓰지 않는다 — 확정된 재화만 소비한다.
    /// </summary>
    public bool TrySpendConfirmed(int cost)
    {
        if (cost < 0) return false;
        if (cost > ConfirmedGold) return false;

        ConfirmedGold -= cost;
        return true;
    }
}
