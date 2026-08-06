/// <summary>수리 시도 결과 (#11)</summary>
public enum RepairResult
{
    Success,
    NotDamaged,     // 멀쩡한 시설 — 수리할 게 없다
    NotEnoughGold,  // 금고 잔액 부족
}

/// <summary>
/// 마을 시설의 체력·파괴·수리 로직 (#11). UnityEngine 에 의존하지 않아 WSL 에서 테스트할 수 있다.
/// 파괴돼도 사라지지 않고 Hp 0 상태로 남는다 — 금고 골드를 써서 다시 세운다.
/// </summary>
public class FacilityCore
{
    public int MaxHp { get; }
    public int Hp { get; private set; }

    public bool IsDestroyed => Hp <= 0;

    /// <summary>파손(만피가 아님) 상태 — 파괴 전이라도 수리 대상이다</summary>
    public bool IsDamaged => Hp < MaxHp;

    public FacilityCore(int maxHp)
    {
        MaxHp = maxHp < 1 ? 1 : maxHp;
        Hp = MaxHp;
    }

    /// <summary>이번 호출로 파괴됐으면 true (파괴 연출을 한 번만 내보내기 위함)</summary>
    public bool TakeDamage(int amount)
    {
        if (amount <= 0) return false;
        if (IsDestroyed) return false;

        Hp -= amount;
        if (Hp < 0) Hp = 0;

        return IsDestroyed;
    }

    /// <summary>
    /// 금고에서 cost 만큼 내고 만피로 복구. 실패하면 체력도 지갑도 그대로 둔다.
    /// 비용은 남은 체력과 무관한 고정값이다.
    /// </summary>
    public RepairResult TryRepair(GoldWallet wallet, int cost)
    {
        if (!IsDamaged) return RepairResult.NotDamaged;
        if (!wallet.TrySpendConfirmed(cost)) return RepairResult.NotEnoughGold;

        Hp = MaxHp;
        return RepairResult.Success;
    }
}
