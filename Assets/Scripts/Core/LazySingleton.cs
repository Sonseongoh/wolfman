using UnityEngine;

/// <summary>
/// 씬에 배치하지 않고, 처음 <see cref="Instance"/> 를 부르는 쪽에서 저절로 생기는 싱글턴.
/// 그 뒤로는 씬을 넘어 산다.
///
/// **씬에 꽂지 말 것.** 꽂아두면 중복 판정이 <c>Destroy(gameObject)</c> 로 오브젝트를 통째로
/// 지우면서, 같은 오브젝트에 얹힌 다른 컴포넌트까지 함께 죽는다
/// (<c>SceneLifetimeCompositionTests</c> 가 기록한 사고 셋이 전부 그 형태다).
/// 지연 생성이므로 씬 편집이 아예 필요 없다.
///
/// **플레이 모드 밖에서도 안전하다.** <c>DontDestroyOnLoad</c> 는 에디터에서 예외를 던져서,
/// 가드가 없으면 잔액이나 축 값을 에디터에서 읽어보는 것만으로 터진다 — 이 저장소는 유니티를
/// 에디터 모드로 띄워 검증하는 일이 잦아 그게 실제로 검증을 막았다. 에디터에서는 대신
/// 씬에 저장되지 않는 임시 오브젝트로 만든다.
///
/// **스스로 도는 <c>Update</c> 를 두지 말 것.** 아무도 <see cref="Instance"/> 를 안 부르면
/// 오브젝트 자체가 없어서 그 <c>Update</c> 도 돌지 않는다. 시간이 흘러야 하는 값이라면
/// 그 씬의 흐름 소유자가 밖에서 굴려야 한다 (<c>WildAxisManager.Advance</c> 참고).
/// </summary>
/// <typeparam name="T">구현 클래스 자신. <c>class CurrencyManager : LazySingleton&lt;CurrencyManager&gt;</c> 꼴로 쓴다.</typeparam>
public abstract class LazySingleton<T> : MonoBehaviour where T : LazySingleton<T>
{
    static T _instance;

    /// <summary>없으면 만들어서 준다. null 을 돌려주지 않는다 (앱 종료 중은 예외).</summary>
    public static T Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject(typeof(T).Name);
                _instance = go.AddComponent<T>();

                if (Application.isPlaying) DontDestroyOnLoad(go);
                else go.hideFlags = HideFlags.HideAndDontSave;
            }
            return _instance;
        }
    }

    /// <summary>
    /// 그래도 누가 씬에 꽂았을 때의 방어.
    ///
    /// **구현 클래스는 <c>Awake</c> 를 직접 만들지 말 것.** 만들면 이쪽을 가려서 중복 방어가
    /// 통째로 사라지고, 값이 둘로 갈린 채 조용히 돌아간다. 초기화할 것이 있으면
    /// <see cref="OnSingletonAwake"/> 를 재정의한다 — 그러면 <c>base</c> 호출을 잊을 수가 없다.
    /// </summary>
    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = (T)this;
        if (Application.isPlaying) DontDestroyOnLoad(gameObject);

        OnSingletonAwake();
    }

    /// <summary>
    /// 이 인스턴스가 "그 하나"로 확정된 뒤에 한 번 불린다. 중복이라 지워지는 쪽에서는 불리지 않는다.
    /// 구독 같은 초기화를 여기 둔다 (<c>CurrencyManager</c> · <c>WildAxisManager</c> 가 런 경계를 구독한다).
    /// </summary>
    protected virtual void OnSingletonAwake() { }
}
