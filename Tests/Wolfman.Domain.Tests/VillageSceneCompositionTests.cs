using NUnit.Framework;

/// <summary>
/// 마을 씬이 실제로 플레이 가능한 상태로 엮여 있는지 검사한다 (#11).
///
/// 코드가 아니라 "사람이 에디터에서 한 씬 배치"를 검사하는 테스트다.
/// 이게 없으면 컴파일도 도메인 테스트도 전부 초록인데 "마을 남기"를 누르면 빈 화면이 뜬다.
/// 씬을 엮기 전까지는 빨간 게 정상이며, 그 실패 목록이 그대로 에디터 작업 체크리스트가 된다.
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
    public void 마을에는_전투_컴포넌트가_없다()
    {
        // 마을은 전투가 없는 페이즈다 (습격은 #12). 사냥용 컴포넌트를 그대로 복사해오면
        // 적도 없는데 자동 발사가 돌고 레벨업 UI 가 뜬다.
        Assert.Multiple(() =>
        {
            Assert.That(scene.ComponentCount("PlayerAttack"), Is.Zero, "마을에 PlayerAttack 이 붙어 있다");
            Assert.That(scene.ComponentCount("PlayerLevel"), Is.Zero, "마을에 PlayerLevel 이 붙어 있다");
            Assert.That(scene.ComponentCount("MeleeAttack"), Is.Zero, "마을에 MeleeAttack 이 붙어 있다");
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
