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

    public int TempGold { get; private set; }
    public int ConfirmedGold { get; private set; }

    void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void AddTempGold(int amount)
    {
        TempGold += amount;
    }

    /// <summary>사망 시 임시 주머니 손실</summary>
    public void LoseTempGold()
    {
        TempGold = 0;
    }

    /// <summary>살아서 귀환 시 임시 주머니 + 클리어 보너스를 금고로 확정</summary>
    public void BankGold(int clearBonus = 0)
    {
        ConfirmedGold += TempGold + clearBonus;
        TempGold = 0;
    }

    /// <summary>달 rewardTier(1~5) 기준 클리어 보너스 골드</summary>
    public static int ClearBonus(int rewardTier) => rewardTier * 20;
}
