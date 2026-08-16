# 🐺 wolfman

늑대인간이 주인공인 탑다운 2D 로그라이크. **뱀서라이크 전투 + 마을 운영** 장르로, 매 밤 확률로 떠오르는 **달이 그 밤의 룰셋**을 정한다.

굶으면 이성을 잃고 사냥하면 짐승에 가까워진다. 어느 쪽 끝에 닿아도 **내가 마을을 덮친다** — 이 게임에서 마을을 위협하는 것은 플레이어 자신뿐이다.

- 용어집 — [CONTEXT.md](CONTEXT.md)
- 게임 디자인 — [Docs/GameDesign.md](Docs/GameDesign.md)
- 결정 기록 — [Docs/adr/](Docs/adr/)

## 개발 환경

- Unity 6.5 (6000.5.6f1) · Universal 2D (URP) · 새 Input System
- 제출 빌드는 WebGL → GitHub Pages ([ADR 0003](Docs/adr/0003-제출-빌드는-웹으로-내고-깃허브-페이지에-올린다.md))
- 프로젝트 파일은 Windows 쪽에 있어야 한다 — 유니티가 WSL 경로를 열지 못한다

## 조작법

| 입력 | 동작 |
|---|---|
| WASD / 방향키 / 가상 조이스틱 | 이동 |
| 1 / 2 / 3 | 웨이브 클리어 후 3택 선택 (클릭도 가능) |
| ESC 또는 우상단 ⏸ | 일시정지 (어느 씬에서든) |
| E | 마을에서 시설 수리 |
| T | 마을에서 시설에 피해 (디버그) |

공격은 자동 — 사거리 안의 가장 가까운 적에게 발톱을 휘두른다.

## 씬 구성

```
TitleScene → MainScene(허브) → HuntScene (사냥) 또는 VillageScene (마을)
                    ▲                    │
                    └──── 정산 ◀─────────┘
```

## 현재 구현된 것

**달** — 9종 ScriptableObject, 가중치 추첨. 슬롯머신 연출 + 전설 등급 승급 연출. 달이 적 배율·드랍·조명 색을 정한다

**사냥** — 무한 웨이브. 적 4종(추적형·돌진형·원거리형·기본형), 웨이브마다 규모 증가. 근접 발톱 자동 공격(기본 데미지 10, 0.6초 간격), 넉백·히트스톱·카메라 흔들림·데미지 숫자

**성장** — 웨이브 클리어마다 3택 1 (등급 4단계 가중치 추첨, 16종). 현재는 그 사냥 안에서만 유지

**재화** — 골드 코인 드랍 → 임시 골드 → 살아서 귀환하면 금고로 확정. 사망 시 임시 골드 손실

**런 경계** (#121) — 타이틀에서 새로 시작하면 이전 런의 금고 골드와 라운드 번호가 지워진다. 죽음은 밤만 소모할 뿐 런을 끝내지 않는다

**탈출** — 5웨이브부터 10% 확률로 포탈이 열린다. 화면 밖이면 가장자리에 방향·거리 표시

**마을** — 인간 형태로 전환(`PlayerFormRule`이 페이즈로 결정), 시설 체력·파괴·수리

**그 외** — 전역 일시정지, 게임오버 화면, 효과음, 가상 조이스틱, 타이틀 화면

## 스크립트 구조

**`Assets/Scripts/Core/`** — `UnityEngine`에 의존하지 않는 순수 로직을 여기에 둔다. WSL에서 테스트하기 위해서다.

| 파일 | 역할 |
|---|---|
| `GameManager.cs` | 현재 달·라운드·페이즈 보관, 이벤트 발행 |
| `RoundPhase.cs` · `RoundFlowRule.cs` | 페이즈 정의와 라운드 한 바퀴의 판정 (순수) |
| `PlayerFormRule.cs` | 페이즈 → 인간/늑대인간 (순수) |
| `GoldWallet.cs` · `FacilityCore.cs` | 재화 뱅킹, 시설 체력·수리 (순수) |
| `CurrencyManager.cs` | 위 지갑의 씬 수명 관리 |
| `MoonData.cs` · `MoonTable.cs` · `MoonEffects.cs` | 달 데이터, 추첨, 조명 전환 |
| `PauseSystem.cs` · `SoundManager.cs` | 전역 일시정지, 효과음 |

**`Assets/Scripts/`** — 씬에 붙는 것들

| 묶음 | 파일 |
|---|---|
| 플레이어 | `PlayerMovement` `PlayerHealth` `PlayerTransform` `MeleeAttack` `PlayerAttack` `PlayerWalkAnim` |
| 적 | `EnemyChase` `EnemyHealth` `EnemyCharge` `EnemyRangedAttack` `EnemyProjectile` |
| 흐름 | `WaveManager` `RoundController` `VillageController` `TitleScreen` `EscapePortal` |
| 획득물 | `GoldCoin` `HealthPickup` `Projectile` |
| 연출 | `CameraFollow` `DamageNumber` `DeathPop` `HitStop` `SlashEffect` `WalkWobble` |
| UI | `ChoiceCardUI` `GoldPanelUI` `UIFont` `VirtualJoystick` |
| 마을 | `FacilityHealth` |

UI는 전부 `OnGUI` 임시 구현이다. Canvas 전환은 아직이다.

## 테스트

| 위치 | 대상 | 실행 |
|---|---|---|
| `Tests/Wolfman.Domain.Tests/` | 순수 C# 로직 (NUnit) | WSL에서 `dotnet test` |
| `Assets/Tests/PlayMode/` | 씬이 필요한 흐름 | 유니티 Test Runner |

## 로드맵

설계는 [Docs/GameDesign.md](Docs/GameDesign.md)에 확정돼 있고, 아래는 아직 코드가 없는 것들이다.

- [ ] **야성 · 굶주림 축** — 사냥하면 야성이 차고 마을에 있으면 굶주림이 찬다. 양 끝 모두 폭주로 이어진다
- [ ] **폭주** — 한계를 넘으면 조작은 남지만 막지 못한 채 마을을 부순다
- [ ] **낮과 밤** — 낮에 정해진 횟수만큼 행동(사기·고치기·묶이기), 해질녘에 달을 보고 그 밤을 정한다
- [ ] **영구 강화 상점** (#13) — 대장간에서 금고 골드로. 런을 넘어 남는 유일한 성장
- [ ] **망루탑** — 세 번째 시설. 야성을 늦추고 묶이는 장소
- [ ] **죽음의 비용** (#124)
- [ ] Canvas 기반 UI, 밸런싱

동료 시스템은 1차 MVP에서 제외.
