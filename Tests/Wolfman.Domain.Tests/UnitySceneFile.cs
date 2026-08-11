using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

/// <summary>
/// 유니티 씬(.unity)과 빌드 설정은 텍스트 YAML이라, 에디터 없이도 읽어서 검사할 수 있다.
///
/// 이 프로젝트는 동작의 절반이 코드가 아니라 "사람이 에디터에서 씬을 엮는 단계"에 들어있다.
/// 그 단계는 컴파일도 도메인 테스트도 잡아주지 않아서, 코드가 전부 초록인데도
/// 막상 눌러보면 빈 씬이 뜨는 일이 생긴다 (#11 — 마을에 플레이어를 안 넣은 채로 넘어간 사례).
/// 그 구멍을 메우는 테스트 전용 헬퍼다. WSL의 dotnet 루프에서 그대로 돈다.
/// </summary>
public class UnitySceneFile
{
    readonly string text;

    public string SceneName { get; }

    UnitySceneFile(string sceneName, string text)
    {
        SceneName = sceneName;
        this.text = text;
    }

    /// <summary>Assets/Scenes/{sceneName}.unity 를 읽는다.</summary>
    public static UnitySceneFile Load(string sceneName)
    {
        string path = Path.Combine(RepoRoot, "Assets", "Scenes", sceneName + ".unity");
        if (!File.Exists(path))
            throw new FileNotFoundException($"씬 파일이 없다: {path}");

        return new UnitySceneFile(sceneName, File.ReadAllText(path));
    }

    /// <summary>이 태그를 단 GameObject가 씬에 있는가. (예: "Player")</summary>
    public bool HasTaggedObject(string tag)
    {
        return text.Contains("m_TagString: " + tag);
    }

    /// <summary>
    /// 이 스크립트가 씬에서 컴포넌트로 붙은 횟수.
    /// 씬은 스크립트를 이름이 아니라 .meta 의 GUID 로 참조하므로, 파일명을 바꿔도 이 검사는 살아남는다.
    ///
    /// 스크립트 파일 자체가 없으면 0 이다 — 없는 스크립트는 어디에도 붙어 있을 수 없다.
    /// "이 컴포넌트는 씬에 없어야 한다"는 검사가 스크립트를 지웠다는 이유로 터지면 안 되기 때문이다:
    /// #28 이 PlayerLevel 을 지우자 마을 씬 구성 테스트가 실제로 그렇게 터졌다.
    /// </summary>
    public int ComponentCount(string scriptName)
    {
        string guid = FindScriptGuid(scriptName);
        return guid == null ? 0 : CountOccurrences(text, guid);
    }

    /// <summary>
    /// Assets/Scripts 아래에서 {scriptName}.cs.meta 를 찾아 GUID를 읽는다.
    /// 그런 스크립트가 없으면 null — 삭제된 스크립트를 묻는 것도 정당한 질문이다.
    /// </summary>
    public static string FindScriptGuid(string scriptName)
    {
        string scriptsDir = Path.Combine(RepoRoot, "Assets", "Scripts");
        string metaPath = Directory
            .EnumerateFiles(scriptsDir, scriptName + ".cs.meta", SearchOption.AllDirectories)
            .FirstOrDefault();

        if (metaPath == null) return null;

        string guidLine = File.ReadLines(metaPath).FirstOrDefault(l => l.StartsWith("guid:"));
        if (guidLine == null)
            throw new InvalidDataException($"{metaPath} 에 guid 줄이 없다");

        return guidLine.Substring("guid:".Length).Trim();
    }

    /// <summary>
    /// 이 씬이 빌드 설정에 등록돼 있고 켜져 있는가.
    /// 등록되지 않은 씬은 SceneManager.LoadScene 이 런타임에 실패한다.
    /// </summary>
    public static bool IsInBuildSettings(string sceneName)
    {
        string settingsPath = Path.Combine(RepoRoot, "ProjectSettings", "EditorBuildSettings.asset");
        string scenePath = "Assets/Scenes/" + sceneName + ".unity";

        string[] lines = File.ReadAllLines(settingsPath);
        for (int i = 0; i < lines.Length; i++)
        {
            if (!lines[i].Contains("path: " + scenePath)) continue;

            // 직렬화 형태:  - enabled: 1
            //                 path: Assets/Scenes/X.unity
            return i > 0 && lines[i - 1].Contains("enabled: 1");
        }
        return false;
    }

    /// <summary>
    /// 이 스크립트가 붙어 있는 GameObject 들의 fileID.
    ///
    /// "무엇이 같은 오브젝트에 얹혀 있는가" 를 묻기 위한 것이다. 한 오브젝트에 수명이 다른 것들이
    /// 섞여 있으면, 그 중 하나가 `Destroy(gameObject)` 를 부르는 순간 나머지가 같이 죽는다.
    /// </summary>
    public IReadOnlyCollection<string> GameObjectIdsWith(string scriptName)
    {
        var ids = new List<string>();
        string guid = FindScriptGuid(scriptName);
        if (guid == null) return ids;

        foreach (string block in Blocks())
        {
            if (!block.Contains("guid: " + guid)) continue;

            Match owner = Regex.Match(block, @"m_GameObject: \{fileID: (\d+)\}");
            if (owner.Success) ids.Add(owner.Groups[1].Value);
        }
        return ids;
    }

    /// <summary>
    /// 부모가 없는데 씬의 `SceneRoots` 목록에는 빠져 있는 트랜스폼들. 정상이면 비어 있다.
    ///
    /// 씬 YAML 을 손으로 편집해 오브젝트를 새로 만들면 딱 여기가 어긋난다 — 오브젝트는 존재하고
    /// `FindObjectsByType` 에도 잡히지만 씬의 루트 목록에는 없어서, 하이어라키 순회나
    /// `Scene.GetRootGameObjects()` 로는 보이지 않는다. 컴파일도 런타임도 아무 말을 안 해준다.
    /// </summary>
    public IReadOnlyCollection<string> RootTransformsMissingFromSceneRoots()
    {
        var rootTransforms = new List<string>();
        var registered = new HashSet<string>();

        foreach (string block in Blocks())
        {
            Match head = Regex.Match(block, @"^!u!(\d+) &(\d+)");
            if (!head.Success) continue;

            string unityClass = head.Groups[1].Value;
            string fileId = head.Groups[2].Value;

            // 4 = Transform, 224 = RectTransform
            if ((unityClass == "4" || unityClass == "224") && block.Contains("m_Father: {fileID: 0}"))
                rootTransforms.Add(fileId);

            if (unityClass == "1660057539") // SceneRoots
                foreach (Match m in Regex.Matches(block, @"- \{fileID: (\d+)\}"))
                    registered.Add(m.Groups[1].Value);
        }

        return rootTransforms.Where(id => !registered.Contains(id)).ToList();
    }

    /// <summary>씬 YAML 은 `--- !u!&lt;클래스&gt; &amp;&lt;fileID&gt;` 로 시작하는 문서들의 나열이다.</summary>
    IEnumerable<string> Blocks() => Regex.Split(text, @"^--- ", RegexOptions.Multiline).Skip(1);

    static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int at = haystack.IndexOf(needle, StringComparison.Ordinal);
        while (at >= 0)
        {
            count++;
            at = haystack.IndexOf(needle, at + needle.Length, StringComparison.Ordinal);
        }
        return count;
    }

    static string repoRoot;

    /// <summary>
    /// 테스트 어셈블리 위치(Tests/.../bin/Debug/net9.0)에서 위로 올라가며
    /// Assets 와 ProjectSettings 가 함께 있는 디렉터리를 찾는다 — 그게 유니티 프로젝트 루트다.
    /// </summary>
    public static string RepoRoot
    {
        get
        {
            if (repoRoot != null) return repoRoot;

            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Assets")) &&
                    Directory.Exists(Path.Combine(dir.FullName, "ProjectSettings")))
                {
                    repoRoot = dir.FullName;
                    return repoRoot;
                }
                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                $"프로젝트 루트를 찾지 못했다 (Assets + ProjectSettings 를 가진 상위 폴더 없음). 시작 위치: {AppContext.BaseDirectory}");
        }
    }
}
