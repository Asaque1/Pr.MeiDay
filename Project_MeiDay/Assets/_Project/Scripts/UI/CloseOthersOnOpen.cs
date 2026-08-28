using UnityEngine;

// 이 창이 열리면 지정한 다른 창들을 닫음
// 설명서랑 업적창이 동시에 떠서 겹치는 것 막는 용도
// (업적 켜진 상태에서 설명서를 누르면 업적이 닫히고 설명서가 뜸)
public class CloseOthersOnOpen : MonoBehaviour
{
    [SerializeField] GameObject[] others; // 같이 떠 있으면 안 되는 창들

    void OnEnable()
    {
        if (others == null)
        {
            return;
        }

        for (int i = 0; i < others.Length; i++)
        {
            if (others[i] != null && others[i].activeSelf == true)
            {
                others[i].SetActive(false);
            }
        }
    }
}
