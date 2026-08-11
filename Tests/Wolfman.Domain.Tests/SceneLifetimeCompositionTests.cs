using System.Linq;
using NUnit.Framework;

/// <summary>
/// **무엇이 씬을 넘어 살아남는가** 를 씬 배치로 검사한다.
///
/// 이 프로젝트의 흐름 버그 세 개가 전부 같은 자리에서 나왔다: 사냥 씬의 `GameManager` 오브젝트 하나에
/// 수명이 다른 셋(`GameManager` 전역 · `SoundManager` 전역 · `MoonEffects` 씬 한정)이 얹혀 있었고,
/// 중복 판정이 `Destroy(gameObject)` 로 오브젝트를 통째로 지우면서 나머지 둘을 같이 죽였다.
/// 그 결과 전투 효과음이 전부 죽었고, 달 분위기 조명이 한 번도 적용되지 않았고,
/// 파괴된 사운드를 부른 달 공개 코루틴이 예외로 죽어 사냥이 시작되지 않았다.
///
/// 셋 다 **코드는 멀쩡하고 배치가 틀린** 버그라 컴파일도 도메인 테스트도 잡지 못했다.
/// 여기 담긴 것은 그 배치 규칙이다. 에디터 없이 WSL 에서 그대로 돈다.
/// </summary>
[TestFixture]
public class SceneLifetimeCompositionTests
{
    static readonly string[] AllScenes = { "TitleScene", "MainScene", "HuntScene", "VillageScene" };

    /// <summary>씬을 넘어 사는 것은 맨 처음 로드되는 씬에 한 벌만 둔다.</summary>
    [Test]
    public void 사운드는_살아남는_오브젝트에_하나만_있다()
    {
        // 살아남는 GameManager 오브젝트는 맨 처음 로드되는 TitleScene 의 것 하나뿐이다.
        // 뒤에 오는 씬의 GameManager 오브젝트는 전부 중복이라 통째로 파괴된다 —
        // 거기 사운드를 얹으면 소리가 나기도 전에 죽는다.
        Assert.That(UnitySceneFile.Load("TitleScene").ComponentCount("SoundManager"), Is.EqualTo(1),
            "TitleScene 에 SoundManager 가 없으면 게임 전체에 소리가 나지 않는다 — " +
            "살아남는 오브젝트가 여기 것 하나뿐이다.");

        foreach (string scene in AllScenes.Where(s => s != "TitleScene"))
            Assert.That(UnitySceneFile.Load(scene).ComponentCount("SoundManager"), Is.Zero,
                $"{scene} 에도 SoundManager 사본이 있다. 클립 연결이 두 곳으로 갈라져 " +
                "한쪽만 고치면 다른 쪽이 이기고 조용히 무시된다 — 사운드는 한 곳에만 둔다.");
    }

    /// <summary>씬과 함께 죽어야 하는 것은, 씬을 넘어 사는 오브젝트에 얹지 않는다.</summary>
    [Test]
    public void 달_분위기는_GameManager_오브젝트에_얹혀있지_않다()
    {
        UnitySceneFile hunt = UnitySceneFile.Load("HuntScene");

        Assert.That(hunt.ComponentCount("MoonEffects"), Is.EqualTo(1),
            "사냥 씬에 MoonEffects 가 없으면 그 밤의 달이 조명에 반영되지 않는다 (#25)");

        var sharedWithGameManager = hunt.GameObjectIdsWith("MoonEffects")
            .Intersect(hunt.GameObjectIdsWith("GameManager"))
            .ToList();

        Assert.That(sharedWithGameManager, Is.Empty,
            "MoonEffects 가 GameManager 와 같은 오브젝트에 있다. MoonEffects 는 그 씬의 " +
            "Global Light 2D 를 잡고 있어 씬과 함께 죽어야 하는데, GameManager 오브젝트는 " +
            "중복이면 통째로 파괴되므로 조명이 적용되기 전에 같이 죽는다.");
    }

    /// <summary>
    /// 손으로 편집한 씬 YAML 이 조용히 어긋나지 않았는지. CLAUDE.md 가 경고하는 그 파손이다.
    /// </summary>
    [Test]
    public void 모든_씬의_루트가_SceneRoots에_등록돼_있다()
    {
        foreach (string scene in AllScenes)
        {
            var missing = UnitySceneFile.Load(scene).RootTransformsMissingFromSceneRoots();

            Assert.That(missing, Is.Empty,
                $"{scene} 에 부모도 없고 SceneRoots 목록에도 없는 트랜스폼이 있다 " +
                $"(fileID {string.Join(", ", missing)}). 오브젝트는 존재하지만 씬의 루트 목록에서 " +
                "빠져 있어 하이어라키와 Scene.GetRootGameObjects() 에 나타나지 않는다.");
        }
    }
}
