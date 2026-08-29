using UnityEngine;

// PDA 를 다시 열 때 안쪽 화면을 처음 상태로 되돌림
//
// PDA 안 손 모양 버튼은 PDA 오브젝트만 꺼서, 서류 발급 화면을 켜둔 채로 닫으면
// 그 상태가 남아 다시 열었을 때 민원 내용이랑 처리 버튼이 안 보였음
public class PdaResetOnOpen : MonoBehaviour
{
    void OnEnable()
    {
        TranslationView view = FindFirstObjectByType<TranslationView>(FindObjectsInactive.Include);
        if (view != null)
        {
            view.OnPdaOpened();
        }
    }
}
