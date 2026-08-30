using UnityEngine;

// 민원인이 화낼 때 화면을 잠깐 흔듦 (기획서의 "테이블 쿵 치며 화면 흔들림")
// ComplaintManager 가 화났다고 알려주면 여기서는 연출만 함
public class ScreenShake : MonoBehaviour
{
    [SerializeField] RectTransform target; // 흔들 대상 (안 꽂으면 자기 자신)
    [SerializeField] float duration = 0.35f; // 흔드는 시간
    [SerializeField] float power = 10f;      // 처음에 흔들리는 폭

    Vector2 _home;      // 원래 자리 (흔들고 나서 여기로 돌아옴)
    bool _homeSaved = false;
    float _left = 0f;   // 남은 흔들 시간

    void Start()
    {
        if (target == null)
        {
            target = transform as RectTransform;
        }

        SaveHome();

        if (GameManager.instance != null && GameManager.instance.complaint != null)
        {
            GameManager.instance.complaint.OnComplainantAngry += Shake;
        }
    }

    void OnDestroy()
    {
        if (GameManager.instance != null && GameManager.instance.complaint != null)
        {
            GameManager.instance.complaint.OnComplainantAngry -= Shake;
        }
    }

    // 흔들기 시작 (이미 흔들리는 중이면 처음부터 다시)
    public void Shake()
    {
        SaveHome();
        _left = duration;
    }

    void Update()
    {
        if (_left <= 0f || target == null)
        {
            return;
        }

        _left -= Time.deltaTime;

        if (_left <= 0f)
        {
            target.anchoredPosition = _home; // 끝났으면 제자리로
            return;
        }

        // 뒤로 갈수록 약해짐 - 끝에서 뚝 끊기면 어색함
        float now = power * (_left / duration);
        target.anchoredPosition = _home + new Vector2(
            Random.Range(-now, now),
            Random.Range(-now, now));
    }

    // 원래 자리 기억해두기 (흔드는 중에 다시 부르면 흔들린 자리를 원점으로 잡아버림)
    void SaveHome()
    {
        if (_homeSaved == true || target == null)
        {
            return;
        }

        _home = target.anchoredPosition;
        _homeSaved = true;
    }
}
