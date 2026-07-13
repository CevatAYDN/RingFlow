using UnityEngine;
using System.Collections.Generic;

namespace RingFlow.Gameplay.Economy
{
    [System.Serializable]
    public struct StoreProductEntry
    {
        public string Id;
        public ProductType Type;
        public string PriceString;
        public string DisplayNameKey;
        public string DescriptionKey;
    }

    public enum ProductType
    {
        Consumable,
        NonConsumable,
        Subscription
    }

    [CreateAssetMenu(fileName = "StoreCatalog", menuName = "RingFlow/Store Catalog", order = 61)]
    public class StoreCatalogSO : ScriptableObject
    {
        public List<StoreProductEntry> Products = new()
        {
            new() { Id = "remove_ads",  Type = ProductType.NonConsumable, PriceString = "$3.99",  DisplayNameKey = "store.remove_ads",  DescriptionKey = "" },
            new() { Id = "coins_100",   Type = ProductType.Consumable,     PriceString = "$0.99",  DisplayNameKey = "store.coins_100",   DescriptionKey = "" },
            new() { Id = "diamonds_50", Type = ProductType.Consumable,     PriceString = "$0.99",  DisplayNameKey = "store.diamonds_50", DescriptionKey = "" }
        };
    }
}
