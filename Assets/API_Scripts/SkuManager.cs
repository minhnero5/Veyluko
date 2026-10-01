using System;
using System.Collections.Generic;

[System.Serializable]
public class SkuManager
{
    public List<SKU_INAPP> skuInapps;

    public int GetTotalSku()
    {
        return skuInapps.Count;
    }

    public bool IsSkuAvailable(string id)
    {
        foreach (var item in skuInapps)
        {
            if (item.id == id && item.status?.Equals("ACTIVE", System.StringComparison.OrdinalIgnoreCase) == true)
            {
                return true;
            }
        }
        return false;
    }

    public string GetTileProduct(string id)
    {
        foreach (var item in skuInapps)
        {
            if (item.id == id)
            {
                return item.title;
            }
        }
        return string.Empty;
    }

    public string GetDescriptionProduct(string id)
    {
        foreach (var item in skuInapps)
        {
            if (item.id == id)
            {
                return item.description;
            }
        }
        return string.Empty;
    }

    public float GetPriceProduct(string id)
    {
        foreach (var item in skuInapps)
        {
            if (item.id == id)
            {
                return item.price;
            }
        }
        return 0f;
    }

    public int GetAmountProduct(string id)
    {
        foreach (var item in skuInapps)
        {
            if (item.id == id)
            {
                return item.amount;
            }
        }
        return 0;
    }

}

[System.Serializable]
public class SKU_INAPP
{
    public string sku;
    public string id;
    public string status;
    public int amount;
    public float price;
    public string title;
    public string description;
}