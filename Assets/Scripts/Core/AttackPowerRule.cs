using System;

/// <summary>
/// 한 번의 타격이 적에게 넣는 피해를 정한다 (#38).
///
/// 발톱(<c>MeleeAttack</c>)과 달빛 참격(<c>PlayerAttack</c>)은 같은 식을 쓴다.
/// 두 경로가 동시에 도는 게 아니라 스킬이 바꾸는 배타적 모드이기 때문에,
/// 식을 양쪽에 복사해 두면 한쪽에만 새 배율을 넣는 실수가 테스트를 통과해 버린다.
/// 실제로 그 상태였고, 굶주림 페널티(#117)를 얹기 전에 여기로 모았다.
///
/// UnityEngine 에 의존하지 않아 WSL 에서 테스트할 수 있다.
/// 반올림은 <c>Mathf.RoundToInt</c> 와 같은 은행가 반올림이고, 곱셈도 float 로 한다 —
/// double 로 넓히면 5 × 0.7 이 3.5 가 아니라 3.4999999 가 되어 기존 수치가 조용히 1 줄어든다.
/// </summary>
public static class AttackPowerRule
{
    /// <summary>피해가 0 이 되면 "약해졌다"가 아니라 "고장났다"로 읽히므로 여기서 멈춘다.</summary>
    public const int MinimumDamage = 1;

    /// <summary>
    /// 이 타격이 넣는 피해.
    /// </summary>
    /// <param name="baseDamage">무기가 가진 기본 피해. 발톱은 인스펙터 값, 참격은 프리팹 값.</param>
    /// <param name="skillDamageBonus">
    /// 스킬 공격력 %보너스의 합 (0.15 = +15%). 곱연산이 아니라 합연산이다 —
    /// 같은 스킬을 여러 장 쌓았을 때 벌어지지 않게 하려는 의도된 선택이다.
    /// </param>
    /// <param name="moonPowerMultiplier">그 밤의 달이 주는 플레이어 강화 배율. 달이 없으면 1.</param>
    public static int Damage(int baseDamage, float skillDamageBonus, float moonPowerMultiplier)
    {
        // 중간값을 float 지역변수에 담는 것까지 옛 인라인 식과 똑같이 맞춰 뒀다.
        // 괄호로 묶어 한 줄로 접으면 곱셈이 확장 정밀도로 남을 수 있어 990개 조합 중 4개가 어긋난다.
        float skillMult = 1f + skillDamageBonus;
        float raw = baseDamage * skillMult * moonPowerMultiplier;

        return Math.Max(MinimumDamage, (int)Math.Round((double)raw, MidpointRounding.ToEven));
    }
}
