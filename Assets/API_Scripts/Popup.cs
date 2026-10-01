using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class Popup : MonoBehaviour
{
    [SerializeField] protected Image background;
    /* [SerializeField]*/
    protected Color32 startColor = new Color32(3, 9, 26, 0);
    /*[SerializeField]*/
    protected Color32 endColor = new Color32(3, 9, 26, 204);

    [SerializeField] protected Transform popup;
    [SerializeField] protected Vector3 startSCale = Vector3.zero;
    [SerializeField] protected Vector3 endSCale = Vector3.one;

    [SerializeField] protected float timeShowAnimation = 0.3f;
    [SerializeField] protected Ease easeShow = Ease.OutBack;
    [SerializeField] protected Ease easeHide = Ease.InBack;

    [SerializeField] protected Button btnClose;

    protected bool canClose = true;


    public void Show(Action completeAction = null)
    {
        canClose = false;
        if (background)
        {
            background.color = startColor;
            background.DOColor(endColor, timeShowAnimation).SetUpdate(true);
        }
        this.gameObject.SetActive(true);
        if (btnClose)
        {
            btnClose.onClick.RemoveAllListeners();
        }
        if (popup)
        {
            popup.localScale = startSCale;
            popup.DOScale(endSCale, timeShowAnimation).SetEase(easeShow).SetUpdate(true).OnComplete(() =>
            {
                if (btnClose)
                {
                    btnClose.onClick.AddListener(() =>
                    {
                        PlayClickButton();
                        canClose = true;
                        Hide(null);
                    });
                }
                canClose = true;
                completeAction?.Invoke();
            });
        }
        else
        {
            if (btnClose)
            {
                btnClose.onClick.AddListener(() =>
                {
                    PlayClickButton();
                    canClose = true;
                    Hide(null);
                });
            }
            canClose = true;
            completeAction?.Invoke();
        }
    }
    public void Hide(Action completeAction = null)
    {
        if (!canClose) return;
        canClose = false;
        if (background)
        {
            background.color = endColor;
            background.DOColor(startColor, timeShowAnimation).SetUpdate(true);
        }
        if (popup)
        {
            popup.localScale = endSCale;
            popup.DOScale(startSCale, timeShowAnimation).SetEase(easeHide).SetUpdate(true).OnComplete(() =>
            {
                completeAction?.Invoke();
                OnClosedPopup();
                this.gameObject.SetActive(false);
            });
        }
        else
        {
            completeAction?.Invoke();
            OnClosedPopup();
            this.gameObject.SetActive(false);
        }
    }

    public virtual void OnClosedPopup() { }

    public void PlayClickButton()
    {
       
    }
}
