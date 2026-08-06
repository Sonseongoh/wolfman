using UnityEngine;

/// <summary>
/// 재화 뱅킹 시스템 (#8).
/// TempGold: 사냥 중 쌓이는 임시 주머니 — 사망 시 손실.
/// ConfirmedGold: 살아서 귀환하거나 마을에서 성공 후 금고로 확정된 재화.
/// </summary>
public class CurrencyManager : MonoBehaviour, IGoldVault
{
    static CurrencyManager _instance;
    public static CurrencyManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("CurrencyManager");
                _instance = go.AddComponent<CurrencyManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    // 실제 계산은 순수 C# GoldWallet 이 한다 (#11) — 이쪽은 씬 수명만 관리하는 껍데기.
    // wallet 을 밖으로 내보내지 않는다: 내보내면 Bank()/LoseTemp() 같은 흐름 제어까지
    // 아무 데서나 부를 수 있게 된다. 소비자에게는 IGoldVault(차감 하나)만 보인다.
    readonly GoldWallet wallet = new GoldWallet();

    public int TempGold => wallet.TempGold;
    public int ConfirmedGold => wallet.ConfirmedGold;

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
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

    /// <summary>달 rewardTier(1~5) 기준 클리어 보너스 골드</summary>
    public static int ClearBonus(int rewardTier) => rewardTier * 20;
}
