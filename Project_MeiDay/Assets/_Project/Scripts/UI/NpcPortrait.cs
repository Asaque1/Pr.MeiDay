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

    // 화면에 얼마나 크게 그릴지 (판 높이를 이 높이에 맞춤. 가로는 마스크가 알아서 잘라줌)
    [SerializeField] float drawHeight = 385f;

    // 장식은 있을 때가 있고 없을 때가 있어야 자연스러움
    [SerializeField] int decoChanceOutOf10 = 6;

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

        Put(clothesBackLayer, Pick(clothesBackParts));
        Put(bodyLayer, Pick(bodyParts));
        Put(clothesLayer, Pick(clothesParts));
        Put(faceLayer, Pick(faceParts));
        Put(eyesLayer, Pick(eyesParts));
        Put(noseLayer, Pick(noseParts));
        Put(mouthLayer, Pick(mouthParts));

        // 장식은 확률로 (없으면 빈 칸)
        if (Random.Range(0, 10) < decoChanceOutOf10)
        {
            Put(decoLayer, Pick(decoParts));
        }
        else
        {
            Put(decoLayer, null);
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

    // 뽑은 묶음의 조각들을 원본 판에서의 자리 그대로 전부 놓기
    void Put(Transform layer, PartSet set)
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
            slot.color = Color.white; // 자리 잡아두려고 칠해둔 색이 남아있으면 그림이 물듦
            PlaceAsInSheet(slot.rectTransform, set.pieces[i]);
        }
    }

    // 조각을 원본 판에서 있던 자리에 그대로 놓기
    void PlaceAsInSheet(RectTransform rt, Sprite part)
    {
        if (sheetHeight <= 0f)
        {
            return; // 설정 실수
        }

        float scale = drawHeight / sheetHeight;
        Rect r = part.rect;

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(r.width * scale, r.height * scale);

        // 판 가운데를 기준으로 얼마나 떨어져 있었는지
        float offX = (r.x + r.width * 0.5f) - sheetWidth * 0.5f;
        float offY = (r.y + r.height * 0.5f) - sheetHeight * 0.5f;
        rt.anchoredPosition = new Vector2(offX * scale, offY * scale);
    }
}
