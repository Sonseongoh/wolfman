using UnityEngine;

/// <summary>
/// 재화 뱅킹 시스템 (#8).
/// TempGold: 사냥 중 쌓이는 임시 주머니 — 사망 시 손실.
/// ConfirmedGold: 살아서 귀환하거나 마을에서 성공 후 금고로 확정된 재화.
/// </summary>
public class CurrencyManager : MonoBehaviour
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

    // 실제 계산은 순수 C# GoldWallet 이 한다 (#11) — 이쪽은 씬 수명만 관리하는 껍데기
    readonly GoldWallet wallet = new GoldWallet();

    /// <summary>도메인 로직이 직접 지갑을 다뤄야 할 때 (FacilityCore 수리 등, #11)</summary>
    public GoldWallet Wallet => wallet;

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

    /// <summary>금고에서 차감 — 잔액이 모자라면 아무것도 바꾸지 않고 false (#11 수리, #13 상점)</summary>
    public bool TrySpendConfirmedGold(int amount) => wallet.TrySpendConfirmed(amount);

    /// <summary>달 rewardTier(1~5) 기준 클리어 보너스 골드</summary>
    public static int ClearBonus(int rewardTier) => rewardTier * 20;
}
