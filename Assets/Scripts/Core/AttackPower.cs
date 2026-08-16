using UnityEngine;

/// <summary>
/// 지금 이 순간의 배율들을 모아 <see cref="AttackPowerRule"/> 에 넘긴다 (#38).
///
/// 규칙은 순수하게 두고 "누가 배율을 쥐고 있는가"만 여기서 안다.
/// 새 배율을 붙일 자리도 여기 한 곳이다 — 굶주림 페널티(#117)가 들어올 곳.
///
/// 공격 컴포넌트가 직접 <c>GameManager</c>·<c>SkillSystem</c> 을 뒤지지 않게 하는 게 핵심이다.
/// 발톱과 달빛 참격은 스킬이 바꾸는 배타적 모드라, 뒤지는 코드가 둘로 나뉘어 있으면
/// 한쪽에만 배율을 추가해도 다른 모드에서만 조용히 빠진 채로 통과한다.
/// </summary>
public static class AttackPower
{
    /// <summary>이 기본 데미지로 지금 때리면 적이 실제로 받는 피해.</summary>
    /// <param name="baseDamage">무기의 기본 피해. 발톱은 인스펙터 값, 참격은 프리팹 값 + 스킬 보너스.</param>
    public static int ForHit(int baseDamage)
    {
        // 달·스킬은 사냥 밖(마을 단독 재생, 테스트 씬)에서는 아직 없을 수 있다. 없으면 배율 없음.
        float moonPower = GameManager.Instance != null && GameManager.Instance.CurrentMoon != null
            ? GameManager.Instance.CurrentMoon.playerPowerMultiplier
            : 1f;

        float skillBonus = SkillSystem.Instance != null ? SkillSystem.Instance.damageBonus : 0f;

        return AttackPowerRule.Damage(baseDamage, skillBonus, moonPower);
    }
}
