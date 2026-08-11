using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

/// <summary>
/// `Assets/` 안에 들어와서는 안 되는 것을 막는다.
///
/// 유니티는 `Assets/` 아래의 모든 `.cs` 를 Assembly-CSharp 로 컴파일한다. 거기 컴파일되지 않는
/// 파일이 하나라도 섞이면 **프로젝트 전체가 열리지 않는다** — 그 파일과 무관한 사람까지 전부 막힌다.
/// 컴파일 검증은 유니티 안에서만 가능해서(WSL 에는 UnityEngine.dll 이 없다) 이쪽에서는
/// 컴파일을 흉내낼 수 없다. 대신 **들어오면 안 되는 것이 들어왔는지**는 파일만 보고 알 수 있다.
/// </summary>
[TestFixture]
public class AssetsFolderHygieneTests
{
    /// <summary>
    /// PlayMode 테스트 코드는 `Assets/` 밖에 둔다 (#108).
    ///
    /// `UnityTest`·`UnitySetUp` 은 `UnityEngine.TestRunner.dll` 에 있는데, 이 어셈블리는
    /// `playModeTestRunnerEnabled` 가 켜져 있을 때만 Assembly-CSharp 에 참조된다. 저장소에서는
    /// 꺼두므로(유니티가 deprecated 처리했고 플레이어 빌드에 영향을 준다) 그런 파일이 `Assets/`
    /// 안에 있으면 Assembly-CSharp 가 통째로 컴파일에 실패한다.
    ///
    /// `#if UNITY_INCLUDE_TESTS` 로 막으려는 시도는 통하지 않는다 — 그 define 은 플래그와 무관하게
    /// 항상 켜져 있어서 가드를 그냥 통과한다. 실제로 그렇게 dev 를 한 번 깨뜨렸다.
    /// </summary>
    [Test]
    public void Assets_아래에_PlayMode_테스트_코드가_없다()
    {
        string assets = Path.Combine(UnitySceneFile.RepoRoot, "Assets");

        var offenders = new List<string>();
        foreach (string path in Directory.EnumerateFiles(assets, "*.cs", SearchOption.AllDirectories))
        {
            string src = File.ReadAllText(path);
            if (src.Contains("UnityEngine.TestTools") ||
                src.Contains("[UnityTest]") ||
                src.Contains("[UnitySetUp]") ||
                src.Contains("[UnityTearDown]"))
            {
                offenders.Add(Path.GetRelativePath(UnitySceneFile.RepoRoot, path));
            }
        }

        Assert.That(offenders, Is.Empty,
            "Assets/ 안에 PlayMode 테스트 코드가 있다:\n  " + string.Join("\n  ", offenders) + "\n\n" +
            "저장소는 playModeTestRunnerEnabled 를 꺼두므로 UnityEngine.TestRunner.dll 이 참조되지 않는다. " +
            "이 파일들은 Assembly-CSharp 컴파일을 통째로 실패시켜 프로젝트가 열리지 않게 만든다. " +
            "PlayMode 테스트는 Tests/PlayMode/ 에 두고, 돌릴 때만 버리는 사본으로 복사해 넣을 것.");
    }
}
