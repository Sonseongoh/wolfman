using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

/// <summary>
/// 콜라이더 분리 회귀 테스트 (#143).
///
/// 콜라이더를 발밑(솔리드, 이동 차단)과 몸통(트리거, 피격)으로 쪼갠 변경은 두 갈래로 검증이 갈린다.
/// 값이 맞는지는 에디터에서 재면 되지만, **실제로 걸어 들어가지는가 / 한 번 때렸는데 두 번 깎이지 않는가**
/// 는 물리 스텝과 트리거 콜백이 도는 자리라 PlayMode 만이 맞는 seam 이다.
/// 특히 "캐릭터에 콜라이더가 둘이 됐다"는 사실 자체가 OnTrigger 계열을 두 번 부르는 새 위험을
/// 만들었고, 그건 정지 상태에서 값만 봐서는 절대 보이지 않는다.
///
/// 이 파일은 Assets/ 안에 두면 안 된다 (#108) — 돌릴 때만 버리는 사본의
/// Assets/Tests/PlayMode/ 로 복사한다. 실행 방법은 MoonRevealFlowTests.cs 머리 주석 참고.
/// </summary>
public class ColliderSplitTests : PlayModeTestBase
{
    /// <summary>
    /// AC 1·2 (#143): 묘비 뒤(위)로 걸어 들어가지고, 그때 묘비가 플레이어 앞에 그려진다.
    ///
    /// 이게 이 티켓의 본체다. 전에는 콜라이더가 스프라이트 전체라 최대 겹침이 0.05유닛(3픽셀)뿐이라
    /// 뒤로 들어갈 수도, Y 정렬이 보일 일도 없었다. 두 단언이 같이 서야 의미가 있다 —
    /// 겹치지 않으면 정렬은 공허하고, 정렬이 틀리면 겹쳐봐야 그림이 깨진다.
    /// </summary>
    [UnityTest]
    public IEnumerator 묘비_뒤로_걸어_들어가지고_그때_묘비가_앞에_그려진다()
    {
        yield return EnterHunt();

        GameObject player = GameObject.FindWithTag("Player");
        Assert.NotNull(player, "사냥 씬에 Player 가 없다");

        GameObject tomb = SpawnTomb(player.transform.position + new Vector3(0f, -1.05f, 0f));
        yield return Settle();

        Collider2D playerFoot = FootOf(player);
        Collider2D tombFoot = FootOf(tomb);
        Assert.NotNull(playerFoot, "플레이어에 발밑(솔리드) 콜라이더가 없다 — 분리가 안 됐다");
        Assert.NotNull(tombFoot, "묘비에 발밑 콜라이더가 없다");

        Assert.IsFalse(playerFoot.Distance(tombFoot).isOverlapped,
            "묘비 뒤에 설 수 없다 — 발밑이 겹친다. 장애물 콜라이더가 아직 그림 전체를 막고 있다 (#143)");

        SpriteRenderer ps = player.GetComponent<SpriteRenderer>();
        SpriteRenderer ts = tomb.GetComponent<SpriteRenderer>();
        Assert.IsTrue(ps.bounds.Intersects(ts.bounds),
            "플레이어와 묘비 스프라이트가 겹치지 않는다 — 겹치지 않으면 Y 정렬을 볼 일이 없다 (#125)");

        // YSort 는 LateUpdate 라 플레이 모드에서 실제로 돈다 — 값을 손으로 넣지 않는다
        Assert.Greater(ts.sortingOrder, ps.sortingOrder,
            $"묘비 위에 섰는데 플레이어가 앞에 그려진다 (player={ps.sortingOrder}, tomb={ts.sortingOrder}) — Y 정렬이 뒤집혔다");
    }

    /// <summary>
    /// AC 3 (#143): 걸어서 밑동을 통과할 수 없다. 뒤로 들어가진다고 해서 지나쳐버리면 장애물이 아니다.
    ///
    /// "파묻힌 상태에서 밀려나는가"는 일부러 보지 않는다. PlayerMovement.FixedUpdate 가 매 스텝
    /// rb.linearVelocity 를 통째로 덮어써서 밀려나려는 속도까지 지우기 때문인데, 그건 이 티켓과
    /// 무관한 기존 동작이고 게임에서 문제도 되지 않는다 — HuntTerrain 이 obstacleSafeRadius 로
    /// 플레이어 위에 장애물이 생기지 않게 막는다. 실제 요구는 "걸어서 못 지나간다"이므로
    /// 게임과 같은 방식(속도 기반 이동)으로 밀어붙여 본다.
    /// </summary>
    [UnityTest]
    public IEnumerator 걸어서_묘비_밑동을_통과할_수_없다()
    {
        yield return EnterHunt();

        GameObject player = GameObject.FindWithTag("Player");
        GameObject tomb = SpawnTomb(player.transform.position + new Vector3(0f, 2f, 0f));

        Collider2D playerFoot = FootOf(player);
        Collider2D tombFoot = FootOf(tomb);
        Assert.NotNull(playerFoot, "플레이어에 발밑(솔리드) 콜라이더가 없다");

        // 묘비 밑동 바로 아래, 겹치지 않는 자리에서 출발한다 (좌표는 런타임 콜라이더에서 계산)
        const float Gap = 0.5f;
        float startFootTop = tombFoot.bounds.min.y - Gap;
        player.transform.position = new Vector3(
            tombFoot.bounds.center.x - playerFoot.offset.x,
            startFootTop - playerFoot.bounds.extents.y - playerFoot.offset.y,
            player.transform.position.z);
        Physics2D.SyncTransforms();
        Assert.IsFalse(playerFoot.Distance(tombFoot).isOverlapped, "전제가 깨졌다 — 출발부터 겹쳐 있다");

        // 입력 대신 직접 민다. PlayerMovement 를 꺼야 매 스텝 속도를 덮어쓰지 않는다.
        var move = player.GetComponent<PlayerMovement>();
        if (move != null) move.enabled = false;
        var rb = player.GetComponent<Rigidbody2D>();
        Assert.NotNull(rb, "플레이어에 Rigidbody2D 가 없다");

        float startY = player.transform.position.y;
        for (int i = 0; i < 60; i++)   // 5유닛/초 × 1.2초 = 막히지 않으면 6유닛을 지나 완전히 통과할 거리
        {
            rb.linearVelocity = Vector2.up * 5f;
            yield return new WaitForFixedUpdate();
        }
        rb.linearVelocity = Vector2.zero;

        float traveled = player.transform.position.y - startY;
        float penetration = -playerFoot.Distance(tombFoot).distance; // 파고든 깊이 (접촉만이면 0에 가깝다)

        Assert.Greater(traveled, 0f,
            "플레이어가 아예 움직이지 않았다 — 테스트가 미는 데 실패했다");

        // 막혔다면 띄워둔 만큼만 가고 멈춘다. 안 막히면 6유닛을 갔을 거리다.
        Assert.Less(traveled, Gap + 0.5f,
            $"밑동에 막히지 않고 {traveled:F2}유닛을 갔다 (막혔다면 {Gap:F2} 부근에서 멈춰야 한다)");

        // isOverlapped 는 맞닿아 쉬는 상태에서도 true 다 — 접촉 오프셋 이상 파고들었는지를 본다
        Assert.Less(penetration, 0.05f,
            $"밑동을 {penetration:F3}유닛 파고들었다 — 접촉이 아니라 관통이다");
    }

    /// <summary>
    /// AC 4 (#143): 한 번 휘두르면 적은 한 번만 맞는다.
    ///
    /// 콜라이더가 둘이 되면서 새로 생긴 위험이다 — MeleeAttack 은 OverlapCircleAll 로 닿은 콜라이더를
    /// 전부 훑으므로, 가드가 없으면 같은 적을 발밑·몸통 두 번 때린다. 그래서 이 테스트는 먼저
    /// "정말 두 개가 잡히는지"를 확인해 위험이 실재함을 못박고, 그 다음 피해량이 한 번 분인지 본다.
    ///
    /// 기대 피해는 AttackPower.ForHit 에 그대로 물어본다 (#117 이 식을 그리로 모았다) —
    /// 여기서 공식을 복제하면 배율이 하나 늘 때마다 이 테스트가 조용히 틀린 값을 기대하게 된다.
    /// </summary>
    [UnityTest]
    public IEnumerator 한_번_휘두르면_적은_한_번만_맞는다()
    {
        yield return EnterHunt();

        GameObject player = GameObject.FindWithTag("Player");
        MeleeAttack melee = player.GetComponent<MeleeAttack>();
        Assert.NotNull(melee, "플레이어에 MeleeAttack 이 없다");

        EnemyHealth enemy = null;
        yield return WaitUntil(() => (enemy = Object.FindAnyObjectByType<EnemyHealth>()) != null);
        Assert.NotNull(enemy, $"{Timeout}초 안에 적이 스폰되지 않았다");

        // 사거리 안으로 끌어다 놓는다
        Vector3 target = player.transform.position + new Vector3(1.1f, 0f, 0f);
        enemy.transform.position = target;
        var chase = enemy.GetComponent<EnemyChase>();
        if (chase != null) chase.enabled = false;   // 추적으로 자리를 벗어나지 않게
        Physics2D.SyncTransforms();

        // 위험이 실재하는지 — 같은 적의 콜라이더가 둘 다 잡혀야 이 테스트가 의미 있다.
        // 나중에 콜라이더를 도로 하나로 합치면 여기가 먼저 깨져서, 이 테스트가 조용히
        // 이중 타격을 못 잡는 상태로 남지 않는다.
        int mine = 0;
        foreach (Collider2D c in Physics2D.OverlapCircleAll(target, melee.hitRadius))
            if (c.GetComponent<EnemyHealth>() == enemy) mine++;
        Assert.AreEqual(2, mine,
            $"적에게 걸린 콜라이더가 {mine}개다 — 2개(발밑+몸통)가 아니면 이 테스트는 이중 타격을 못 잡는다");

        int expected = AttackPower.ForHit(melee.baseDamage);
        int before = Hp(enemy);
        CallWith(melee, "Swing", target);
        yield return null;
        int drop = before - Hp(enemy);

        Assert.Greater(drop, 0, "적이 전혀 맞지 않았다 — 몸통 트리거로 피격이 들어가지 않는다");
        Assert.AreEqual(expected, drop,
            $"한 번 휘둘렀는데 {drop} 깎였다 (한 번 분은 {expected}) — 발밑까지 세어 두 번 때린 것으로 보인다 (#143)");
    }

    /// <summary>
    /// AC 5 (#143): 골드는 한 번만 적립된다. 위와 같은 이유로 픽업 트리거도 두 번 들어올 수 있다.
    /// </summary>
    [UnityTest]
    public IEnumerator 골드는_한_번만_적립된다()
    {
        yield return EnterHunt();

        GameObject player = GameObject.FindWithTag("Player");
        Assert.NotNull(CurrencyManager.Instance, "CurrencyManager 가 없다");

        const int Value = 7;   // 1 이면 이중 적립(2)과 우연한 값이 헷갈린다
        GameObject coin = new GameObject("TestCoin");
        coin.transform.position = player.transform.position;
        var circle = coin.AddComponent<CircleCollider2D>();
        circle.isTrigger = true;
        circle.radius = 0.2f;
        coin.AddComponent<SpriteRenderer>();
        var gold = coin.AddComponent<GoldCoin>();
        gold.value = Value;

        int before = CurrencyManager.Instance.TempGold;
        yield return Settle();

        int gained = CurrencyManager.Instance.TempGold - before;
        Assert.AreEqual(Value, gained,
            $"코인 하나({Value}G)를 주웠는데 {gained}G 가 들어왔다 — 플레이어 콜라이더 둘에 각각 반응해 이중 적립됐다 (#143)");
    }

    // ── 공통 ────────────────────────────────────────────────────────────────

    /// <summary>솔리드(=발밑) 콜라이더. 몸통은 트리거라 걸러진다.</summary>
    static Collider2D FootOf(GameObject go)
    {
        foreach (Collider2D c in go.GetComponents<Collider2D>())
            if (!c.isTrigger) return c;
        return null;
    }

    static GameObject SpawnTomb(Vector3 pos)
    {
        GameObject prefab = Resources.Load<GameObject>("Obstacles/BigTomb");
        Assert.NotNull(prefab, "Resources/Obstacles/BigTomb 를 찾지 못했다");
        GameObject tomb = Object.Instantiate(prefab, pos, Quaternion.identity);
        if (tomb.GetComponent<YSort>() == null) tomb.AddComponent<YSort>(); // HuntTerrain 이 런타임에 하는 일
        Physics2D.SyncTransforms();
        return tomb;
    }

    /// <summary>물리가 자리를 잡을 시간 — 밀려남·트리거 콜백이 도는 프레임을 넉넉히 준다.</summary>
    static IEnumerator Settle()
    {
        for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
        yield return null;   // LateUpdate(YSort) 가 한 번 더 도는 프레임
    }

    /// <summary>타이틀 → 마을 → 사냥. RunBoundaryTests 와 같은 경로를 지난다.</summary>
    IEnumerator EnterHunt()
    {
        SceneManager.LoadScene("TitleScene");
        yield return WaitForScene("TitleScene");

        var title = Object.FindAnyObjectByType<TitleScreen>();
        Assert.NotNull(title, "TitleScene 에 TitleScreen 이 없다");
        Call(title, "StartGame");
        yield return WaitForScene("VillageScene");

        MoonRevealUI reveal = null;
        yield return WaitUntil(() =>
        {
            if (reveal == null) reveal = Object.FindAnyObjectByType<MoonRevealUI>();
            return reveal != null && Get<bool>(reveal, "choosing");
        });
        Assert.NotNull(reveal, "마을에 MoonRevealUI 가 없다 — 달 공개가 시작되지 않았다 (#103)");
        Assert.IsTrue(Get<bool>(reveal, "choosing"),
            $"{Timeout}초 안에 달 공개가 선택 단계에 이르지 못했다 (timeScale={Time.timeScale})");

        reveal.Choose(MoonRevealUI.Choice.Hunt);
        yield return WaitForScene("HuntScene");
        yield return Settle();   // 지형·스폰이 자리 잡을 시간
    }

    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    static int Hp(EnemyHealth e) => (int)typeof(EnemyHealth).GetField("hp", Any).GetValue(e);

    /// <summary>인자 있는 사설 메서드 호출 — 베이스의 Call 은 무인자 전용이다.</summary>
    static void CallWith(object target, string method, params object[] args)
        => target.GetType().GetMethod(method, Any).Invoke(target, args);
}
