# 웹 빌드와 배포

제출용 웹 빌드를 뜨고 GitHub Pages에 올리는 절차. **왜** 이 방식인지는 [ADR 0003](adr/0003-제출-빌드는-웹으로-내고-깃허브-페이지에-올린다.md)에 있다. 여기는 **어떻게** 하는지만 적는다.

## 확정 링크

**https://sonseongoh.github.io/wolfman/**

이 링크는 고정이다. 심사 기간 동안 빌드만 갈아끼운다 — 링크를 다시 제출할 일이 없도록 고른 방식이다.

| | |
|---|---|
| 소스 브랜치 | `dev` |
| 배포 브랜치 | `gh-pages` (orphan, 빌드 산출물만) |
| Pages 설정 | `gh-pages` / (root) |

## 배포하는 두 가지 방법

| | `scripts/deploy-web.sh` | GitHub Actions "웹 배포" |
|---|---|---|
| 누가 | 유니티가 설치된 사람 | 저장소에 접근 가능한 누구나 |
| 빌드하는 곳 | 내 PC | Unity Build Automation (클라우드) |
| 걸리는 시간 | 약 10분 | 약 40분 (큐 대기 + 빌드 27분) |
| 제약 | 유니티 필요 | 무료 빌드 분 **월 200분** |

```bash
./scripts/deploy-web.sh              # dev 최신을 빌드해서 배포
./scripts/deploy-web.sh --dry-run    # 빌드까지만, push 하지 않는다
```

Actions 쪽은 저장소의 **Actions 탭 → "웹 배포" → Run workflow**로 실행한다. 브랜치와 커밋을 지정할 수 있다.

**둘 다 수동이다. push 하면 자동으로 배포되는 경로는 없다.** 클라우드 빌드가 1회 약 27분인데 무료 한도가 월 200분이라 커밋마다 돌리면 한 달을 못 버틴다. 그리고 배포는 "지금 이 리비전을 심사자에게 보이겠다"는 판단이라 사람이 누르는 편이 맞다.

아래 1~5장은 두 방법이 실제로 하는 일을 손으로 적은 것이다. **자동화가 막혔을 때 여기로 돌아온다.**

### Actions 배포를 쓰기 전에 (한 번만)

저장소 시크릿 **`UNITY_BUILD_API_KEY`** 가 필요하다. Unity Cloud 대시보드의 Build Automation → Settings 에서 API key 를 복사해 `Settings → Secrets and variables → Actions` 에 넣는다. CLI 로는 값을 화면에 노출하지 않고 넣을 수 있다:

```bash
tr -d ' \t\r\n' < ~/.unity-build-token | gh secret set UNITY_BUILD_API_KEY --repo Sonseongoh/wolfman
```

**등록에 admin 은 필요 없다 — collaborator 면 된다.** 이 저장소는 개인 계정 소유이고, GitHub 문서가 개인 계정 저장소는 "you must be a repository collaborator", REST API 는 "collaborator access" 라고 못박는다. 실제로 `admin: false, push: true` 인 계정으로 등록된다.

한 번 등록하면 **값을 다시 읽을 수 없다.** 이름과 갱신 시각만 조회된다:

```bash
gh api repos/Sonseongoh/wolfman/actions/secrets
```

그래서 키 원본은 Unity Cloud 대시보드에서 언제든 다시 볼 수 있다는 것만 기억하면 된다. 유출됐다면 거기서 재발급한다.

클라우드 쪽 설정은 이렇게 잡혀 있다:

| | |
|---|---|
| 조직 / 프로젝트 | `wonu606` / `wolfman` |
| 빌드 타깃 | `webgl-dev` (WebGL, `dev`, Windows) |
| 에디터 버전 | `6000_5_6f1` — 프로젝트를 올리면 여기도 같이 올려야 한다 |
| 자동 빌드 | **꺼져 있다** |

무료 한도를 넘기면 청구되는 게 아니라 **프로젝트가 잠긴다**(결제 수단을 등록하지 않은 경우). 사용량은 대시보드의 Usage 에서 본다.

## 1. 무엇을 빌드할지 정한다

`dev`의 **깨끗한 체크아웃**에서 빌드한다. 작업 중인 워킹 트리에서 뜨면 커밋되지 않은 변경이 라이브로 새어 나간다.

```bash
git clone --branch dev --single-branch gh-personal:Sonseongoh/wolfman.git wolfman-build
cd wolfman-build
git lfs pull          # 아트·오디오는 LFS 에 있다
```

유니티 에디터가 다른 프로젝트 폴더를 열고 있어도 상관없다 — 별도 디렉터리라 잠금이 겹치지 않는다.

## 2. 빌드 설정을 확인한다

ADR 0003이 세 값을 못박았다. 하나라도 어긋나면 **로더가 죽거나** 갱신이 브라우저 캐시에 막힌다.

```bash
grep -E "webGLCompressionFormat|webGLDecompressionFallback|webGLNameFilesAsHashes" \
  ProjectSettings/ProjectSettings.asset
```

| 기대값 | 의미 |
|---|---|
| `webGLCompressionFormat: 0` | Brotli |
| `webGLDecompressionFallback: 1` | 브라우저가 직접 푼다. **Pages는 응답 헤더를 못 붙이므로 이게 꺼져 있으면 로더가 죽는다** |
| `webGLNameFilesAsHashes: 1` | 내용이 바뀌면 URL이 바뀐다. 캐시 문제를 원천 차단 |

`playModeTestRunnerEnabled: 0` 인지도 같이 본다. 켜져 있으면 테스트 어셈블리가 플레이어 빌드에 딸려간다 ([#108](https://github.com/Sonseongoh/wolfman/issues/108)).

## 3. 빌드한다

### 에디터에서

`File > Build Profiles` → **Web** 프로필 선택 → `Build` → 출력 폴더를 `Build/Web` 으로.

### 배치모드에서 (WSL에서 무인 실행)

```bash
"/mnt/c/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Unity.exe" \
  -batchmode -nographics \
  -projectPath 'C:\...\wolfman-build' \
  -buildTarget WebGL \
  -executeMethod WebBuilder.Build \
  -logFile 'C:\...\wolfman-build\Build.log'
echo "exit: $?"     # 0 = 성공
```

`Assets/Editor/WebBuilder.cs`가 진입점이다. 성공하면 로그에 이렇게 남는다:

```
[WebBuilder] scenes (4): TitleScene, MainScene, HuntScene, VillageScene
[WebBuilder] compression=Brotli fallback=True hashes=True
[WebBuilder] result=Succeeded errors=0 warnings=7 size=28615592
```

씬 목록·압축 설정이 로그에 찍히므로 **빌드 후에 확인**할 수 있다. 씬이 4개가 아니거나 압축이 다르면 그 빌드는 올리지 않는다.

빌드가 실패하면 스크립트 컴파일부터 의심한다:

```bash
grep -c "error CS" Build.log
```

## 4. 배포한다

> **덮어쓰지 말 것.** 해시 파일명을 쓰므로 덮어쓰면 옛 빌드의 파일이 지워지지 않고 영원히 쌓인다.
> **폴더를 비우고 새로 채운다.** `.nojekyll` 만은 남긴다 — 유니티가 만들어주지 않는데,
> 없으면 Pages의 Jekyll이 `_` 로 시작하는 파일을 무시해버린다.

```bash
# gh-pages 를 워크트리로 꺼낸다 (브랜치를 오가지 않아도 된다)
git fetch origin gh-pages:gh-pages
git worktree add ../wolfman-pages gh-pages
cd ../wolfman-pages

# 비운다 — .git 과 .nojekyll 만 남긴다
find . -mindepth 1 -maxdepth 1 ! -name '.git' ! -name '.nojekyll' -exec rm -rf {} +

# 새로 채운다
cp -r ../wolfman-build/Build/Web/. .

git add -A
git commit -m "deploy: <무엇이 새로 올라가는지> (#이슈번호)"
git push origin gh-pages
```

커밋 메시지에는 **이번 배포로 라이브에 새로 가는 것**을 적는다. `gh-pages` 히스토리가 "언제 무엇이 심사자에게 보이기 시작했는가"의 유일한 기록이다.

## 5. 확인한다

Pages 게시에 1분쯤 걸린다.

```bash
U=https://sonseongoh.github.io/wolfman

# index.html 이 참조하는 해시 4개
curl -s "$U/" | grep -oE '[a-f0-9]{32}\.[a-z.]+' | sort -u

# 새 파일이 200 인지
curl -s -o /dev/null -w '%{http_code}\n' "$U/Build/<새 wasm 파일명>"

# 옛 파일이 404 인지 — 200 이면 비우기가 안 된 것이다
curl -s -o /dev/null -w '%{http_code}\n' "$U/Build/<이전 wasm 파일명>"
```

마지막으로 브라우저에서 실제로 열어 **타이틀 → 마을 → 달 공개 → 사냥**까지 한 바퀴 밟아본다. 로더가 죽는 문제(압축 설정)는 이때만 드러난다.

## 알아둘 것

- **빌드 시 유니티가 `Assets/Settings/*.asset` 과 `ProjectSettings.asset` 을 스스로 수정한다.** WebGL 플랫폼 전환에 따른 정규화라 정상이다. 클린 클론에서 빌드하면 이 변경은 그 클론에만 남고 버려진다.
- **`Docs/` 폴더 배포 옵션은 쓸 수 없다.** 저장소에 이미 대문자 `Docs/` 가 있고 팀이 쓰는 Windows는 대소문자를 구분하지 않아 둘이 공존할 수 없다 (ADR 0003).
- **저장소가 public 이어야 한다.** 무료 플랜은 비공개 저장소에 Pages를 붙일 수 없다.
