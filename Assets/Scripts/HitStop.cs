using System.Collections;
using UnityEngine;

/// <summary>
/// 히트스톱 (#31): 강한 타격 순간 아주 짧게 시간을 멈춰 무게감을 준다.
/// 코드로 자동 생성 — 씬 배치 불필요. HitStop.Do(0.06f) 식으로 호출.
/// </summary>
public class HitStop : MonoBehaviour
{
    static HitStop runner;

    public static void Do(float duration)
    {
        // 이미 다른 시스템(스킬 선택·게임오버)이 시간을 멈춘 상태면 개입하지 않는다
        if (Time.timeScale == 0f) return;

        if (runner == null)
        {
            GameObject go = new GameObject("HitStop");
            DontDestroyOnLoad(go);
            runner = go.AddComponent<HitStop>();
        }
        runner.StopAllCoroutines();
        runner.StartCoroutine(runner.Freeze(duration));
    }

    IEnumerator Freeze(float duration)
    {
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);

        // 멈춰 있는 사이에 선택창·게임오버가 열렸으면 정지 상태를 유지해야 한다
        bool keepFrozen = false;

        if (SkillSystem.Instance != null && SkillSystem.Instance.IsChoosing) keepFrozen = true;
        if (PauseSystem.IsPaused) keepFrozen = true;

        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health != null && health.IsDead) keepFrozen = true;

        if (!keepFrozen) Time.timeScale = 1f;
    }
}
