using System;
using UnityEngine;
using UnityEngine.UI;

// 인트로/아웃트로 컷신용 슬라이드쇼 (그림을 클릭으로 넘기면서 보여줌)
public class SlideshowView : MonoBehaviour
{
    [SerializeField] GameObject slidePanel; // 슬라이드 전체 (재생 중일 때만 켜짐. 이 스크립트 오브젝트 말고 자식을 넣을 것)
    [SerializeField] Image slideImage;      // 그림 나오는 자리

    Sprite[] _slides; // 지금 재생 중인 그림들
    int _index = 0;   // 몇 번째 그림인지
    Action _onDone;   // 다 보고 나면 부를 것

    // 슬라이드쇼 시작 (그림이 없으면 바로 끝낸 걸로 침)
    public void Play(Sprite[] slides, Action onDone)
    {
        _slides = slides;
        _onDone = onDone;
        _index = 0;

        if (slides == null || slides.Length == 0 || slidePanel == null)
        {
            Finish();
            return;
        }

        // 다른 패널에 가리면 클릭이 안 먹어서 영영 못 넘어감 - 맨 앞으로 올려둠
        slidePanel.transform.SetAsLastSibling();
        slidePanel.SetActive(true);

        // 부모가 꺼져 있으면 화면에 안 나오고 클릭도 못 함 -> 그냥 건너뜀
        if (slidePanel.activeInHierarchy == false)
        {
            Debug.LogWarning("컷신 패널의 부모가 꺼져 있어서 안 보임 - 건너뜀");
            Finish();
            return;
        }

        SetSlide(slides[0]);
    }

    // 그림 갈아끼우기 (안 꽂아뒀어도 안 터지게)
    void SetSlide(Sprite sprite)
    {
        if (slideImage != null)
        {
            slideImage.sprite = sprite;
        }
    }

    // 화면 클릭 (버튼 OnClick 에 연결) - 다음 그림으로, 끝이면 종료
    public void OnClickNext()
    {
        _index++;

        if (_slides == null || _index >= _slides.Length)
        {
            Finish();
            return;
        }

        SetSlide(_slides[_index]);
    }

    // 끝내고 알려주기
    void Finish()
    {
        if (slidePanel != null)
        {
            slidePanel.SetActive(false);
        }

        if (_onDone != null)
        {
            Action done = _onDone;
            _onDone = null;
            done();
        }
    }
}
