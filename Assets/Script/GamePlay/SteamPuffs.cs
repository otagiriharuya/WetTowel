using UnityEngine;
using DG.Tweening;

// 保温庫の湯気：子スプライトをループで上昇＋フェードさせる（見た目だけ）
public class SteamPuffs : MonoBehaviour
{
    private void Start()
    {
        int i = 0;
        foreach (Transform child in transform)
        {
            SpriteRenderer sr = child.GetComponent<SpriteRenderer>();
            if (sr == null)
            {
                i++;
                continue;
            }
            Vector3 startPos = child.localPosition;
            float delay = i * 0.45f;
            Color c = sr.color;
            c.a = 0f;
            sr.color = c;
            Sequence seq = DOTween.Sequence();
            seq.AppendInterval(delay)
               .AppendCallback(() => { if (child != null) child.localPosition = startPos; })
               .Append(sr.DOFade(0.7f, 0.35f))
               .Join(child.DOLocalMoveY(startPos.y + 0.9f, 1.4f).SetEase(Ease.OutQuad))
               .Append(sr.DOFade(0f, 0.4f))
               .SetLoops(-1);
            i++;
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
