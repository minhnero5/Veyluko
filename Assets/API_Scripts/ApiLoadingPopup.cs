using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ApiLoadingPopup : MonoBehaviour
{
   [SerializeField] private Image loadingImage;
    private Tween rotateTween;

    public void Show()
    {
        this.gameObject.SetActive(true);
    }

    public void Hide()
    {
        this.gameObject.SetActive(false);
    }
    private void OnEnable()
    {
        StartRotationAnimation();
    }

    private void OnDisable()
    {
        StopRotationAnimation();
    }

    private void StartRotationAnimation()
    {
        rotateTween = loadingImage.transform.DORotate(new Vector3(0, 0, -360), 0.2f, RotateMode.FastBeyond360)
            .SetLoops(-1, LoopType.Incremental)
            .SetEase(Ease.InBounce);
    }

    private void StopRotationAnimation()
    {
        rotateTween?.Kill();
    }

    private void OnMouseDown()
    {
        Hide();
    }


}
