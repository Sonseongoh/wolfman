/// <summary>
/// 한 라운드의 진행 단계. 모듈 A/B/C가 공유하는 기준.
///
/// UnityEngine 에 의존하지 않아 WSL 테스트에서 그대로 쓸 수 있다 —
/// 페이즈로 갈리는 규칙(예: 플레이어 형태 #53)을 씬 없이 검사하기 위해 코어로 옮겼다.
/// </summary>
public enum RoundPhase
{
    MoonReveal,   // 달 공개 (C)
    ActionSelect, // 마을에 남기 vs 사냥 나가기 선택 (C)
    Village,      // 마을 디펜스/운영 (B)
    Hunt,         // 사냥 뱀서 전투 (A)
    Reward,       // 정산/보상 (C)
}
