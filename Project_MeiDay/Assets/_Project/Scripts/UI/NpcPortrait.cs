using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 일반 민원인 얼굴을 파츠로 조합해서 보여주는 것
// 파츠 그림들은 전부 같은 1920x1080 판에서 잘라낸 거라, 그 판 안에서의 원래 자리를
// 그대로 되살려 놓으면 눈/코/입이 얼굴 제자리에 붙음 (자리 데이터를 따로 안 만들어도 됨)
//
// 주의: 눈처럼 좌우 한 쌍인 파츠는 유니티 자동 자르기가 반쪽씩 따로 잘라놨음
// 그래서 한 그림 파일에서 나온 조각들을 한 묶음으로 보고 전부 같이 그려야 두 눈이 다 나옴
public class NpcPortrait : MonoBehaviour
{
    // 한 그림 파일에서 나온 조각 묶음 (눈이면 왼눈+오른눈)
    [System.Serializable]
    public class PartSet
    {
        public Sprite[] pieces;
    }

    // 뒤에서 앞으로 겹치는 순서대로 (뒤 옷 -> 몸 -> 앞 옷 -> 얼굴 -> 눈 -> 코 -> 입 -> 장식)
    // 조각이 여러 개인 파츠가 있어서 레이어마다 그릴 자리를 여러 개 둠
    [SerializeField] Transform clothesBackLayer;
    [SerializeField] Transform bodyLayer;
    [SerializeField] Transform clothesLayer;
    [SerializeField] Transform faceLayer;
    [SerializeField] Transform eyesLayer;
    [SerializeField] Transform noseLayer;
    [SerializeField] Transform mouthLayer;
    [SerializeField] Transform decoLayer;

    // 파츠 후보 (그림 파일 하나가 묶음 하나)
    [SerializeField] PartSet[] clothesBackParts;
    [SerializeField] PartSet[] bodyParts;
    [SerializeField] PartSet[] clothesParts;
    [SerializeField] PartSet[] faceParts;
    [SerializeField] PartSet[] eyesParts;
    [SerializeField] PartSet[] noseParts;
    [SerializeField] PartSet[] mouthParts;
    [SerializeField] PartSet[] decoParts;

    // 파츠를 잘라낸 원본 판 크기 (여기 맞춰서 자리를 되살림)
    [SerializeField] float sheetWidth = 1920f;
    [SerializeField] float sheetHeight = 1080f;

    // 판은 옆으로 넓지만 사람은 가운데 이만큼 안에만 그려져 있음 (제일 넓은 몸 파츠 기준)
    // 세로는 판 높이를 그대로 씀 - 사람이 판 바닥부터 천장까지 꽉 차게 그려져 있음
    // 이 네모를 아래 그릴 크기에 맞추니까 누굴 뽑아도 팔이 안 잘림
    [SerializeField] float charWidth = 800f;

    // 화면에 그릴 크기 - 머리 꼭대기가 이 오브젝트 자리에 오고 아래로 내려감
    [SerializeField] float drawWidth = 212f;
    [SerializeField] float drawHeight = 256f;

    // 위에서부터 세로로 얼마나 보여줄지 (0.75 = 머리부터 몸까지, 1 = 통째로)
    // 넘치는 아래쪽은 창이 잘라냄
    [Range(0.3f, 1f)]
    [SerializeField] float showRatio = 0.75f;

    // 장식은 있을 때가 있고 없을 때가 있어야 자연스러움
    [SerializeField] int decoChanceOutOf10 = 6;

    // 피부색 후보 (기획에서 준 색표) - 몸이랑 얼굴에만 입힘
    // 파츠 그림이 흰 바탕이라 곱하기 색으로 바로 물듦
    [SerializeField]
    Color[] skinColors = new Color[]
    {
        new Color32(0xAD, 0x35, 0x35, 0xFF),
        new Color32(0xDF, 0xA0, 0x22, 0xFF),
        new Color32(0xFF, 0x99, 0x00, 0xFF),
        new Color32(0xFF, 0xFF, 0x8A, 0xFF),
        new Color32(0xAA, 0xFF, 0x00, 0xFF),
        new Color32(0x00, 0xFF, 0x55, 0xFF),
        new Color32(0x00, 0xFF, 0xAA, 0xFF),
        new Color32(0x2B, 0xFF, 0xFF, 0xFF),
        new Color32(0x50, 0xB2, 0xE2, 0xFF),
        new Color32(0x95, 0x74, 0xD8, 0xFF),
        new Color32(0x5A, 0x5A, 0x78, 0xFF),
        new Color32(0x5C, 0x49, 0x3B, 0xFF),
        new Color32(0x8D, 0x80, 0x55, 0xFF),
        new Color32(0xC5, 0xB2, 0x54, 0xFF),
        new Color32(0xEB, 0xB5, 0xEB, 0xFF),
        new Color32(0x97, 0x43, 0x5F, 0xFF),
        new Color32(0x8C, 0x8C, 0x8C, 0xFF),
    };

    // 같은 사람이면 항상 같은 얼굴이어야 함 (매번 바뀌면 누군지 못 알아봄)
    // 그래서 민원인 id 같은 글자를 씨앗으로 받아서 뽑음
    public void Build(string seedText)
    {
        int seed = 0;
        if (string.IsNullOrEmpty(seedText) == false)
        {
            for (int i = 0; i < seedText.Length; i++)
            {
                seed = seed * 31 + seedText[i]; // 글자마다 섞어서 숫자 하나로
            }
        }

        // 전역 난수를 건드리면 게임 다른 부분(민원 뽑기 등)이 영향을 받아서 따로 씀
        Random.State keep = Random.state;
        Random.InitState(seed);

        // 피부색도 씨앗으로 뽑음 - 같은 사람이면 항상 같은 색
        Color skin = Color.white;
        if (skinColors != null && skinColors.Length > 0)
        {
            skin = skinColors[Random.Range(0, skinColors.Length)];
        }

        Put(clothesBackLayer, Pick(clothesBackParts), Color.white);
        Put(bodyLayer, Pick(bodyParts), skin);
        Put(clothesLayer, Pick(clothesParts), Color.white);
        Put(faceLayer, Pick(faceParts), skin);
        Put(eyesLayer, Pick(eyesParts), Color.white);
        Put(noseLayer, Pick(noseParts), Color.white);
        Put(mouthLayer, Pick(mouthParts), Color.white);

        // 장식은 확률로 (없으면 빈 칸)
        if (Random.Range(0, 10) < decoChanceOutOf10)
        {
            Put(decoLayer, Pick(decoParts), Color.white);
        }
        else
        {
            Put(decoLayer, null, Color.white);
        }

        Random.state = keep;
    }

    // 파츠 묶음 하나 뽑기 (비어있으면 null)
    PartSet Pick(PartSet[] parts)
    {
        if (parts == null || parts.Length == 0)
        {
            return null;
        }

        return parts[Random.Range(0, parts.Length)];
    }

    // 뽑은 묶음의 조각들을 원본 판에서의 자리 그대로 전부 놓기 (tint 는 피부색 입힐 때 씀)
    void Put(Transform layer, PartSet set, Color tint)
    {
        if (layer == null)
        {
            return; // 아직 안 꽂아둔 칸
        }

        int count = 0;
        if (set != null && set.pieces != null)
        {
            count = set.pieces.Length;
        }

        // 그릴 자리가 모자라면 만들고, 남으면 꺼둠 (매번 지웠다 만들면 낭비라 재활용)
        while (layer.childCount < count)
        {
            GameObject go = new GameObject("Piece", typeof(RectTransform));
            go.transform.SetParent(layer, false);
            Image made = go.AddComponent<Image>();
            made.raycastTarget = false;
        }

        for (int i = 0; i < layer.childCount; i++)
        {
            Image slot = layer.GetChild(i).GetComponent<Image>();
            if (slot == null)
            {
                continue;
            }

            if (i >= count || set.pieces[i] == null)
            {
                slot.enabled = false;
                continue;
            }

            slot.enabled = true;
            slot.sprite = set.pieces[i];
            slot.color = tint; // 피부 파츠는 피부색, 나머지는 흰색 (그림 원색 그대로)
            PlaceAsInSheet(slot.rectTransform, set.pieces[i]);
        }
    }

    // 위에서 showRatio 만큼이 그릴 높이를 채우는 배율
    // 단 가로로 삐져나가면 가로에 맞춰 줄임 - 팔이 잘려나가면 안 되니까
    float DrawScale()
    {
        float byHeight = drawHeight / (sheetHeight * showRatio);
        float byWidth = drawWidth / charWidth;

        if (byWidth < byHeight)
        {
            return byWidth;
        }
        return byHeight;
    }

    // 조각을 원본 판에서 있던 자리에 그대로 놓기
    void PlaceAsInSheet(RectTransform rt, Sprite part)
    {
        if (charWidth <= 0f || sheetHeight <= 0f)
        {
            return; // 설정 실수
        }

        float scale = DrawScale();
        Rect r = part.rect;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(r.width * scale, r.height * scale);

        // 좌우는 판 가운데 기준으로 얼마나 떨어져 있었는지
        // 위아래는 판 천장 기준 - 그래야 머리가 창 위에 붙고 아래쪽이 잘림
        float offX = (r.x + r.width * 0.5f) - sheetWidth * 0.5f;
        float offY = (r.y + r.height * 0.5f) - sheetHeight;
        rt.anchoredPosition = new Vector2(offX * scale, offY * scale);
    }
}
