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

        if (slides == null || slides.Length == 0)
        {
            Finish();
            return;
        }

        slidePanel.SetActive(true);
        slideImage.sprite = slides[0];
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

        slideImage.sprite = _slides[_index];
    }

    // 끝내고 알려주기
    void Finish()
    {
        slidePanel.SetActive(false);

        if (_onDone != null)
        {
            Action done = _onDone;
            _onDone = null;
            done();
        }
    }
}
