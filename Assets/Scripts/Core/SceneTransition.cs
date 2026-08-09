using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 씬 전환 연출 싱글턴 (#82).
/// SceneTransition.Go("씬이름") 으로 어디서든 호출.
/// 페이드 아웃 → 달 아이콘 → 페이드 인 순으로 연출.
/// </summary>
public class SceneTransition : MonoBehaviour
{
    public static SceneTransition Instance { get; private set; }

    [Range(0.3f, 2f)] public float fadeOutDuration = 0.45f;
    [Range(0.3f, 2f)] public float fadeInDuration  = 0.55f;
    [Range(0f, 1f)]   public float holdDuration    = 0.25f;

    float alpha;
    bool  active;
    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>페이드 아웃 → 씬 로드 → 페이드 인</summary>
    public static void Go(string sceneName)
    {
        if (Instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }
        Instance.StartCoroutine(Instance.Run(sceneName));
    }

    IEnumerator Run(string sceneName)
    {
        if (active) yield break;
        active = true;

        // 페이드 아웃
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.unscaledDeltaTime;
            alpha = Mathf.Clamp01(t / fadeOutDuration);
            yield return null;
        }
        alpha = 1f;

        // 씬 로드 (비동기 — 검은 화면 유지)
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = false;

        // 로드 완료 대기 + holdDuration 동안 달 아이콘 표시
        float held = 0f;
        while (!op.isDone)
        {
            held += Time.unscaledDeltaTime;
            if (op.progress >= 0.9f && held >= holdDuration)
                op.allowSceneActivation = true;
            yield return null;
        }

        // 페이드 인
        t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.unscaledDeltaTime;
            alpha = 1f - Mathf.Clamp01(t / fadeInDuration);
            yield return null;
        }
        alpha = 0f;
        active = false;
    }

    void OnGUI()
    {
        if (alpha <= 0f) return;

        float H = Screen.height;

        GUI.color = new Color(0f, 0f, 0f, alpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, H), Texture2D.whiteTexture);
        GUI.color = Color.white;
    }
}
