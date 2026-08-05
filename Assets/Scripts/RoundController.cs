using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// MainScene 흐름 제어 (#6)
/// - 첫 시작/보상 후: 달 추첨하고 SampleScene으로
/// - 전투/마을 끝나고 돌아왔을 때: 보상 화면 표시
/// 달 연출 + 사냥/마을 선택은 SampleScene(WaveManager)에서 첫 웨이브에만 처리
/// </summary>
public class RoundController : MonoBehaviour
{
    bool showingReward;
    float rewardTimer;

    void Start()
    {
        if (GameManager.Instance == null) return;
        if (SceneManager.GetActiveScene().name != "MainScene") return;

        if (GameManager.Instance.Phase == RoundPhase.Hunt ||
            GameManager.Instance.Phase == RoundPhase.Village)
        {
            showingReward = true;
            rewardTimer = 3f;
        }
        else
        {
            GameManager.Instance.StartNextRound();
            SceneManager.LoadScene("SampleScene");
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
            SceneManager.LoadScene("SampleScene");
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

        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 50), "라운드 클리어!", center);
        if (moon != null)
            GUI.Label(new Rect(0, Screen.height * 0.45f, Screen.width, 40),
                $"보상 등급: {moon.rewardTier}성  |  라운드 {GameManager.Instance.RoundNumber}", center);
    }
}
