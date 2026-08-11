using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 달이 뜰 때 화면 전체 조명을 그 달의 분위기로 전환 (#25).
/// 블러드문 = 붉은 밤, 블랙문 = 칠흑, 하베스트문 = 황금빛...
///
/// **씬마다 자기 오브젝트에 붙이고 그 씬의 Global Light 2D를 연결한다.**
/// 잡고 있는 조명이 씬의 것이라 이 컴포넌트도 씬과 함께 죽어야 한다 —
/// 씬을 넘어 사는 GameManager 오브젝트에 얹혀 있던 동안은 중복 판정에 끌려
/// 매번 파괴돼서 달 분위기가 한 번도 적용되지 않았다.
/// </summary>
public class MoonEffects : MonoBehaviour
{
    [Tooltip("씬의 Global Light 2D")]
    public Light2D globalLight;

    [Tooltip("조명이 새 분위기로 넘어가는 데 걸리는 시간(초)")]
    public float transitionTime = 1.5f;

    Color targetColor = Color.white;
    float targetIntensity = 1f;

    void Start()
    {
        if (GameManager.Instance == null) return;

        GameManager.Instance.OnMoonRevealed += HandleMoonRevealed;

        // 구독만으로는 늦다. 달은 이 씬보다 먼저 정해진다 — 정산 허브(MainScene)가 달을 뽑아
        // OnMoonRevealed 를 쏘고, 그 다음에야 사냥 씬이 로드된다. 여기서 구독을 시작할 무렵이면
        // 이번 밤의 달은 이미 지나간 이벤트다. 그래서 지금 떠 있는 달을 직접 읽어 한 번 적용한다.
        // (사냥 씬만 단독으로 재생할 때는 아직 달이 없어 null 이고, 곧 이벤트로 온다.)
        HandleMoonRevealed(GameManager.Instance.CurrentMoon);
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoonRevealed -= HandleMoonRevealed;
    }

    void HandleMoonRevealed(MoonData moon)
    {
        if (moon == null) return;
        targetColor = moon.ambientColor;
        targetIntensity = moon.ambientIntensity;
    }

    void Update()
    {
        if (globalLight == null) return;

        // 달 공개는 실시간으로 흐른다 (#101) — 조명만 게임 시간에 매여 있으면
        // timeScale 이 0 인 채로 사냥 씬에 들어왔을 때 카드는 도는데 조명만 얼어붙는다
        float t = transitionTime > 0f ? Time.unscaledDeltaTime / transitionTime : 1f;
        globalLight.color = Color.Lerp(globalLight.color, targetColor, t);
        globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, t);
    }
}
