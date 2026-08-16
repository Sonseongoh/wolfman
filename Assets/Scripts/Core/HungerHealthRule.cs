/// <summary>
/// 굶주림이 최대 체력을 눌렀을 때 현재 체력을 어떻게 다루는가 (#117).
///
/// 굶주림은 **피해가 아니라 눌러두는 것**이다. 최대치가 3 으로 내려가면 체력 5 도 3 이 되어야
/// 페널티가 다음 피격까지 미뤄지지 않지만, 굶주림이 풀리면 눌렸던 만큼은 돌아와야 한다.
/// 안 돌려주면 단계가 오르내릴 때마다 맞지도 않은 체력이 계단식으로 사라져,
/// 마을과 사냥을 오갈수록 아무 이유 없이 약해진다.
///
/// 눌러둔 양(<c>held</c>)을 따로 세는 이유가 그것이다 — 눌린 것만 돌려주고,
/// 실제로 맞아서 잃은 것은 돌려주지 않는다.
///
/// UnityEngine 에 의존하지 않아 WSL 에서 검사할 수 있다.
/// </summary>
public static class HungerHealthRule
{
    /// <summary>정산 결과 — 지금의 체력과, 굶주림이 아직 눌러두고 있는 양.</summary>
    public readonly struct Settled
    {
        public readonly int Hp;
        public readonly int Held;

        public Settled(int hp, int held)
        {
            Hp = hp;
            Held = held;
        }
    }

    /// <summary>
    /// 최대치가 바뀐 뒤의 체력을 정한다. 매 프레임 불러도 같은 답을 주는 순수 함수다.
    /// </summary>
    /// <param name="hp">지금 체력.</param>
    /// <param name="held">굶주림이 지금까지 눌러둔 누계.</param>
    /// <param name="cap">굶주림 페널티까지 반영한 지금의 최대 체력.</param>
    public static Settled Settle(int hp, int held, int cap)
    {
        if (held < 0) held = 0;

        // 최대치가 내려갔다 — 넘치는 만큼을 눌러둔다 (잃은 게 아니라 맡아둔 것).
        if (hp > cap) return new Settled(cap, held + (hp - cap));

        // 최대치가 올라갔고 맡아둔 것이 있다 — 빈자리만큼 돌려준다.
        // 맞아서 생긴 빈자리까지 채워주지 않도록 맡아둔 양으로 상한을 건다.
        if (held > 0 && hp < cap)
        {
            int giveBack = held < cap - hp ? held : cap - hp;
            return new Settled(hp + giveBack, held - giveBack);
        }

        return new Settled(hp, held);
    }
}
