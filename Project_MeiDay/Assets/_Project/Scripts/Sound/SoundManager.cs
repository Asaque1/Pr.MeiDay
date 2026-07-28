using UnityEngine;

// 효과음이랑 배경음을 틀어주는 매니저
// 어디서든 SoundManager.instance 로 불러서 씀
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance; // 어디서든 쓰는 나 자신

    [SerializeField] AudioSource sfxSource; // 효과음 내보내는 스피커
    [SerializeField] AudioSource bgmSource; // 배경음 내보내는 스피커 (인스펙터에서 Loop 켜둘 것)

    // 자주 쓰는 효과음들 (인스펙터에서 연결)
    public AudioClip cardPlace;   // 카드를 상자에 놓을 때
    public AudioClip wrongAnswer; // 해석이나 처리 틀렸을 때
    public AudioClip buyItem;     // 아이템 샀을 때
    public AudioClip typeTick;    // 대사 타이핑 한 글자마다

    // 단계마다 깔리는 배경음 (인스펙터에서 연결. 안 넣으면 그 단계는 그냥 조용함)
    public AudioClip titleBgm;    // 타이틀
    public AudioClip dayBgm;      // 낮 - 민원 처리 (결산까지 이어짐)
    public AudioClip nightBgm;    // 밤 - 상점이랑 알바
    public AudioClip endingBgm;   // 엔딩
    public AudioClip gameOverBgm; // 게임 오버

    void Awake()
    {
        instance = this; // 게임 켜질 때 instance 에 나를 넣어줌

        // 배경음은 계속 돌아야 하니까 여기서 켜둠 (인스펙터에서 깜빡해도 되게)
        if (bgmSource != null)
        {
            bgmSource.loop = true;
        }
    }

    void Start()
    {
        // 단계 바뀔 때마다 배경음 갈아끼우게 구독해둠
        if (GameManager.instance != null)
        {
            GameManager.instance.OnPhaseChanged += PlayBgmFor;
            PlayBgmFor(GameManager.instance.phase); // 켜질 때 지금 단계로 한 번 맞춰줌
        }
    }

    void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnPhaseChanged -= PlayBgmFor;
        }
    }

    // 단계에 맞는 배경음으로 바꾸기
    void PlayBgmFor(GamePhase phase)
    {
        if (phase == GamePhase.Intro)
        {
            PlayBgm(titleBgm);
        }
        else if (phase == GamePhase.DayWork)
        {
            PlayBgm(dayBgm);
        }
        else if (phase == GamePhase.NightShop)
        {
            PlayBgm(nightBgm);
        }
        else if (phase == GamePhase.Ending)
        {
            PlayBgm(endingBgm);
        }
        else if (phase == GamePhase.GameOver)
        {
            PlayBgm(gameOverBgm);
        }
        // 결산은 낮 음악 그대로 두는 게 자연스러워서 안 건드림
    }

    // 효과음 한 번 틀기
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return; // 아직 소리 안 넣은 데서 불러도 안 터지게
        }

        sfxSource.PlayOneShot(clip);
    }

    // 배경음 바꾸기 (이미 같은 곡이면 그대로 둠)
    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource == null)
        {
            return; // 스피커 연결 안 한 씬에서도 안 터지게
        }
        if (bgmSource.clip == clip)
        {
            return;
        }

        // 아직 곡 안 넣은 단계면 소리 끄고 끝
        if (clip == null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();
    }
}