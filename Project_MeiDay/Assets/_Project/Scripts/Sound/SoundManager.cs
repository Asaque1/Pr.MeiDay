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

    void Awake()
    {
        instance = this; // 게임 켜질 때 instance 에 나를 넣어줌
    }

    // 효과음 한 번 틀기
    public void PlaySfx(AudioClip clip)
    {
        if (clip == null)
        {
            return; // 아직 소리 안 넣은 데서 불러도 안 터지게
        }

        sfxSource.PlayOneShot(clip);
    }

    // 배경음 바꾸기 (이미 같은 곡이면 그대로 둠)
    public void PlayBgm(AudioClip clip)
    {
        if (bgmSource.clip == clip)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.Play();
    }
}