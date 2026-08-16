/// <summary>
/// 금고에서 재화를 꺼내 쓰는 쪽이 보는 계약 (#11).
/// 소비자(시설 수리, 추후 상점 #13)에게 필요한 건 "차감" 하나뿐이라 그것만 노출한다 —
/// 뱅킹이나 주머니 손실 같은 흐름 제어는 이 문으로 열리지 않는다.
/// </summary>
public interface IGoldVault
{
    /// <summary>금고에서 cost 만큼 차감. 잔액이 모자라면 아무것도 바꾸지 않고 false.</summary>
    bool TrySpendConfirmed(int cost);
}

/// <summary>
/// 재화 뱅킹의 순수 로직 (#8, #11). UnityEngine 에 의존하지 않아 WSL 에서 테스트할 수 있다.
/// TempGold: 사냥 중 쌓이는 임시 주머니 — 사망 시 손실.
/// ConfirmedGold: 금고로 확정된 재화 — 시설 수리·상점 등 소비의 유일한 재원.
/// </summary>
public class GoldWallet : IGoldVault
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

    /// <summary>
    /// 런 경계 (#121): 런이 끝나면 금고까지 전부 비운다. LoseTemp(죽음)와 다르다 —
    /// 죽음은 밤 하나의 끝이라 주머니만 잃지만, 런의 끝은 지갑 전부를 지운다.
    /// </summary>
    public void ResetRun()
    {
        TempGold = 0;
        ConfirmedGold = 0;
    }
}
