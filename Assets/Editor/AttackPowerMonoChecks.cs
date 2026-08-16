using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// <see cref="AttackPowerRule"/> 를 **유니티(Mono) 안에서** 대조하는 회귀 검사 (#38, #117).
///
/// 왜 WSL 의 dotnet test 가 아니라 여기인가 —
/// Mono 는 float 곱셈의 중간값을 확장 정밀도로 들고 있고, 테스트가 도는 .NET 9 는 매 단계 float 로 접는다.
/// 딱 0.5 에 걸리는 조합에서 둘의 답이 갈린다: <c>Damage(100, 0.45f, 0.1f)</c> 는 에디터에서 15,
/// WSL 에서 14 다. 실제로 도는 것은 에디터 쪽 값이므로 **그 4건은 여기서만 못 박을 수 있다.**
/// (규칙 본문의 곱셈을 한 줄로 접으면 이 대조가 깨진다 — <c>AttackPowerRule.cs</c> 주석 참조.)
///
/// ⚠️ 이 파일을 <c>Wolfman.Domain.Tests.csproj</c> 에 링크하지 말 것. .NET 9 에서 돌면 고정 4건이
/// 어긋나는데, 그게 이 파일이 존재하는 이유다.
///
/// NUnit 도 유니티 테스트 러너도 쓰지 않는 평범한 정적 클래스다 — 테스트 러너에 기대면
/// <c>AssetsFolderHygieneTests</c> 가 막는다(그 어셈블리는 저장소 설정상 참조되지 않아
/// Assembly-CSharp 컴파일을 통째로 깨뜨린다). 그래서 검사이면서 테스트가 아니다.
/// 플레이어 빌드에도 들어가지 않는다 — <c>Assets/Editor/</c> 아래이기 때문이다.
/// 메뉴 <b>Wolfman / 검증 / 공격력 규칙 Mono 대조</b> 또는 <see cref="Run"/> 으로 돌린다.
/// </summary>
internal static class AttackPowerMonoChecks
{
    // 이 조합들에서 옛 인라인 식과 값이 같아야 한다. 스윕과 방향성 검사가 함께 도는 격자다.
    static readonly int[] BaseDamages = { 1, 2, 3, 5, 7, 10, 15, 20, 50, 100, 999 };
    static readonly float[] SkillBonuses = { 0f, 0.05f, 0.15f, 0.2f, 0.3f, 0.45f };
    static readonly float[] MoonPowers = { 0.1f, 0.5f, 0.7f, 0.85f, 1f, 1.5f, 2f, 3f };

    // 굶주림 단계가 거는 배율 (WildAxisCore.AttackMultiplier 와 같은 값, 깊어지는 순서).
    static readonly float[] HungerMultipliers = { 1f, 0.85f, 0.70f, 0.50f };

    [MenuItem("Wolfman/검증/공격력 규칙 Mono 대조")]
    static void RunFromMenu() => Debug.Log(Run());

    /// <summary>
    /// 스크립트를 고칠 때마다 저절로 돈다.
    ///
    /// 메뉴로만 돌리게 두면 아무도 안 눌러서 조용히 썩는다 — 그러면 이 파일이 지키려던
    /// 반올림 4건이 깨져도 아무도 모른다. CI 에 붙이려면 유니티 라이선스가 필요한데
    /// 이 저장소의 워크플로는 웹 배포 하나뿐이라, 도메인 리로드를 길목으로 삼았다.
    /// 에디터를 켠 사람이면 누구나 지나간다.
    ///
    /// **통과하면 아무 말도 하지 않는다** — 매 리로드마다 로그를 남기면 그게 소음이 되고,
    /// 소음이 되면 진짜 실패도 묻힌다.
    /// </summary>
    [InitializeOnLoadMethod]
    static void RunOnDomainReload()
    {
        if (TryFindFailures(out string report)) Debug.LogError(report);
    }

    /// <summary>대조를 돌리고 결과를 한 줄짜리 문자열로 돌려준다 (실패가 있으면 목록까지).</summary>
    public static string Run()
    {
        TryFindFailures(out string report);
        return report;
    }

    /// <summary>대조를 돌린다. 실패가 하나라도 있으면 true.</summary>
    public static bool TryFindFailures(out string report)
    {
        var failures = new List<string>();
        int checks = 0;

        checks += 고정값을_대조한다(failures);
        checks += 굶주릴수록_약해진다(failures);

        if (failures.Count == 0)
        {
            report = $"[AttackPowerMonoChecks] OK: {checks}건";
            return false;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"[AttackPowerMonoChecks] 실패 {failures.Count}건 / 검사 {checks}건");
        foreach (string f in failures) sb.AppendLine("  " + f);
        report = sb.ToString();
        return true;
    }

    /// <summary>
    /// WSL 이 재현하지 못해 도메인 스위트에서 뺀 4건. Mono 실측값이다.
    /// 여기가 어긋나면 규칙 본문의 곱셈 구조가 바뀐 것이다.
    /// </summary>
    static int 고정값을_대조한다(List<string> failures)
    {
        Expect(failures, AttackPowerRule.Damage(10, 0.05f, 3f, 1f), 31, "Damage(10, 0.05, 3)");
        Expect(failures, AttackPowerRule.Damage(100, 0.05f, 0.7f, 1f), 73, "Damage(100, 0.05, 0.7)");
        Expect(failures, AttackPowerRule.Damage(100, 0.3f, 0.85f, 1f), 110, "Damage(100, 0.3, 0.85)");
        Expect(failures, AttackPowerRule.Damage(100, 0.45f, 0.1f, 1f), 15, "Damage(100, 0.45, 0.1)");
        return 4;
    }

    // 한때 "4-인자가 3-인자와 같은 값을 내는가"를 528 조합으로 대조했다. 3-인자 오버로드를
    // 없앤 지금은 지울 수밖에 없다 — 남겨두면 자기 자신과 비교하는 항등식이라 절대 실패하지 않는다.
    // 그때 지키려던 것(오버로드 추가가 기존 수치를 건드리지 않았는가)은 아래 고정 4건이 이어받는다.

    /// <summary>
    /// 굶주림이 깊어질수록 피해가 줄기만 하고(단조 비증가), 그래도 0 으로는 떨어지지 않는다.
    /// 최소 1 이 무너지면 아사 데드락이 "적이 안 죽는다"라는 진짜 데드락이 된다.
    /// </summary>
    static int 굶주릴수록_약해진다(List<string> failures)
    {
        int checks = 0;
        foreach (int b in BaseDamages)
            foreach (float s in SkillBonuses)
                foreach (float m in MoonPowers)
                {
                    int previous = int.MaxValue;
                    foreach (float h in HungerMultipliers)
                    {
                        int dmg = AttackPowerRule.Damage(b, s, m, h);

                        if (dmg > previous)
                            failures.Add($"굶주림이 깊어졌는데 피해가 늘었다: Damage({b}, {s}, {m}, {h}) = {dmg} > {previous}");
                        if (dmg < AttackPowerRule.MinimumDamage)
                            failures.Add($"최소 피해가 무너졌다: Damage({b}, {s}, {m}, {h}) = {dmg}");

                        previous = dmg;
                        checks++;
                    }
                }
        return checks;
    }

    static void Expect(List<string> failures, int actual, int expected, string what)
    {
        if (actual != expected) failures.Add($"{what} → {actual} (기대 {expected})");
    }
}
