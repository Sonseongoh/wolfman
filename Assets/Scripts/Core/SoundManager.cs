using UnityEngine;

/// <summary>
/// 전투 효과음 재생 (#34).
/// AudioClip은 Inspector에서 연결. SampleScene의 GameManager 오브젝트에 부착.
/// </summary>
public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance { get; private set; }

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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
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
