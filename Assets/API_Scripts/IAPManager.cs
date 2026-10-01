using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

public class IAPManager : MonoBehaviour
{
    public static IAPManager Instance { get; private set; }
    StoreController m_StoreController;
    private Dictionary<string, Action> purchaseCallbacks = new();
    public static Action<Product> OnProductFetched;
    public static Action<bool> SetBuyButtonState;
    public bool canShowShop = false;
    public bool isProductsFetched = false;

    private List<string> productIds = new List<string>
    {
       "literallysimple_0.29","literallysimple_0.49", "literallysimple_0.99", "literallysimple_1.99", "literallysimple_2.99", "literallysimple_3.99",
        "literallysimple_4.99", "literallysimple_5.99", "literallysimple_7.99", "literallysimple_9.99", "literallysimple_19.99",
        "literallysimple_29.99", "literallysimple_49.99","literallysimple_99.99","literallysimple_13","literallysimple_14","literallysimple_15"
    };

    void Awake()
    {
        if (Instance != null) return;

        Instance = this;
        InitializeIAP();
        DontDestroyOnLoad(gameObject);
    }

    public string GetProductId(int index)
    {
        if (index < 0 || index >= productIds.Count)
        {
            Debug.LogError($"[IAP] Invalid product index: {index}");
            return null;
        }
        return productIds[index];
    }

    public Product GetProductById(string productId)
    {

        return m_StoreController.GetProductById(productId);
    }

    async void InitializeIAP()
    {
        m_StoreController = UnityIAPServices.StoreController();

        m_StoreController.OnPurchaseFailed += OnPurchaseFailed;
        m_StoreController.OnPurchasePending += OnPurchasePending;
        m_StoreController.OnPurchaseConfirmed += OnPurchaseConfirmed;
        m_StoreController.OnStoreDisconnected += OnStoreDisconnected;
        Debug.Log("Connecting to store.");


        await m_StoreController.Connect();

        m_StoreController.OnPurchaseDeferred  += OnDeferred;
        m_StoreController.OnProductsFetchFailed += OnProductsFetchedFailed;
        m_StoreController.OnProductsFetched += OnProductsFetched;
        m_StoreController.OnPurchasesFetched += OnPurchasesFetched;
        m_StoreController.OnPurchasesFetchFailed += OnPurchasesFetchFailed;
        FetchProducts();

        m_StoreController.FetchPurchases();
    }

    private void OnDeferred(DeferredOrder order)
    {      
        Debug.Log("Deferred: waiting for approval.");
    }

    public void BuyProduct(string productId, Action onSuccess = null)
    {
        SetBuyButtonState?.Invoke(false);
        if (purchaseCallbacks.ContainsKey(productId))
        {
            purchaseCallbacks[productId] = onSuccess;
        }
        else
        {
            purchaseCallbacks.Add(productId, onSuccess);
        }
        m_StoreController.PurchaseProduct(productId);
    }

    void FetchProducts()
    {
        List<ProductDefinition> initialProductsToFetch = new List<ProductDefinition>();
        foreach (var item in productIds)
        {
            var a = new ProductDefinition(item, ProductType.Consumable);
            initialProductsToFetch.Add(a);
        }
        m_StoreController.FetchProducts(initialProductsToFetch);
    }

    void OnPurchasePending(PendingOrder order)
    {
        var buyId = order.CartOrdered.Items().First()?.Product.definition.id;
        purchaseCallbacks.TryGetValue(buyId, out var callback);
        if (callback != null)
        {
            callback.Invoke();

            // Debug.Log("PendingOrder.Info.Receipt: " + order.Info.Receipt);
            Debug.Log($"[IAP] Purchase success for product: {buyId}");
        }
        else
        {
            Debug.LogWarning($"[IAP] No callback found for pending purchase: {buyId}");
        }
        m_StoreController.ConfirmPurchase(order);
    }

    void OnProductsFetched(List<Product> products)
    {
        Debug.Log($"[IAP] Fetched {products.Count} products.");
        if (products.Count > 0)
        {
            if (products.Any(p => p.availableToPurchase))
            {
                canShowShop = true;
            }
        }
        isProductsFetched = true;
    }

    void OnPurchaseFailed(FailedOrder order)
    {
        var product = order.CartOrdered.Items().First()?.Product;
        if (product == null)
        {
            Debug.Log("Could not find product in failed order.");
        }

        Debug.Log($"Purchase failed - Product: '{product?.definition.id}'," +
                  $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                  + $"Purchase Failure Details: {order.Details}");

        bool alreadyOwned =
            order.FailureReason == PurchaseFailureReason.DuplicateTransaction ||
            (order.Details?.ToString().IndexOf("ITEM_ALREADY_OWNED", StringComparison.OrdinalIgnoreCase) >= 0) ||
            (order.Details?.ToString().IndexOf("already own", StringComparison.OrdinalIgnoreCase) >= 0);

        if (alreadyOwned)
        {
            m_StoreController.FetchPurchases();
        }

        SetBuyButtonState?.Invoke(true);
    }
    void OnPurchaseConfirmed(Order order)
    {
        switch (order)
        {
            case ConfirmedOrder confirmedOrder:
                OnPurchaseConfirmed(confirmedOrder);
                break;
            case FailedOrder failedOrder:
                OnPurchaseConfirmationFailed(failedOrder);
                break;
            default:
                Debug.Log("Unknown OnPurchaseConfirmed result.");
                break;
        }

    }
    void OnPurchaseConfirmed(ConfirmedOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product == null)
        {
            Debug.Log("Could not find product in purchase confirmation.");
        }

        Debug.Log($"Purchase confirmed- Product: {product?.definition.id}");

        try
        {
            UserDataManager.Instance.SetOrderInfo(order);
            UserDataManager.Instance.VerifyPurchaseInfo();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[IAP] Error processing purchase: {ex.Message}");
            // Có thể thêm xử lý retry hoặc thông báo người dùng
        }
        SetBuyButtonState?.Invoke(true);
    }

    void OnPurchaseConfirmationFailed(FailedOrder order)
    {
        var product = GetFirstProductInOrder(order);
        if (product == null)
        {
            Debug.Log("Could not find product in failed confirmation.");
        }

        Debug.Log($"Confirmation failed - Product: '{product?.definition.id}'," +
                  $"PurchaseFailureReason: {order.FailureReason.ToString()},"
                  + $"Confirmation Failure Details: {order.Details}");

        SetBuyButtonState?.Invoke(true);
    }

    Product GetFirstProductInOrder(Order order)
    {
        return order.CartOrdered.Items().First()?.Product;
    }

    void OnStoreDisconnected(StoreConnectionFailureDescription description)
    {
        Debug.Log($"Store disconnected details: {description.message}");
    }

    void OnProductsFetchedFailed(ProductFetchFailed failure)
    {
        Debug.Log($"Products fetch failed for {failure.FailedFetchProducts.Count} products: {failure.FailureReason}");
    }

    void OnPurchasesFetched(Orders orders)
    {
        Debug.Log($"[IAP] PurchasesFetched - Pending:{orders.PendingOrders.Count}, Confirmed:{orders.ConfirmedOrders.Count}, Deferred:{orders.DeferredOrders.Count}");

        // Trên Google Play: các Pending chưa xử lý thường sẽ được đẩy qua OnPurchasePending để bạn xử lý/Confirm. :contentReference[oaicite:1]{index=1}
    }

    void OnPurchasesFetchFailed(PurchasesFetchFailureDescription desc)
    {
        Debug.LogWarning($"[IAP] PurchasesFetchFailed - {desc.FailureReason} | {desc.Message}");
    }

}
