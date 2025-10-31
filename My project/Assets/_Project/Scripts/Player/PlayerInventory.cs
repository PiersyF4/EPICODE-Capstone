using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    // itemId -> quantità
    private readonly Dictionary<string, int> items = new Dictionary<string, int>();

    // callback (per UI, se vuoi)
    public event Action<string, int> OnItemChanged;

    public void AddItem(string itemId, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || amount == 0) return;

        if (!items.ContainsKey(itemId))
            items[itemId] = 0;

        items[itemId] += amount;
        OnItemChanged?.Invoke(itemId, items[itemId]);
        // Debug.Log($"[INV] {itemId} = {items[itemId]}");
    }

    public int GetCount(string itemId) => items.TryGetValue(itemId, out var n) ? n : 0;
}