using UnityEngine;

/// <summary>
/// 전투 효과음 재생 (#34). AudioClip은 Inspector에서 연결한다.
///
/// **TitleScene 의 GameManager 오브젝트에 하나만 둔다.** 씬을 넘어 살아남아야 하는데,
/// 살아남는 오브젝트는 맨 처음 로드되는 TitleScene 것 하나뿐이기 때문이다.
/// 사냥 씬에도 사본을 두었던 동안은 GameManager 중복 판정에 끌려 매번 파괴돼서
/// 실제 플레이에서는 소리가 한 번도 나지 않았다.
/// </summary>
public class SoundManager : MonoBehaviour
{
    static SoundManager instance;

    /// <summary>
    /// 부르는 쪽은 전부 `SoundManager.Instance?.PlayX()` 다. 그런데 C# 의 `?.` 는 유니티가
    /// 오버로딩한 `==` 를 타지 않아서, **파괴된** 사운드를 살아있는 참조로 착각하고 그대로 통과시킨다.
    /// 그러면 안쪽에서 MissingReferenceException 이 터지고, 그게 코루틴 안이면 코루틴이 통째로 죽는다 —
    /// 달 슬롯이 첫 칸에서 멈추고 사냥이 시작되지 않던 원인이 이것이었다.
    ///
    /// 여기서 유니티의 파괴 판정을 한 번 태워 두면 그 호출 전부가 조용히 넘어간다.
    /// 사운드가 없어서 소리가 안 나는 것과, 사운드가 없어서 게임이 멈추는 것은 다른 일이다.
    /// </summary>
    public static SoundManager Instance => instance != null ? instance : null;

    [Header("전투")]
    public AudioClip sfxShoot;    // 발사
    public AudioClip sfxSlash;    // 근접 할퀴기
    public AudioClip sfxHit;      // 적 타격
    public AudioClip sfxDeath;    // 적 처치
    public AudioClip sfxDamaged;  // 플레이어 피격

    public AudioClip sfxButton;   // UI 버튼 선택

    [Header("획득 / 성장")]
    public AudioClip sfxPickup;   // 보석 획득
    public AudioClip sfxLevelUp;  // 레벨업 / 스킬 선택

    [Header("연출")]
    public AudioClip sfxWaveClear;             // 웨이브 클리어
    public AudioClip sfxSlot;                  // 달 슬롯머신 플립
    public AudioClip sfxMoonReveal;            // 달 최종 확정
    public AudioClip sfxMoonRevealLegendary;   // 전설 달 최종 확정
    public AudioClip sfxGameOver;              // 게임오버

    [Header("BGM")]
    public AudioClip bgmBattle;    // 전투 배경음
    public AudioClip bgmVillage;   // 마을 배경음
    [Range(0f, 1f)] public float bgmVolume = 0.4f;

    AudioSource src;
    AudioSource bgmSrc;

    void Awake()
    {
        // 앞선 사운드가 파괴됐다면 != 가 false 라 이 인스턴스가 자리를 물려받는다
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        src = gameObject.AddComponent<AudioSource>();
        bgmSrc = gameObject.AddComponent<AudioSource>();
        bgmSrc.loop = true;
        bgmSrc.volume = bgmVolume;
    }

    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSrc.clip == clip) return;
        bgmSrc.clip = clip;
        bgmSrc.Play();
    }

    public void StopBGM() => bgmSrc.Stop();

    void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null) src.PlayOneShot(clip, volume);
    }

    public void PlayButton()      => Play(sfxButton);
    public void PlayWaveClear()   => Play(sfxWaveClear);
    public void PlayShoot()       => Play(sfxShoot, 0.7f);
    public void PlaySlash()       => Play(sfxSlash, 0.9f);
    public void PlayMoonReveal(bool legendary = false)
        => Play(legendary && sfxMoonRevealLegendary != null ? sfxMoonRevealLegendary : sfxMoonReveal);
    public void PlayHit()      => Play(sfxHit, 0.8f);
    public void PlayDeath()    => Play(sfxDeath);
    public void PlayDamaged()  => Play(sfxDamaged, 0.9f);
    public void PlayPickup()   => Play(sfxPickup, 0.6f);
    public void PlayLevelUp()  => Play(sfxLevelUp);
    public void PlaySlot()     => Play(sfxSlot, 0.5f);
    public void PlayGameOver() => Play(sfxGameOver);
}
