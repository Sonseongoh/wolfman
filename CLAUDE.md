# wolfman — 작업 가이드

늑대인간이 주인공인 탑다운 2D 로그라이크 (뱀파이어 서바이버류).
밤/보름달 변신 시스템이 이 게임의 정체성이자 차별점이다.

## 개발 환경 — 먼저 읽을 것

이 프로젝트는 **WSL 과 Windows 에 걸쳐 있다.**

| | 위치 | 이유 |
|---|---|---|
| 프로젝트 파일 | `C:\Users\SSAFY\workspace\personal\wolfman`<br>(WSL 에서는 `/mnt/c/Users/SSAFY/workspace/personal/wolfman`) | 유니티가 Windows 네이티브로 돌아야 빠름 |
| Unity 에디터 | Windows | `\\wsl$` 로 열면 읽기가 11배 느리고 파일 변경 감지가 안 됨 |
| 코드 편집 / git | WSL | — |

`/home/ssafy/workspace/personal/wolfman` 은 이전 위치이며 **백업으로만 남아있다.**
거기서 작업하지 말 것 — 유니티가 보는 건 `C:\` 쪽이다.

### Unity 버전

`6000.5.6f1` (Unity 6.5) · Universal 2D (URP) · 새 Input System.
`ProjectSettings/ProjectVersion.txt` 와 반드시 일치해야 한다. 다른 버전으로 열면
씬/프리팹이 자동 업그레이드되면서 대량 diff 가 발생한다.

## 유니티 프로젝트에서 조심할 것

### 1. 코드만으로는 절반이다

동작의 상당 부분이 코드가 아니라 **에디터의 인스펙터 할당**에 들어있다:

- `PlayerAttack.projectilePrefab`
- `EnemySpawner.enemyPrefab`
- `EnemyHealth.gemPrefab`
- `CameraFollow.target`

전부 `public` 필드고 씬/프리팹 YAML 에 GUID 로 박혀 있다. 새 프리팹을 만들거나
참조를 연결하는 건 에디터 작업이므로, 스크립트만 추가하고 "완료"라고 하면 안 된다.
무엇을 인스펙터에서 연결해야 하는지 명시할 것.

### 2. `.meta` 파일은 반드시 같이 커밋한다

`.meta` 에 GUID 가 들어있어서, 빠뜨리면 다른 사람 프로젝트에서 참조가 전부 끊긴다.
파일을 추가/이동/삭제할 때 짝이 되는 `.meta` 도 같이 처리할 것.

### 3. 씬/프리팹 충돌은 일반 머지로 풀지 않는다

`.gitattributes` 에 `merge=unityyamlmerge` 가 설정돼 있다. 유니티가 제공하는
전용 3-way 머지 도구를 써야 한다 (설정은 아래 참고). 텍스트 머지로 억지로
풀면 씬이 조용히 깨진다.

여러 명이 같은 씬을 동시에 건드리지 않는 게 최선이다. 가능하면 프리팹 단위로 나눠 작업.

### 4. 컴파일 검증은 에디터에서만 가능하다

WSL 쪽엔 `UnityEngine.dll` 이 없다. `MonoBehaviour` 를 건드린 코드는 문법이
맞는지조차 이쪽에서 확인할 수 없으니, 수정 후 유니티에서 콘솔을 확인해야 한다.

예외: `UnityEngine` 에 의존하지 않는 **순수 C# 로직**은 `~/.dotnet/dotnet` 으로
컴파일·테스트가 가능하다. 달 추첨 확률, 웨이브 곡선, 레벨업 경험치 곡선처럼
수치가 중요한 로직은 이 레이어로 빼면 테스트할 수 있다.

## 브랜치 / 커밋

상세 규약은 `CONTRIBUTING.md`. 요약:

```
<type>: <제목> (#이슈번호)
```

`feat` `fix` `asset` `design` `refactor` `docs` `chore`

- **PR 은 `dev` 로 올린다** (`main` 아님)
- 작업 브랜치: `feat/3-달-시스템`, `fix/7-플레이어-이동` 형식
- 머지 전 팀원 1명 이상 리뷰
- 이슈 번호 연결 필수 (`closes #이슈번호`)

`dev` 가 `main` 보다 앞서 있다. 새 작업은 `dev` 기준으로 딸 것.

## 스크립트 구조 (`Assets/Scripts/`)

매니저 클래스 없이 `MonoBehaviour` 들이 `GameObject.FindWithTag("Player")` 와
`GetComponent` 로 느슨하게 연결된 프로토타입 구조다.

| 파일 | 역할 |
|---|---|
| `PlayerMovement.cs` | WASD 이동 (Rigidbody2D) |
| `PlayerAttack.cs` | 최근접 적 조준 자동 발사 |
| `PlayerHealth.cs` | 체력, 접촉 데미지, 게임오버/재시작 |
| `PlayerLevel.cs` | 경험치, 레벨업 강화 선택 |
| `CameraFollow.cs` | 카메라 추적 |
| `EnemySpawner.cs` | 적 스폰 + 난이도 곡선 |
| `EnemyChase.cs` | 적의 플레이어 추적 |
| `EnemyHealth.cs` | 적 체력, 사망 시 보석 드랍 |
| `Projectile.cs` | 투사체 이동/명중 판정 |
| `XPGem.cs` | 경험치 보석 (자석 흡수) |

UI 는 아직 `OnGUI` 임시 구현이고, 스프라이트는 도형(네모·원) 프로토타입이다.

## 알려진 임시 구현

고칠 때 참고 — 의도적으로 대충 짠 부분이라 놀랄 필요 없다:

- `PlayerAttack.FindNearestEnemy()` 가 매 발사마다 `FindObjectsByType` 로 전체 적을
  훑는다. 적이 많아지면 부담이 되므로 웨이브 규모가 커지면 손봐야 한다.
- `OnGUI` 기반 HP/XP/레벨업 UI — Canvas 기반으로 교체 예정.
- `Projectile` 이 `transform.Translate` 로 움직여서 물리와 따로 논다.

## 도구

- **Git LFS — 현재 쓰지 않는다.** `.gitattributes` 에는 `merge=unityyamlmerge`
  규칙만 있고 LFS 필터는 없다. 이미지·오디오는 저장소에 생바이너리로 들어간다.
  지금은 `Assets/Art/` PNG 29개 합계 약 271KB 라 문제될 규모가 아니다.
  다만 전투 사운드(#34)처럼 큰 바이너리가 들어오기 전에 도입 여부를 팀에서 정할 것 —
  에셋이 쌓인 뒤에 전환하면 `git-filter-repo` 로 히스토리를 다시 써야 한다.
- **.NET SDK 9** — `~/.dotnet/dotnet`. WSL 에 `libicu` 가 없으면
  `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1` 를 붙여야 실행된다.
- **git identity** — `~/.gitconfig` 의 `includeIf` 가 경로로 계정을 가른다.
  `useConfigOnly = true` 라서 규칙에 안 걸리는 경로에서는 커밋이 거부된다.
  프로젝트를 또 옮기면 `~/.gitconfig` 에 경로를 추가해야 한다.

## Agent skills

### Issue tracker

GitHub Issues (`Sonseongoh/wolfman`) — `gh` CLI 로 조작한다.
`gh` 활성 계정이 `biddan606` 이어야 이 저장소가 보인다
(`gh auth switch --user biddan606`). 자세한 규약은 `Docs/agents/issue-tracker.md`.

### Triage labels

canonical 5개 역할을 이름 그대로 쓴다 (`needs-triage`, `needs-info`,
`ready-for-agent`, `ready-for-human`, `wontfix`). 팀 라벨(`A-전투`, `B-마을`,
`C-흐름`, `feature`, `asset`)과는 별개 축이다. 매핑은 `Docs/agents/triage-labels.md`.

### Domain docs

single-context — 루트 `CONTEXT.md` + `Docs/adr/`. 읽기 규칙은 `Docs/agents/domain.md`.
