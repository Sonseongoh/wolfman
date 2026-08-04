using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 달이 뜰 때 화면 전체 조명을 그 달의 분위기로 전환 (#25).
/// 블러드문 = 붉은 밤, 블랙문 = 칠흑, 하베스트문 = 황금빛...
/// GameManager 오브젝트에 붙이고 씬의 Global Light 2D를 연결한다.
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
        if (GameManager.Instance != null)
            GameManager.Instance.OnMoonRevealed += HandleMoonRevealed;
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

        float t = transitionTime > 0f ? Time.deltaTime / transitionTime : 1f;
        globalLight.color = Color.Lerp(globalLight.color, targetColor, t);
        globalLight.intensity = Mathf.Lerp(globalLight.intensity, targetIntensity, t);
    }
}
