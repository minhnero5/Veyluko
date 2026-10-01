using NUnit.Framework.Interfaces;
using TMPro;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.UI;
using OctoberStudio;
using System;
using System.Collections;

public class IAPButtonView : MonoBehaviour
{
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI priceText;
    [SerializeField] int inappIndex = 0;

    Button buyBtn;
    int coin = 500;
    private string productId = "coin_inapp_1";
    private Product product;

    private void Awake()
    {
        buyBtn = GetComponentInChildren<Button>();
        buyBtn.onClick.AddListener(OnBuyButtonClicked);
        productId = IAPManager.Instance.GetProductId(inappIndex);

#if !UNITY_EDITOR
        if (!ServerUrl.IsServerConfigured)
        {
            StartCoroutine(WaitProductFetch());
        }
#endif
    }

    void OnEnable()
    {
        ShopWindowBehavior.OnShopWindowOpened += FetchedInapp;
        IAPManager.SetBuyButtonState += SetActiveBuyBtn;
    }
    void OnDisable()
    {
        ShopWindowBehavior.OnShopWindowOpened -= FetchedInapp;
        IAPManager.SetBuyButtonState -= SetActiveBuyBtn;
    }

    private void SetActiveBuyBtn(bool isActive)
    {
        Debug.Log("[IAP] Setting buy button active state to: " + isActive);
        buyBtn.gameObject.SetActive(isActive);
    }

    private void FetchedInapp()
    {
        if (!ServerUrl.IsServerConfigured)
        {
            Debug.Log("Server URL not configured, skipping product fetch on Server.");
#if !UNITY_EDITOR
            OnProductFetched(IAPManager.Instance.GetProductById(productId));
#endif
            return;
        }

        Debug.Log("[IAP] Fetching product: " + productId);
        if (UserDataManager.Instance.isServerOn && UserDataManager.Instance.skuManager.skuInapps.Count > 0)
        {
            if (UserDataManager.Instance.skuManager.IsSkuAvailable(productId))
            {
#if UNITY_EDITOR
                FectchProductFromSKU();
                Debug.Log("[IAP] Editor Mode: Fetching product from SKU manager.");
                return;
#endif

                if (IAPManager.Instance.GetProductById(productId) != null)
                {
                    FectchProductFromSKU();
                }
                else
                {
                    gameObject.SetActive(false);
                    Debug.Log("Product " + productId + " is not available for purchase.");
                }
            }
            else
            {
                Debug.Log("Cant fetching product" + productId + " from SKU Manager");
                gameObject.SetActive(false);
            }
        }
        else
        {
#if !UNITY_EDITOR
        OnProductFetched(IAPManager.Instance.GetProductById(productId));
#endif
        }
    }

    private void OnBuyButtonClicked()
    {
        if (!ServerUrl.IsServerConfigured)
        {
            IAPManager.Instance.BuyProduct(this.productId, () =>
                {
                    OnPurchaseSuccess();
                });
            return;
        }

        if (UserDataManager.Instance.isLoggedIn)
        {
            UserDataManager.OnBuyInappAction = () =>
            {
                Debug.Log($"[IAP] Attempting to purchase product: {productId}");
                IAPManager.Instance.BuyProduct(this.productId, () =>
                {
                    OnPurchaseSuccess();
                });
            };

            UserDataManager.Instance.GetUserStatus();
        }
        else
        {
            Action purchaseAction = () =>
            {
                IAPManager.Instance.BuyProduct(this.productId, () =>
                {
                    OnPurchaseSuccess();
                });
            };

            ApiPopupManager.Instance.ShowAskLoginPopup(purchaseAction, purchaseAction);
        }
    }

    public void FectchProductFromSKU()
    {
        coin = UserDataManager.Instance.skuManager.GetAmountProduct(productId);
        titleText.text = coin.ToString() + " Coins";
        priceText.text = UserDataManager.Instance.skuManager.GetPriceProduct(productId).ToString() + "$";
    }

    public void OnProductFetched(Product product)
    {
        if (product == null)
        {
            Debug.LogError($"[IAP] Product with ID {productId} not found.");
            gameObject.SetActive(false);
            return;
        }

        if (product.availableToPurchase)
        {
            string tile = product.metadata.localizedTitle;
            coin = int.Parse(tile.Split(' ')[0]);
            titleText.text = coin.ToString() + " Coins";
            priceText.text = product.metadata.localizedPriceString;
        }
    }

    public void OnPurchaseSuccess()
    {
        GameController.Gold.Deposit(coin);

        if (!UserDataManager.Instance.isLoggedIn)
        {
            UserDataManager.Instance.AddCoins(coin);
            NoticePopUp.Instance.ShowMessage("Purchase Successful");
        }
    }

    IEnumerator WaitProductFetch()
    {
        while (!IAPManager.Instance.isProductsFetched)
        {
            yield return null;
        }
        if (IAPManager.Instance.canShowShop)
        {
            OnProductFetched(IAPManager.Instance.GetProductById(productId));
        }
    }

}
