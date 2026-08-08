using NUnit.Framework;

/// <summary>
/// 마을 씬이 실제로 플레이 가능한 상태로 엮여 있는지 검사한다 (#11).
///
/// 코드가 아니라 "사람이 에디터에서 한 씬 배치"를 검사하는 테스트다.
/// 이게 없으면 컴파일도 도메인 테스트도 전부 초록인데 "마을 남기"를 누르면 빈 화면이 뜬다.
///
/// 여기 담긴 기준은 스펙이 문자로 요구한 목록이 아니라, **손으로 돌려서 통과시킨 구성**이다
/// (이동·파괴 전이·수리 성공/실패·라운드 루프 반복·사냥 귀환 회귀까지 확인한 그 배치).
/// 그러니 이 테스트가 빨개지면 "규칙 위반"이 아니라 "검증된 구성에서 벗어났다"는 신호로 읽으면 된다 —
/// 배치를 의도적으로 바꾸는 중이라면 여기 기준도 같이 고치는 게 맞다.
/// </summary>
[TestFixture]
public class VillageSceneCompositionTests
{
    const string Village = "VillageScene";

    UnitySceneFile scene;

    [SetUp]
    public void SetUp()
    {
        scene = UnitySceneFile.Load(Village);
    }

    [Test]
    public void 마을에_플레이어가_있다()
    {
        Assert.That(scene.HasTaggedObject("Player"), Is.True,
            "마을 씬에 Player 태그를 단 오브젝트가 없다 — 마을에 들어가면 조작할 대상이 사라진다. " +
            "CameraFollow 와 FacilityHealth 도 이 태그로 플레이어를 찾는다.");
    }

    [Test]
    public void 마을_플레이어는_움직일_수_있다()
    {
        Assert.That(scene.ComponentCount("PlayerMovement"), Is.EqualTo(1),
            "마을 플레이어에 PlayerMovement 가 정확히 하나 붙어 있어야 WASD 로 움직인다.");
    }

    [Test]
    public void 마을_플레이어는_형태를_결정하는_컴포넌트를_가진다()
    {
        // 이게 없으면 마을 플레이어는 씬에 직렬화된 스프라이트 그대로 남는다 — #53 이전엔 그래서
        // 인간도 늑대도 아닌 기본 네모가 보였다. 형태는 페이즈에서 오지, 씬에 굳어있지 않다.
        Assert.That(scene.ComponentCount("PlayerTransform"), Is.EqualTo(1),
            "마을 플레이어에 PlayerTransform 이 하나 붙어 있어야 마을 페이즈에서 인간으로 보인다.");
    }

    [Test]
    public void 마을에는_전투_컴포넌트가_없다()
    {
        // 스펙은 전투 컴포넌트를 "빼도 된다"고 허용했을 뿐 금지하진 않았다.
        // 뺀 구성으로 검증했으니 그 상태를 지킨다 — 마을엔 적이 없어서(습격은 #12)
        // 사냥용 플레이어를 통째로 복사해오면 아무도 없는데 자동 발사가 돌고 레벨업 UI 가 뜬다.
        // 마을에서 전투를 하기로 결정이 바뀌면 이 테스트부터 고칠 것.
        Assert.Multiple(() =>
        {
            Assert.That(scene.ComponentCount("PlayerAttack"), Is.Zero, "검증된 마을 구성에 없던 PlayerAttack 이 붙었다");
            Assert.That(scene.ComponentCount("PlayerLevel"), Is.Zero, "검증된 마을 구성에 없던 PlayerLevel 이 붙었다");
            Assert.That(scene.ComponentCount("MeleeAttack"), Is.Zero, "검증된 마을 구성에 없던 MeleeAttack 이 붙었다");
        });
    }

    [Test]
    public void 마을에_VillageController가_하나_있다()
    {
        Assert.That(scene.ComponentCount("VillageController"), Is.EqualTo(1),
            "VillageController 가 없으면 금고 잔액 HUD 도, 라운드 종료 버튼도 뜨지 않는다 — 마을에서 빠져나올 수 없다.");
    }

    [Test]
    public void 마을에_시설이_두_채_이상_있다()
    {
        Assert.That(scene.ComponentCount("FacilityHealth"), Is.GreaterThanOrEqualTo(2),
            "인수 기준: 시설이 2채 이상이어야 한다.");
    }

    [Test]
    public void 마을_카메라가_플레이어를_따라간다()
    {
        // 스펙에 명시된 요구는 아니다 — 다만 이게 없으면 플레이어가 화면 밖으로 걸어나가고,
        // "마을에서 플레이어를 조작할 수 있다"가 사실상 성립하지 않아 검증 구성에 포함했다.
        Assert.That(scene.ComponentCount("CameraFollow"), Is.EqualTo(1),
            "CameraFollow 가 없으면 플레이어가 화면 밖으로 걸어나간다.");
    }

    [Test]
    public void 마을_씬은_빌드_설정에_등록돼_있다()
    {
        Assert.That(UnitySceneFile.IsInBuildSettings(Village), Is.True,
            "빌드 설정에 없는 씬은 SceneManager.LoadScene 이 런타임에 실패한다.");
    }
}

/// <summary>
/// 마을 작업이 전투 씬을 건드리지 않았는지 지키는 회귀 테스트 (#11).
/// 마을 플레이어는 프리팹 추출이 아니라 마을 전용 최소 구성으로 만들기로 했으므로,
/// 전투 씬의 플레이어는 그대로 남아 있어야 한다.
/// </summary>
[TestFixture]
public class HuntSceneRegressionTests
{
    const string Hunt = "SampleScene";

    [Test]
    public void 전투_씬은_여전히_플레이어를_가진다()
    {
        UnitySceneFile scene = UnitySceneFile.Load(Hunt);

        Assert.Multiple(() =>
        {
            Assert.That(scene.HasTaggedObject("Player"), Is.True, "전투 씬에서 Player 태그가 사라졌다");
            Assert.That(scene.ComponentCount("PlayerMovement"), Is.EqualTo(1), "전투 씬 플레이어의 PlayerMovement 가 사라졌다");
            Assert.That(scene.ComponentCount("PlayerAttack"), Is.EqualTo(1), "전투 씬 플레이어의 PlayerAttack 이 사라졌다");
        });
    }
}
