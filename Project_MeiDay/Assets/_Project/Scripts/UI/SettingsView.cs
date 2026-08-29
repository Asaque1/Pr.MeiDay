using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 설정창 - 소리 크기 조절이랑 게임 종료
// 창 열고 닫는 건 QuickButton 이 하고, 여기는 안쪽 내용만 맡음
// 소리 값은 SoundManager 가 PlayerPrefs 에 저장하니까 여기선 슬라이더만 물려줌
public class SettingsView : MonoBehaviour
{
    [Header("소리")]
    [SerializeField] Slider bgmSlider;   // 배경음 크기
    [SerializeField] Slider sfxSlider;   // 효과음 크기
    [SerializeField] TMP_Text bgmLabel;  // "80%" 표시
    [SerializeField] TMP_Text sfxLabel;

    [Header("게임 종료")]
    [SerializeField] GameObject quitConfirm; // "정말 끌까요?" 확인 칸
    [SerializeField] GameObject quitButton;  // 확인 중엔 숨김

    bool _loading = false; // 슬라이더에 값 넣는 중 (넣자마자 콜백이 또 도는 걸 막음)

    void OnEnable()
    {
        // 창을 열 때마다 지금 값으로 맞춰줌
        SoundManager sound = SoundManager.instance;

        _loading = true;
        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f;
            bgmSlider.maxValue = 1f;
            bgmSlider.value = (sound != null) ? sound.BgmVolume : 1f;
        }
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f;
            sfxSlider.maxValue = 1f;
            sfxSlider.value = (sound != null) ? sound.SfxVolume : 1f;
        }
        _loading = false;

        ShowLabels();

        // 확인 칸은 항상 접어둔 채로 열기
        SetOn(quitConfirm, false);
        SetOn(quitButton, true);
    }

    // 배경음 슬라이더 On Value Changed 에 연결
    public void OnBgmChanged(float value)
    {
        if (_loading == true || SoundManager.instance == null)
        {
            return;
        }

        SoundManager.instance.SetBgmVolume(value);
        ShowLabels();
    }

    // 효과음 슬라이더 On Value Changed 에 연결
    public void OnSfxChanged(float value)
    {
        if (_loading == true || SoundManager.instance == null)
        {
            return;
        }

        SoundManager.instance.SetSfxVolume(value);
        ShowLabels();

        // 얼마나 커졌는지 바로 들려줌 (숫자만 보고는 감이 안 옴)
        SoundManager.instance.PlaySfx(SoundManager.instance.uiClick);
    }

    // 종료 버튼 - 바로 끄지 않고 한 번 물어봄
    public void OnClickQuit()
    {
        SetOn(quitConfirm, true);
        SetOn(quitButton, false);
    }

    // 확인 칸의 "아니요"
    public void OnClickQuitCancel()
    {
        SetOn(quitConfirm, false);
        SetOn(quitButton, true);
    }

    // 확인 칸의 "예" - 진짜 종료
    public void OnClickQuitConfirm()
    {
        PlayerPrefs.Save(); // 맞춰둔 설정은 확실히 남기고 끄기

#if UNITY_EDITOR
        // 에디터에선 Application.Quit 이 안 먹으니 플레이만 멈춤
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // 슬라이더 옆 퍼센트 글씨 갱신
    void ShowLabels()
    {
        if (bgmLabel != null && bgmSlider != null)
        {
            bgmLabel.text = Mathf.RoundToInt(bgmSlider.value * 100f) + "%";
        }
        if (sfxLabel != null && sfxSlider != null)
        {
            sfxLabel.text = Mathf.RoundToInt(sfxSlider.value * 100f) + "%";
        }
    }

    void SetOn(GameObject go, bool on)
    {
        if (go != null && go.activeSelf != on)
        {
            go.SetActive(on);
        }
    }
}
