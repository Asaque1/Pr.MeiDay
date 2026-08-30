using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 정해둔 날 밤에 그 상점을 열면 나오는 대사 한 줄짜리 설정
// (예: 7일차 밤에 브로커 상점을 누르면 Day7AtBrokerStore 재생)
[System.Serializable]
public class ShopDialogue
{
    public ShopType shop;          // 어느 상점을 눌렀을 때
    public int day = 7;            // 며칠째 밤에
    public DialogueData dialogue;  // 틀어줄 대사

    // 본 기록에 쓸 이름 (이벤트 기록을 같이 써서 저장·복원이 따라옴)
    public string SeenId()
    {
        return "shopdlg_" + shop + "_d" + day;
    }
}

// 밤 상점 화면 - 상점 네 곳을 골라가며 아이템 사게 해줌
public class ShopView : MonoBehaviour
{
    [Header("상점 대사")]
    [SerializeField] ShopDialogue[] shopDialogues; // 정해둔 날 그 상점을 열면 뜨는 대사


    [Header("아이템 목록")]
    [SerializeField] Transform itemParent;  // 아이템 칸들 놓이는 자리
    [SerializeField] ShopItemUI itemPrefab; // 아이템 칸 프리팹
    [SerializeField] int minSlots = 6;      // 물건이 적어도 이만큼은 빈 칸으로 채워둠 (격자가 듬성해 보이지 않게)

    // 우주선 칸이 뜨면 목록 자리를 그만큼 줄여줌 (안 그러면 칸이 겹쳐 보임)
    [SerializeField] RectTransform scrollArea;
    [SerializeField] float scrollHeightNormal = 272f;
    [SerializeField] float scrollHeightWithShip = 138f;

    [Header("상점 정보")]
    [SerializeField] TMP_Text shopNameText; // 지금 보는 상점 이름이랑 레벨
    [SerializeField] TMP_Text infoText;     // 안내 문구 (샀다 / 돈 부족)
    [SerializeField] Image npcImage;        // 상점 주인 그림
    [SerializeField] Sprite bookstoreNpc;
    [SerializeField] Sprite theaterNpc;
    [SerializeField] Sprite partyNpc;
    [SerializeField] Sprite brokerNpc;

    [Header("아래 줄")]
    [SerializeField] GameObject bottomRow;    // 아르바이트 버튼 줄 (우주선 칸이 뜨면 자리를 비켜줌)
    [SerializeField] TMP_Text arbeitLeftText; // 남은 알바 횟수

    [Header("우주선 (브로커에서만 보임)")]
    // 우주선은 목표 물건이라 다른 물건이랑 같은 칸에 섞이면 눈에 안 띔
    [SerializeField] GameObject spaceshipCard;
    [SerializeField] Image spaceshipIcon;
    [SerializeField] TMP_Text spaceshipPriceText;
    [SerializeField] TMP_Text spaceshipButtonText;

    [Header("아이템 상세 창")]
    // 누르자마자 사버리면 실수로 돈이 나감 - 한 번 물어봄
    [SerializeField] GameObject popup;
    [SerializeField] Image popupIcon;
    [SerializeField] TMP_Text popupNameText;
    [SerializeField] TMP_Text popupPriceText;
    [SerializeField] TMP_Text popupKindText;
    [SerializeField] TMP_Text popupDescText;
    [SerializeField] GameObject popupBuyButton;

    public ShopType currentShop = ShopType.Bookstore; // 지금 보고 있는 상점

    List<ShopItemUI> _itemUis = new List<ShopItemUI>();
    ItemDefinition _popupItem; // 지금 상세 창에 띄워둔 물건

    // 패널 켜질 때 목록 만들어줌
    void OnEnable()
    {
        if (GameManager.instance == null)
        {
            return;
        }

        SetInfo("");
        ClosePopup();
        Rebuild();
    }

    // 안내 문구 (아직 안 꽂아뒀어도 안 터지게)
    void SetInfo(string text)
    {
        if (infoText != null)
        {
            infoText.text = text;
        }
    }

    void SetOn(GameObject target, bool on)
    {
        if (target == null)
        {
            return;
        }

        target.SetActive(on);
    }

    // 지금 상점의 아이템 목록 지우고 다시 만들기
    void Rebuild()
    {
        for (int i = 0; i < _itemUis.Count; i++)
        {
            if (_itemUis[i] == null)
            {
                continue;
            }

            // Destroy 는 이 프레임 끝에야 실제로 지워짐
            // 탭을 바꾼 그 프레임에는 헌 칸이 그대로 남아서 격자에 두 배로 깔림
            _itemUis[i].gameObject.SetActive(false);
            Destroy(_itemUis[i].gameObject);
        }
        _itemUis.Clear();

        ShopManager shop = GameManager.instance.shop;

        if (shopNameText != null)
        {
            // 알바로 번 경험치가 어디까지 찼는지 같이 보여줌 (숫자만 있으면 알바가 헛일로 보임)
            int have;
            int need;
            shop.GetLevelProgress(currentShop, out have, out need);

            int level = shop.GetShopLevel(currentShop);
            if (level >= 5)
            {
                shopNameText.text = "상점 LV: " + level + " (최대)";
            }
            else
            {
                shopNameText.text = "상점 LV: " + level + "   " + have + " / " + need;
            }
        }
        if (npcImage != null)
        {
            Sprite npc = NpcOf(currentShop);
            npcImage.sprite = npc;
            npcImage.enabled = (npc != null);
        }

        RebuildSpaceship();

        // 프리팹이나 놓을 자리를 안 꽂아뒀으면 목록을 못 만듦
        if (itemPrefab == null || itemParent == null)
        {
            Debug.LogWarning("상점 화면에 아이템 프리팹이나 놓을 자리가 연결 안 됨");
            return;
        }

        List<ItemDefinition> items = GameManager.instance.content.items;
        int made = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == null)
            {
                continue; // 데이터에 빈 칸
            }
            if (items[i].shopType != currentShop)
            {
                continue; // 다른 상점 물건
            }
            if (items[i].isSpaceshipTicket == true)
            {
                continue; // 우주선은 아래 따로 큰 칸으로 보여줌
            }
            if (shop.IsUnlocked(items[i]) == false)
            {
                continue; // 아직 해금 안 된 물건은 안 보여줌
            }

            // 먼저 목록에 넣어둬야 Set 에서 뭐가 잘못돼도 고아 오브젝트가 안 남음
            ShopItemUI itemUi = Instantiate(itemPrefab, itemParent);
            _itemUis.Add(itemUi);
            itemUi.Set(this, items[i]);
            made++;
        }

        // 남는 자리는 빈 칸으로 채움
        for (int i = made; i < minSlots; i++)
        {
            ShopItemUI empty = Instantiate(itemPrefab, itemParent);
            _itemUis.Add(empty);
            empty.SetEmpty();
        }
    }

    // 우주선 칸 (브로커에서만, 아직 안 샀을 때만)
    void RebuildSpaceship()
    {
        ItemDefinition ticket = FindSpaceship();
        bool show = (currentShop == ShopType.Broker
            && ticket != null
            && GameManager.instance.hasSpaceship == false);

        SetOn(spaceshipCard, show);

        // 우주선 칸이 자리를 많이 먹어서 목록을 그만큼 줄여줌
        if (scrollArea != null)
        {
            float h = scrollHeightNormal;
            if (show == true) { h = scrollHeightWithShip; }
            scrollArea.sizeDelta = new Vector2(scrollArea.sizeDelta.x, h);
        }

        // 우주선 칸이랑 아르바이트 줄은 같은 자리를 써서 하나만 보임
        SetOn(bottomRow, show == false);
        if (arbeitLeftText != null && GameManager.instance.arbeit != null)
        {
            arbeitLeftText.text = "남은 횟수  " + GameManager.instance.arbeit.arbeitLeftToday
                + "/" + GameManager.instance.arbeit.MaxToday();
        }

        if (show == false)
        {
            return;
        }

        // 아이템에 그림을 안 넣어뒀으면 화면에 꽂아둔 임시 그림을 그대로 씀
        if (spaceshipIcon != null && ticket.icon != null)
        {
            spaceshipIcon.sprite = ticket.icon;
            spaceshipIcon.enabled = true;
        }
        if (spaceshipPriceText != null)
        {
            spaceshipPriceText.text = GameManager.instance.shop.PriceFor(ticket) + " cr";
        }

        // 아직 조건이 안 됐으면 버튼에 그 이유를 적어둠
        if (spaceshipButtonText != null)
        {
            string reason = GameManager.instance.shop.BuyBlockReason(ticket);
            if (reason.Length > 0)
            {
                spaceshipButtonText.text = reason;
            }
            else
            {
                spaceshipButtonText.text = "우주선 구매";
            }
        }
    }

    // 우주선표 찾기 (없으면 null)
    ItemDefinition FindSpaceship()
    {
        List<ItemDefinition> items = GameManager.instance.content.items;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null && items[i].isSpaceshipTicket == true)
            {
                return items[i];
            }
        }

        return null;
    }

    // 상점별 주인 그림
    Sprite NpcOf(ShopType type)
    {
        if (type == ShopType.Bookstore)
        {
            return bookstoreNpc;
        }
        if (type == ShopType.Theater)
        {
            return theaterNpc;
        }
        if (type == ShopType.PartyShop)
        {
            return partyNpc;
        }

        return brokerNpc;
    }

    // 사용 형태 (상세 창에 적음)
    string KindName(ItemKind kind)
    {
        if (kind == ItemKind.Permanent)
        {
            return "영구형";
        }
        if (kind == ItemKind.RandomBox)
        {
            return "랜덤박스";
        }

        return "소모형";
    }

    // 아이템 칸이 눌렸다고 알려줌 - 바로 사지 않고 상세 창부터 띄움
    public void OnClickItem(ItemDefinition item)
    {
        _popupItem = item;
        SetInfo("");

        int level = GameManager.instance.inventory.GetItemLevel(item.id);
        string reason = GameManager.instance.shop.BuyBlockReason(item);

        if (popupIcon != null)
        {
            Sprite icon = item.IconFor(level);
            popupIcon.sprite = icon;
            popupIcon.enabled = (icon != null);
        }
        if (popupNameText != null)
        {
            popupNameText.text = item.itemName;

            // 영구템은 다음에 살 게 몇 레벨짜리인지 같이 보여줌
            if (item.kind == ItemKind.Permanent)
            {
                popupNameText.text = item.itemName + " lv" + level + " / " + item.maxLevel;
            }
        }
        if (popupPriceText != null)
        {
            if (reason.Length > 0)
            {
                popupPriceText.text = reason;
            }
            else
            {
                popupPriceText.text = GameManager.instance.shop.PriceFor(item) + " cr";
            }
        }
        if (popupKindText != null)
        {
            popupKindText.text = "사용 형태: " + KindName(item.kind);
        }
        if (popupDescText != null)
        {
            popupDescText.text = item.DescriptionFor(level);
        }

        // 못 사는 물건은 구매 버튼을 아예 숨김
        SetOn(popupBuyButton, reason.Length == 0);
        SetOn(popup, true);
    }

    // 상세 창 닫기
    void ClosePopup()
    {
        _popupItem = null;
        SetOn(popup, false);
    }

    // ---- 아래는 버튼 OnClick 에 연결해서 씀 ----

    // 상세 창: 구매하기
    public void OnClickPopupBuy()
    {
        if (_popupItem == null)
        {
            return;
        }

        ItemDefinition item = _popupItem;
        string reason = GameManager.instance.shop.BuyBlockReason(item);
        if (reason.Length > 0)
        {
            SetInfo(item.itemName + " - " + reason);
            PlayLocked();
            ClosePopup();
            return;
        }

        if (GameManager.instance.shop.Buy(item) == true)
        {
            SetInfo(item.itemName + " 구매 완료");
        }
        else
        {
            SetInfo("크레딧이 부족함");
            PlayLocked();
        }

        ClosePopup();
        Rebuild(); // 레벨이랑 품절 표시 갱신
    }

    // 지금 못 사는 걸 눌렀을 때 나는 소리
    void PlayLocked()
    {
        if (SoundManager.instance != null)
        {
            SoundManager.instance.PlaySfx(SoundManager.instance.lockedClick);
        }
    }

    // 상세 창: 취소
    public void OnClickPopupCancel()
    {
        ClosePopup();
    }

    // 우주선 구매 (큰 칸의 버튼)
    public void OnClickBuySpaceship()
    {
        ItemDefinition ticket = FindSpaceship();
        if (ticket == null)
        {
            return;
        }

        string reason = GameManager.instance.shop.BuyBlockReason(ticket);
        if (reason.Length > 0)
        {
            SetInfo("우주선 - " + reason);
            return;
        }

        if (GameManager.instance.shop.Buy(ticket) == true)
        {
            SetInfo("우주선 구매 완료");
        }
        else
        {
            SetInfo("크레딧이 부족함");
        }

        Rebuild();
    }

    // 탭: 서점
    public void OnClickBookstore()
    {
        ChangeShop(ShopType.Bookstore);
    }

    // 탭: 연극단
    public void OnClickTheater()
    {
        ChangeShop(ShopType.Theater);
    }

    // 탭: 파티용품점
    public void OnClickPartyShop()
    {
        ChangeShop(ShopType.PartyShop);
    }

    // 탭: 브로커
    public void OnClickBroker()
    {
        ChangeShop(ShopType.Broker);
    }

    // 다른 상점으로 바꾸기
    void ChangeShop(ShopType type)
    {
        currentShop = type;
        SetInfo("");
        ClosePopup();
        Rebuild();

        TryShopDialogue(type);
    }

    // 정해둔 날 밤에 그 상점을 열면 대사를 한 번 틀어줌
    // 대사창(DialogueSystem)은 게임 화면과 다른 캔버스라, 뒤에 상점 배경이 그대로 깔림
    void TryShopDialogue(ShopType type)
    {
        GameManager gm = GameManager.instance;
        if (gm == null || shopDialogues == null)
        {
            return;
        }

        // 밤에만 (낮에 상점 화면이 열릴 일은 없지만 안전하게)
        if (gm.phase != GamePhase.NightShop)
        {
            return;
        }

        for (int i = 0; i < shopDialogues.Length; i++)
        {
            ShopDialogue sd = shopDialogues[i];
            if (sd == null || sd.dialogue == null || sd.shop != type)
            {
                continue;
            }
            if (sd.day != gm.calendar.day)
            {
                continue;
            }

            // 대사 파일만 만들어두고 안이 아직 빈 경우가 있음
            // 그냥 두면 아무것도 안 보여준 채 '봤다'고 적혀서, 나중에 내용이 채워져도 안 나옴
            if (sd.dialogue.dialogueLines == null || sd.dialogue.dialogueLines.Length == 0)
            {
                return;
            }

            // 한 번 본 건 다시 안 띄움 (이벤트랑 같은 기록을 써서 저장까지 따라감)
            string seenId = sd.SeenId();
            if (gm.events == null || gm.events.HasSeen(seenId) == true)
            {
                return;
            }

            if (DialogueSystem.Instance == null)
            {
                Debug.LogWarning("씬에 DialogueSystem 이 없어서 상점 대사를 못 띄움: " + seenId);
                return;
            }

            gm.events.MarkSeen(seenId);
            DialogueSystem.Instance.StartDialogue(sd.dialogue);
            return;
        }
    }
}
