using UnityEngine;

/// <summary>
/// 달 전체 목록과 확률 추첨. 에셋 1개를 만들어 GameManager에 꽂는다.
/// </summary>
[CreateAssetMenu(fileName = "MoonTable", menuName = "Wolfman/Moon Table")]
public class MoonTable : ScriptableObject
{
    public MoonData[] moons;

    /// <summary>appearChance를 가중치로 한 확률 추첨</summary>
    public MoonData Draw()
    {
        if (moons == null || moons.Length == 0) return null;

        float total = 0f;
        foreach (MoonData m in moons) total += m.appearChance;

        float roll = Random.Range(0f, total);
        foreach (MoonData m in moons)
        {
            roll -= m.appearChance;
            if (roll <= 0f) return m;
        }
        return moons[moons.Length - 1];
    }
}
