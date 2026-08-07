using UnityEngine;

public enum MoonRarity { Common, Uncommon, Rare, Epic, Legendary }

/// <summary>
/// 달 1개 = 에셋 1개. Project 우클릭 → Create → Wolfman → Moon Data로 생성.
/// 수치 조절은 코드 수정 없이 Inspector에서 한다.
/// </summary>
[CreateAssetMenu(fileName = "MoonData", menuName = "Wolfman/Moon Data")]
public class MoonData : ScriptableObject
{
    [Header("기본 정보")]
    public string moonName;
    public MoonRarity rarity;

    [Tooltip("달 아이콘 — 에셋 확보 후 연결하면 슬롯 연출·HUD에 자동 표시")]
    public Sprite icon;

    [Tooltip("등장 확률(%). 테이블 전체 합 기준 가중치")]
    [Range(0f, 100f)] public float appearChance = 10f;

    [Header("적 배율 (모듈 A/B가 스폰·스탯에 적용)")]
    public float enemyHpMultiplier = 1f;
    public float enemyDamageMultiplier = 1f;
    public float enemySpeedMultiplier = 1f;
    public float enemyCountMultiplier = 1f;

    [Header("플레이어 효과")]
    [Tooltip("false면 이 달에는 변신 불가 (예: 검은달)")]
    public bool transformAllowed = true;
    public float playerPowerMultiplier = 1f;

    [Tooltip("true면 웨이브 스킬 3택에 희귀 이상 스킬 1장 확정 등장 (블루문 효과)")]
    public bool guaranteeRareSkill = false;

    [Header("보상 (모듈 C가 정산에 사용)")]
    [Tooltip("1=일반, 2=고급, 3=희귀, 4=영웅, 5=전설")]
    [Range(1, 5)] public int rewardTier = 1;

    [Tooltip("경험치 보석 드랍 배수 (하베스트문 2)")]
    public float dropMultiplier = 1f;

    [Header("분위기 (화면 조명)")]
    [Tooltip("이 달이 뜬 밤의 전체 조명 색")]
    public Color ambientColor = Color.white;

    [Tooltip("조명 밝기 (1=평범, 낮을수록 어두운 밤)")]
    [Range(0.1f, 1.5f)] public float ambientIntensity = 1f;

    [TextArea] public string description;
}
