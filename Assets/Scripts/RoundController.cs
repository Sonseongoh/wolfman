using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MainScene 흐름 제어 (#6)
/// - 첫 시작/보상 후: 달 추첨하고 SampleScene으로
/// - 전투/마을 끝나고 돌아왔을 때: 보상 화면 표시 + 재화 뱅킹 (#8)
/// 달 연출 + 사냥/마을 선택은 SampleScene(WaveManager)에서 첫 웨이브에만 처리
/// </summary>
public class RoundController : MonoBehaviour
{
    bool showingReward;
    float rewardTimer;
    int rewardTempGold;   // 귀환 시 확정될 임시 골드 (표시용)
    int rewardClearBonus; // 달 등급 클리어 보너스

    void Start()
    {
        if (GameManager.Instance == null) return;
        if (SceneManager.GetActiveScene().name != "MainScene") return;

        // 타이틀은 별도 씬(TitleScene, #94) — 여기는 정산 허브 전용.
        // Phase가 Title인 채 오는 건 에디터에서 MainScene을 직접 Play한 경우뿐이라 새 라운드로 취급된다

        if (RoundFlowRule.ShouldSettle(GameManager.Instance.Phase))
        {
            // 귀환 성공 → 재화 확정 (#8)
            MoonData moon = GameManager.Instance.CurrentMoon;
            int tier = moon != null ? moon.rewardTier : 1;
            rewardClearBonus = CurrencyManager.ClearBonus(tier);

            if (CurrencyManager.Instance != null)
            {
                rewardTempGold = CurrencyManager.Instance.TempGold;
                CurrencyManager.Instance.BankGold(rewardClearBonus);
            }

            showingReward = true;
            rewardTimer = 3f;
        }
        else
        {
            GameManager.Instance.StartNextRound();
            Time.timeScale = 1f; // 앞 씬에서 멈춰둔 시간을 들고 넘어가지 않는다
            SceneManager.LoadScene("HuntScene");
        }
    }

    void Update()
    {
        if (!showingReward) return;
        rewardTimer -= Time.deltaTime;
        if (rewardTimer <= 0f)
        {
            showingReward = false;
            GameManager.Instance.StartNextRound();
            Time.timeScale = 1f; // 앞 씬에서 멈춰둔 시간을 들고 넘어가지 않는다
            SceneManager.LoadScene("HuntScene");
        }
    }

    void OnGUI()
    {
        if (!showingReward) return;
        if (SceneManager.GetActiveScene().name != "MainScene") return;

        MoonData moon = GameManager.Instance?.CurrentMoon;
        GUIStyle center = new GUIStyle(GUI.skin.label)
        {
            fontSize = 32,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUIStyle gold = new GUIStyle(GUI.skin.label)
        {
            fontSize = 24,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.85f, 0.2f) }
        };

        GUI.Label(new Rect(0, Screen.height * 0.30f, Screen.width, 50), "라운드 클리어!", center);
        if (moon != null)
            GUI.Label(new Rect(0, Screen.height * 0.40f, Screen.width, 40),
                $"보상 등급: {moon.rewardTier}성  |  라운드 {GameManager.Instance.RoundNumber}", center);

        // 재화 정산 표시 (#8)
        if (CurrencyManager.Instance != null)
        {
            GUI.Label(new Rect(0, Screen.height * 0.51f, Screen.width, 34),
                $"사냥 수익  {rewardTempGold}G  +  클리어 보너스  {rewardClearBonus}G", gold);
            GUI.Label(new Rect(0, Screen.height * 0.57f, Screen.width, 34),
                $"금고 합계  {CurrencyManager.Instance.ConfirmedGold}G", gold);
        }
    }
}
