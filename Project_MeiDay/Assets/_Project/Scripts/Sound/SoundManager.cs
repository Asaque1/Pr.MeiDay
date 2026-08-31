using UnityEngine;

// 효과음이랑 배경음을 틀어주는 매니저
// 어디서든 SoundManager.instance 로 불러서 씀
public class SoundManager : MonoBehaviour
{
    public static SoundManager instance; // 어디서든 쓰는 나 자신

    [SerializeField] AudioSource sfxSource; // 효과음 내보내는 스피커
    [SerializeField] AudioSource bgmSource; // 배경음 내보내는 스피커

    [Header("효과음 - 쓰는 곳 정해진 것")]
    public AudioClip cardPick;    // 카드 집을 때 (끌기 시작)
    public AudioClip cardPlace;   // 카드를 빈칸에 놓을 때
    public AudioClip uiClick;     // 업적창 여는 것 같은 UI 버튼
    public AudioClip lockedClick; // 지금 못 쓰는 걸 눌렀을 때
    public AudioClip achieveGet;  // 업적 달성
    public AudioClip pageTurn;    // 페이지 넘어갈 때 (하루 결산 노트)
    public AudioClip docAccept;   // 민원을 제대로 처리했을 때 (서류에 확인 도장)
    public AudioClip wrongAnswer; // 해석이나 처리 틀렸을 때 (서류 찢는 소리)

    [Header("효과음 - 아직 소리 없음")]
    // 비워두면 그냥 조용히 넘어감
    public AudioClip buyItem;     // 아이템 샀을 때
    public AudioClip typeTick;    // 대사 타이핑 한 글자마다

    [Header("배경음 - 메인")]
    // 메인 곡 여러 개를 번갈아 돌림 (한 곡이 끝나면 다음 곡, 끝까지 가면 처음으로)
    // 곡이 하나뿐이면 그냥 그 곡을 반복함
    public AudioClip[] mainBgms;

    [Header("배경음 - 알바")]
    public AudioClip bookSortBgm;  // 서점 - 책 정리
    public AudioClip spotlightBgm; // 극단 - 조명 연출
    public AudioClip promoBgm;     // 파티용품점 - 판촉

    [Header("배경음 - 그 밖")]
    // 안 넣어둔 건 메인 곡으로 대신 깔림 (게임 오버만 예외 - 조용한 게 나음)
    public AudioClip titleBgm;    // 타이틀
    public AudioClip dayBgm;      // 낮 (메인 곡을 안 넣었을 때만 씀)
    public AudioClip nightBgm;    // 밤 - 상점
    public AudioClip endingBgm;   // 엔딩
    public AudioClip gameOverBgm; // 게임 오버

    bool _mainMode = false;    // 지금 메인 곡을 돌리는 중인지
    int _mainIndex = -1;       // 메인 곡 중 몇 번째를 트는 중인지
    float _mainHeard = 0f;     // 지금 곡이 어디까지 실제로 흘러갔는지

    // 배경음 파일은 흘려보내기(Streaming) + 백그라운드 로딩이라
    // 아직 다 안 불러온 상태에서 Play() 를 부르면 소리 없이 그냥 지나감
    // 그래서 못 튼 곡을 여기 적어뒀다가 다 불러오면 그때 틀어줌
    AudioClip _bgmPending = null;

    // 설정창에서 만지는 소리 크기 (0 ~ 1)
    // 스피커에 원래 맞춰둔 크기(bgm 0.5 처럼)를 기억해뒀다가 거기에 곱함
    // 그래야 설정을 최대로 올려도 원래 잡아둔 균형이 안 깨짐
    const string BgmKey = "bgmVolume";
    const string SfxKey = "sfxVolume";
    float _bgmBase = 1f;
    float _sfxBase = 1f;
    float _bgmVolume = 1f;
    float _sfxVolume = 1f;

    public float BgmVolume { get { return _bgmVolume; } }
    public float SfxVolume { get { return _sfxVolume; } }

    void Awake()
    {
        instance = this; // 게임 켜질 때 instance 에 나를 넣어줌

        if (bgmSource != null)
        {
            _bgmBase = bgmSource.volume;
        }
        if (sfxSource != null)
        {
            _sfxBase = sfxSource.volume;
        }

        // 지난번에 맞춰둔 크기 불러오기 (처음 켠 사람은 둘 다 최대)
        _bgmVolume = PlayerPrefs.GetFloat(BgmKey, 1f);
        _sfxVolume = PlayerPrefs.GetFloat(SfxKey, 1f);
        ApplyVolume();
    }

    // 배경음 크기 바꾸기 (설정창 슬라이더가 부름)
    public void SetBgmVolume(float value)
    {
        _bgmVolume = Mathf.Clamp01(value);
        ApplyVolume();
        PlayerPrefs.SetFloat(BgmKey, _bgmVolume);
    }

    // 효과음 크기 바꾸기 (설정창 슬라이더가 부름)
    public void SetSfxVolume(float value)
    {
        _sfxVolume = Mathf.Clamp01(value);
        ApplyVolume();
        PlayerPrefs.SetFloat(SfxKey, _sfxVolume);
    }

    // 지금 값들을 실제 스피커에 먹이기
    void ApplyVolume()
    {
        if (bgmSource != null)
        {
            bgmSource.volume = _bgmBase * _bgmVolume;
        }
        if (sfxSource != null)
        {
            sfxSource.volume = _sfxBase * _sfxVolume;
        }
    }

    void Start()
    {
        // 단계 바뀔 때마다 배경음 갈아끼우게 구독해둠
        if (GameManager.instance != null)
        {
            GameManager.instance.OnPhaseChanged += OnPhaseChanged;
            PlayBgmFor(GameManager.instance.phase); // 켜질 때 지금 단계로 한 번 맞춰줌
            return;
        }

        // 로비랑 프롤로그엔 GameManager 가 없음
        // 여기서 안 틀어주면 시작부터 프롤로그 끝까지 50초 넘게 무음이었음
        PlayMain();
    }

    void OnDestroy()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.OnPhaseChanged -= OnPhaseChanged;
        }
    }

    void Update()
    {
        PlayPendingBgm();

        // 메인 곡 한 곡이 끝났으면 다음 곡으로 넘김
        if (_mainMode == false || bgmSource == null)
        {
            return;
        }

        if (bgmSource.isPlaying == true)
        {
            _mainHeard = bgmSource.time; // 어디까지 흘렀는지 기억해둠
            return;
        }

        // 배경음은 흘려보내기(Streaming)라 Play() 를 불러도 바로 소리가 안 남
        // "안 나는 중" 만 보고 넘기면 로딩하는 동안 곡이 다다다다 넘어가버림
        // 실제로 흘러간 적이 있어야 진짜 끝난 걸로 침 (로딩 중엔 위치가 0 에 머물러 있음)
        if (_mainHeard <= 0.1f)
        {
            return;
        }

        if (UsableMainCount() == 0)
        {
            return;
        }

        NextMain();
    }

    // 단계가 바뀌었을 때
    void OnPhaseChanged(GamePhase phase)
    {
        // 결산은 노트를 펴는 화면이라 페이지 넘기는 소리를 같이 냄
        if (phase == GamePhase.DayResult)
        {
            PlaySfx(pageTurn);
        }

        PlayBgmFor(phase);
    }

    // 단계에 맞는 배경음으로 바꾸기
    void PlayBgmFor(GamePhase phase)
    {
        // 게임 오버는 곡이 없으면 그냥 조용히 (메인 곡이 깔리면 어색함)
        if (phase == GamePhase.GameOver)
        {
            PlayBgm(gameOverBgm);
            return;
        }

        if (phase == GamePhase.Intro && titleBgm != null)
        {
            PlayBgm(titleBgm);
            return;
        }
        if (phase == GamePhase.NightShop && nightBgm != null)
        {
            PlayBgm(nightBgm);
            return;
        }
        if (phase == GamePhase.Ending && endingBgm != null)
        {
            PlayBgm(endingBgm);
            return;
        }

        // 나머지(낮, 결산, 곡 아직 없는 단계)는 메인 곡
        PlayMain();
    }

    // 알바 판이 열렸을 때 그 알바 곡으로 (ArbeitManager 가 불러줌)
    public void PlayArbeitBgm(MinigameKind kind)
    {
        AudioClip clip = null;
        if (kind == MinigameKind.BookSort)
        {
            clip = bookSortBgm;
        }
        else if (kind == MinigameKind.Spotlight)
        {
            clip = spotlightBgm;
        }
        else if (kind == MinigameKind.Promo)
        {
            clip = promoBgm;
        }

        if (clip == null)
        {
            return; // 그 알바 곡을 안 넣었으면 밤 음악 그대로 둠
        }

        PlayBgm(clip);
    }

    // 지금 단계에 맞는 배경음으로 되돌리기 (알바 끝났을 때 등)
    public void RefreshBgm()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        PlayBgmFor(GameManager.instance.phase);
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

        _mainMode = false; // 메인 곡 돌리기는 여기서 끝

        if (bgmSource.clip == clip && bgmSource.isPlaying == true)
        {
            return;
        }

        // 아직 곡 안 넣은 단계면 소리 끄고 끝
        if (clip == null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
            _bgmPending = null;
            return;
        }

        bgmSource.loop = true;
        StartBgmClip(clip);
    }

    // 곡을 실제로 트는 자리
    // 다 안 불러온 곡을 바로 Play() 하면 조용히 씹혀서, 그럴 땐 불러오라고 시켜두고 기다림
    void StartBgmClip(AudioClip clip)
    {
        bgmSource.clip = clip;
        _mainHeard = 0f;

        if (clip.loadState == AudioDataLoadState.Loaded)
        {
            _bgmPending = null;
            bgmSource.Play();
            return;
        }

        clip.LoadAudioData();
        _bgmPending = clip;
    }

    // 기다리던 곡이 다 불러와졌으면 그때 틀기 (Update 가 매 프레임 물어봄)
    void PlayPendingBgm()
    {
        if (_bgmPending == null || bgmSource == null)
        {
            return;
        }

        if (_bgmPending.loadState == AudioDataLoadState.Loading)
        {
            return; // 아직 불러오는 중
        }

        // 불러오기에 실패했으면 붙잡고 있어봐야 소용없음
        if (_bgmPending.loadState == AudioDataLoadState.Loaded && bgmSource.clip == _bgmPending)
        {
            bgmSource.Play();
        }

        _bgmPending = null;
    }

    // ---- 메인 곡 돌려 틀기 ----

    // 메인 곡 틀기 (이미 돌고 있으면 끊지 않고 이어서 들려줌)
    void PlayMain()
    {
        if (UsableMainCount() == 0)
        {
            PlayBgm(dayBgm); // 메인 곡을 아직 안 넣었으면 예전 낮 곡으로
            return;
        }

        if (_mainMode == true && bgmSource != null && bgmSource.isPlaying == true)
        {
            return; // 낮에서 결산으로 넘어가는 것처럼, 곡이 끊기면 오히려 어색함
        }

        _mainMode = true;
        _mainIndex = -1;
        NextMain();
    }

    // 다음 메인 곡으로
    void NextMain()
    {
        if (bgmSource == null || mainBgms == null || mainBgms.Length == 0)
        {
            return;
        }

        // 비워둔 칸은 건너뛰고 다음 곡을 찾음 (한 바퀴 다 돌면 포기)
        AudioClip next = null;
        for (int i = 0; i < mainBgms.Length; i++)
        {
            int at = (_mainIndex + 1 + i) % mainBgms.Length;
            if (mainBgms[at] != null)
            {
                _mainIndex = at;
                next = mainBgms[at];
                break;
            }
        }

        if (next == null)
        {
            return;
        }

        // 곡이 하나뿐이면 넘길 데가 없으니 그 곡을 반복시킴
        bgmSource.loop = (UsableMainCount() <= 1);
        StartBgmClip(next);
    }

    // 실제로 틀 수 있는 메인 곡이 몇 개인지
    int UsableMainCount()
    {
        if (mainBgms == null)
        {
            return 0;
        }

        int count = 0;
        for (int i = 0; i < mainBgms.Length; i++)
        {
            if (mainBgms[i] != null)
            {
                count++;
            }
        }
        return count;
    }
}
