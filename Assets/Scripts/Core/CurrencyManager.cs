using UnityEngine;

/// <summary>
/// 재화 뱅킹 시스템 (#8).
/// TempGold: 사냥 중 쌓이는 임시 주머니 — 사망 시 손실.
/// ConfirmedGold: 살아서 귀환하거나 마을에서 성공 후 금고로 확정된 재화.
/// </summary>
public class CurrencyManager : LazySingleton<CurrencyManager>, IGoldVault
{
    // 실제 계산은 순수 C# GoldWallet 이 한다 (#11) — 이쪽은 씬 수명만 관리하는 껍데기.
    // wallet 을 밖으로 내보내지 않는다: 내보내면 Bank()/LoseTemp() 같은 흐름 제어까지
    // 아무 데서나 부를 수 있게 된다. 소비자에게는 IGoldVault(차감 하나)만 보인다.
    readonly GoldWallet wallet = new GoldWallet();

    public int TempGold => wallet.TempGold;
    public int ConfirmedGold => wallet.ConfirmedGold;

    /// <summary>
    /// 런 경계 (#121): 새 런이 열리면 스스로 비운다. GameManager 가 이쪽을 직접 부르지 않고
    /// 구독으로 잇는 건 그쪽 규약이다 — "모듈 간 통신은 이벤트 구독으로 (직접 참조 금지)".
    /// 이 매니저는 씬·프리팹 어디에도 박혀 있지 않고 첫 .Instance 접근에 만들어지므로,
    /// 그때 GameManager 는 이미 있다 (타이틀에서 만들어져 씬을 넘어 산다).
    /// 예외는 GameManager 가 없는 VillageScene 을 단독 재생하는 개발용 경로뿐인데,
    /// 그때는 StartRun 을 부를 GameManager 자체가 없어 지울 런 경계도 없다.
    /// </summary>
    protected override void OnSingletonAwake()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnRunStarted += ResetRun;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.OnRunStarted -= ResetRun;
    }

    public void AddTempGold(int amount)
    {
        wallet.AddTemp(amount);
    }

    /// <summary>사망 시 임시 주머니 손실</summary>
    public void LoseTempGold()
    {
        wallet.LoseTemp();
    }

    /// <summary>살아서 귀환 시 임시 주머니 + 클리어 보너스를 금고로 확정</summary>
    public void BankGold(int clearBonus = 0)
    {
        wallet.Bank(clearBonus);
    }

    /// <summary>
    /// 금고에서 차감 — 잔액이 모자라면 아무것도 바꾸지 않고 false (#11 수리, #13 상점).
    /// 이름에 Gold 를 붙이지 않은 건 IGoldVault 구현이기 때문이다 — GoldWallet 과 같은 이름을 쓴다.
    /// </summary>
    public bool TrySpendConfirmed(int cost) => wallet.TrySpendConfirmed(cost);

    /// <summary>런 경계 (#121): 이전 런의 재화를 전부 지운다 — GameManager.OnRunStarted 가 부른다.</summary>
    public void ResetRun() => wallet.ResetRun();

    /// <summary>달 rewardTier(1~5) 기준 클리어 보너스 골드</summary>
    public static int ClearBonus(int rewardTier) => rewardTier * 20;
}
