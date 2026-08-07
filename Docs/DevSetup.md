# 개발 환경 세팅 (팀원 공통)

## 1. 프로젝트 받기

- Unity 버전: **6000.5.6f1** (Unity 6.5) — 버전을 정확히 맞출 것 (다르면 Library 재임포트·설정 차이 발생)
- `git clone https://github.com/Sonseongoh/wolfman.git` 후 Unity Hub → Add → 폴더 선택

## 2. Unity Smart Merge 설정 (필수, 1회)

씬(.unity)/프리팹(.prefab)/에셋(.asset)은 YAML이라 일반 git 머지가 잘 깨진다.
Unity가 제공하는 UnityYAMLMerge를 머지 드라이버로 등록하면 대부분 자동 병합된다.

프로젝트 폴더에서 아래 두 줄 실행 (**경로는 본인 Unity 설치 위치로 수정**):

```bash
git config merge.unityyamlmerge.name "Unity SmartMerge"
git config merge.unityyamlmerge.driver "'C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/Tools/UnityYAMLMerge.exe' merge -p %O %B %A %A"
```

- 설치 경로 예시
  - 기본 설치: `C:/Program Files/Unity/Hub/Editor/6000.5.6f1/Editor/Data/Tools/UnityYAMLMerge.exe`
  - D드라이브 설치: `D:/Unity/Editors/6000.5.6f1/Editor/Data/Tools/UnityYAMLMerge.exe`
- `.gitattributes`는 저장소에 이미 있으므로 위 설정만 하면 됨

## 3. 협업 규칙 요약

- 이슈 → `feat/이슈번호-이름` 브랜치 → `dev`로 PR → 리뷰 1인 (상세: CONTRIBUTING.md)
- **한 씬/프리팹은 한 사람만 수정** — 남의 씬에 넣을 것은 프리팹으로 만들어 전달
- 씬 분리: 사냥 필드(모듈 A) / 마을(모듈 B) / 메인 루프·공통 UI(모듈 C)

## 4. 공용 코어 (Assets/Scripts/Core/)

모듈 간 약속. 바꿀 일이 있으면 팀 합의 후 수정.

| 파일 | 역할 |
|---|---|
| `GameManager.cs` | 싱글톤. 라운드 번호·현재 달·페이즈 보관. `OnMoonRevealed`, `OnPhaseChanged` 이벤트 제공 |
| `RoundPhase.cs` | 라운드 진행 단계 열거형. UnityEngine 비의존이라 WSL 테스트에서 그대로 쓴다 |
| `PlayerFormRule.cs` | 페이즈 → 플레이어 형태(인간/늑대인간) 규칙. 순수 함수, 씬마다 맞추지 않는다 (#53) |
| `MoonData.cs` | 달 1개 = ScriptableObject 에셋 1개 (확률, 적 배율, 변신 가능 여부, 보상 등급) |
| `MoonTable.cs` | 달 목록 + 가중치 확률 추첨 `Draw()` |

### 사용 예

```csharp
// 현재 달 읽기
MoonData moon = GameManager.Instance.CurrentMoon;

// 달 공개에 반응 (적 스포너가 배율 적용 등)
GameManager.Instance.OnMoonRevealed += moon => ApplyMoonRules(moon);

// 페이즈 전환에 반응 (씬 전환, UI 표시 등)
GameManager.Instance.OnPhaseChanged += phase => { if (phase == RoundPhase.Hunt) StartHunt(); };
```

### 달 데이터 만들기 (에디터)

Project 우클릭 → **Create → Wolfman → Moon Data** (달 6종은 이슈 #7에서 작성)
전부 만든 뒤 **Create → Wolfman → Moon Table** 에셋에 6종을 등록하고 GameManager에 꽂는다.
