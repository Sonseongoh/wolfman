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

    [Header("획득 / 성장")]
    public AudioClip sfxPickup;   // 보석 획득
    public AudioClip sfxLevelUp;  // 레벨업 / 스킬 선택

    [Header("연출")]
    public AudioClip sfxSlot;        // 달 슬롯머신 플립
    public AudioClip sfxMoonReveal;  // 달 최종 확정
    public AudioClip sfxGameOver;    // 게임오버

    AudioSource src;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        src = gameObject.AddComponent<AudioSource>();
    }

    void Play(AudioClip clip, float volume = 1f)
    {
        if (clip != null) src.PlayOneShot(clip, volume);
    }

    public void PlayShoot()       => Play(sfxShoot, 0.7f);
    public void PlaySlash()       => Play(sfxSlash, 0.9f);
    public void PlayMoonReveal()  => Play(sfxMoonReveal);
    public void PlayHit()      => Play(sfxHit, 0.8f);
    public void PlayDeath()    => Play(sfxDeath);
    public void PlayDamaged()  => Play(sfxDamaged, 0.9f);
    public void PlayPickup()   => Play(sfxPickup, 0.6f);
    public void PlayLevelUp()  => Play(sfxLevelUp);
    public void PlaySlot()     => Play(sfxSlot, 0.5f);
    public void PlayGameOver() => Play(sfxGameOver);
}
