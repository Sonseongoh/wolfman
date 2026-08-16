using UnityEngine;

/// <summary>
/// 야성·굶주림 축의 씬 수명을 맡는 껍데기 (#117). 판정은 전부 <see cref="WildAxisCore"/> 가 한다.
///
/// **씬에 배치하지 않는다.** 처음 <see cref="Instance"/> 를 부르는 쪽에서 저절로 생기고
/// 그 뒤로 씬을 넘어 산다 — <c>CurrencyManager</c> 와 같은 꼴이다. 씬에 꽂아두면
/// 중복 판정이 <c>Destroy(gameObject)</c> 로 오브젝트를 통째로 지우면서, 같은 오브젝트에
/// 얹힌 다른 컴포넌트까지 함께 죽는다 (<c>SceneLifetimeCompositionTests</c> 가 기록한 사고 셋).
///
/// **스스로 돌지 않는다 — <c>Update()</c> 가 없다.** 지연 생성 싱글턴이라 아무도 부르지 않으면
/// 오브젝트 자체가 없고, 그러면 자기 Update 도 돌지 않아 축이 조용히 멈춘 채 게임이 진행된다.
/// 그 침묵이 제일 나쁜 실패라, 아예 스스로 돌지 않게 하고 **그 씬의 흐름 소유자**
/// (<c>WaveManager</c> · <c>VillageController</c>)가 <see cref="Advance(float)"/> 로 굴리게 했다.
/// 축이 안 도는 씬은 흐름 소유자가 없는 씬이고, 그건 축을 밀 이유도 없는 씬이다.
/// </summary>
public class WildAxisManager : LazySingleton<WildAxisManager>
{
    // core 를 밖으로 내보내지 않는다: 내보내면 Reset() 같은 런 수명 제어까지 아무 데서나
    // 부를 수 있게 된다. 소비자에게는 읽기값과 이름 붙은 사건(처치·폭주 소비)만 보인다.
    readonly WildAxisCore core = new WildAxisCore();

    /// <summary>축의 현재 위치. 음수면 굶주림 쪽, 양수면 야성 쪽 (게이지 표시용).</summary>
    public float Value => core.Value;

    /// <summary>지금 굶주림이 어느 단계인가.</summary>
    public HungerStage Stage => core.Stage;

    /// <summary>굶주림이 공격에 거는 배율 — <c>AttackPower.ForHit</c> 한 곳에서만 쓴다.</summary>
    public float AttackMultiplier => core.AttackMultiplier;

    /// <summary>굶주림이 깎는 최대 체력 — <c>PlayerHealth.EffectiveMaxHp</c> 가 쓴다.</summary>
    public int MaxHpPenalty => core.MaxHpPenalty;

    /// <summary>상점을 열 수 있는가 (#13 이 소비).</summary>
    public bool ShopOpen => core.ShopOpen;


    /// <summary>
    /// 흐른 시간만큼 축을 민다. 그 씬의 흐름 소유자가 매 프레임 부른다.
    /// 방향 판정은 코어가 하고, 여기서는 "지금 어느 페이즈이고 달이 정한 속도가 얼마인가"만 모은다.
    /// </summary>
    public void Advance(float dt)
    {
        RoundPhase? phase = GameManager.Instance != null ? GameManager.Instance.Phase : (RoundPhase?)null;

        // 달이 야성 상승 속도를 정한다 (보름달은 빠르게, 초승달은 천천히).
        // 사냥 씬만 단독 재생하는 개발 상황에서는 달이 없으므로 기본값으로 돈다.
        float rate = GameManager.Instance != null && GameManager.Instance.CurrentMoon != null
            ? GameManager.Instance.CurrentMoon.wildRisePerSecond
            : WildAxisCore.DefaultWildRisePerSecond;

        core.Advance(phase, dt, rate);
    }

    /// <summary>처치 = 먹기. 처치 깔때기(<c>WaveManager.NotifyEnemyDied</c>)가 부른다.</summary>
    /// <returns>이번 한 입의 회복량. 0 이면 회복 없음.</returns>
    public int NotifyKill() => core.OnKill();

    /// <summary>폭주 신호를 한 번 꺼내 간다 (#12 가 소비). 래치인 이유는 코어 주석 참조.</summary>
    public bool TryConsumeBreakout(out BreakoutSide side) => core.TryConsumeBreakout(out side);

    /// <summary>런이 새로 시작될 때 — 안전한 중앙으로. 밤이 바뀔 때가 아니다.</summary>
    public void ResetRun() => core.Reset();
}
