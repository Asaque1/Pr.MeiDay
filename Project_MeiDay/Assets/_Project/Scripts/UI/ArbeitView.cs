using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 밤 알바 화면 - 알바 고르고, 미니게임 돌리고, 끝나면 결과를 보여줌
// 미니게임 화면 세 개를 인스펙터에서 연결해두면 종목에 맞는 걸 켜줌
public class ArbeitView : MonoBehaviour
{
    [SerializeField] GameObject listPart;     // 알바 목록 묶음
    [SerializeField] GameObject resultPart;   // 결과 묶음
    [SerializeField] Transform itemParent;    // 알바 버튼 놓이는 자리
    [SerializeField] ArbeitItemUI itemPrefab; // 알바 버튼 프리팹
    [SerializeField] TMP_Text leftText;       // 남은 횟수
    [SerializeField] TMP_Text emptyText;      // 할 수 있는 알바가 없을 때 문구
    [SerializeField] TMP_Text resultText;     // 결과 문구

    // 미니게임 화면들 (안 넣어두면 그 종목은 못 함)
    [SerializeField] BookSortView bookSortView;
    [SerializeField] SpotlightView spotlightView;
    [SerializeField] PromoView promoView;

    List<ArbeitItemUI> _itemUis = new List<ArbeitItemUI>();

    void OnEnable()
    {
        if (GameManager.instance == null)
        {
            return; // 아직 게임 준비 전이면 패스
        }

        ShowList();
    }

    void OnDisable()
    {
        // 미니게임 하다가 탭을 옮기거나 밤이 끝나면 횟수를 돌려줌 (안 그러면 하루치가 날아감)
        if (GameManager.instance != null)
        {
            GameManager.instance.arbeit.AbortArbeit();
        }
    }

    // 목록 화면으로 돌아오기 (미니게임이랑 결과는 닫음)
    public void ShowList()
    {
        // 하다 만 알바가 있으면 물려주고 목록으로
        if (GameManager.instance != null)
        {
            GameManager.instance.arbeit.AbortArbeit();
        }

        HideAllGames();

        if (resultPart != null)
        {
            resultPart.SetActive(false);
        }
        if (listPart != null)
        {
            listPart.SetActive(true);
        }

        Rebuild();
    }

    // 알바 목록 지우고 다시 만들기
    void Rebuild()
    {
        for (int i = 0; i < _itemUis.Count; i++)
        {
            Destroy(_itemUis[i].gameObject);
        }
        _itemUis.Clear();

        ArbeitManager am = GameManager.instance.arbeit;
        if (leftText != null)
        {
            leftText.text = "남은 알바 " + am.arbeitLeftToday + "회";
        }

        // 횟수를 다 썼으면 목록 대신 안내만
        bool canWork = am.CanWork();
        if (emptyText != null)
        {
            emptyText.gameObject.SetActive(canWork == false);
            emptyText.text = "오늘은 더 못 함 (내일 다시)";
        }
        if (canWork == false)
        {
            return;
        }

        // 프리팹이나 놓을 자리를 안 꽂아뒀으면 목록을 못 만듦
        if (itemPrefab == null || itemParent == null)
        {
            Debug.LogWarning("알바 화면에 목록 프리팹이나 놓을 자리가 연결 안 됨");
            return;
        }

        List<MinigameDefinition> all = GameManager.instance.content.minigames;
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == null)
            {
                continue; // 데이터에 빈 칸이 섞여있으면 건너뜀
            }

            // 먼저 목록에 넣어둬야 Set 에서 뭐가 잘못돼도 고아 오브젝트가 안 남음
            ArbeitItemUI itemUi = Instantiate(itemPrefab, itemParent);
            _itemUis.Add(itemUi);
            itemUi.Set(this, all[i]);
        }
    }

    // 알바 버튼이 눌렸을 때 (ArbeitItemUI 가 불러줌)
    public void OnClickArbeit(MinigameDefinition arbeit)
    {
        if (GameManager.instance.arbeit.StartArbeit(arbeit) == false)
        {
            return; // 횟수가 없거나 이미 하는 중
        }

        if (listPart != null)
        {
            listPart.SetActive(false);
        }
        if (resultPart != null)
        {
            resultPart.SetActive(false);
        }

        // 종목에 맞는 미니게임 화면 켜기
        if (arbeit.kind == MinigameKind.BookSort && bookSortView != null)
        {
            bookSortView.Play(this);
            return;
        }
        if (arbeit.kind == MinigameKind.Spotlight && spotlightView != null)
        {
            spotlightView.Play(this);
            return;
        }
        if (arbeit.kind == MinigameKind.Promo && promoView != null)
        {
            promoView.Play(this);
            return;
        }

        // 화면을 안 만들어둔 종목이면 그냥 실패로 끝냄 (횟수는 이미 깎였으니 결과는 보여줌)
        Debug.LogWarning("이 알바의 미니게임 화면이 연결 안 됨: " + arbeit.minigameName);
        OnGameFinished(0f);
    }

    // 미니게임이 끝났을 때 (각 미니게임 화면이 불러줌)
    public void OnGameFinished(float rate)
    {
        // 보수를 얼마 받았는지 보려고 전후 크레딧을 비교함
        int before = GameManager.instance.economy.credits;
        GameManager.instance.arbeit.FinishArbeit(rate);
        int earned = GameManager.instance.economy.credits - before;

        HideAllGames();

        if (listPart != null)
        {
            listPart.SetActive(false);
        }
        if (resultPart != null)
        {
            resultPart.SetActive(true);
        }
        if (resultText != null)
        {
            resultText.text = "수행률 " + Mathf.RoundToInt(rate * 100) + "%\n"
                + "받은 크레딧 " + earned;
        }
    }

    // 미니게임 화면 전부 끄기
    void HideAllGames()
    {
        if (bookSortView != null)
        {
            bookSortView.gameObject.SetActive(false);
        }
        if (spotlightView != null)
        {
            spotlightView.gameObject.SetActive(false);
        }
        if (promoView != null)
        {
            promoView.gameObject.SetActive(false);
        }
    }

    // 결과 화면의 확인 버튼 OnClick 에 연결
    public void OnClickResultConfirm()
    {
        ShowList();
    }
}
