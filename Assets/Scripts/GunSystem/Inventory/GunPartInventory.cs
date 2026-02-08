using System;
using System.Collections.Generic;
using UnityEngine;

public class GunPartInventory : MonoBehaviour
{
    [Header("Inventory")]
    [SerializeField] private bool allowDuplicates = true;
    [SerializeField] private List<GunPart> parts = new();

    public IReadOnlyList<GunPart> Parts => parts;

    public event Action<GunPart> PartAdded;
    public event Action<GunPart> PartRemoved;

    public bool AddPart(GunPart part)
    {
        if (part == null) return false;

        if (!allowDuplicates && parts.Contains(part))
        {
            return false;
        }

        parts.Add(part);
        PartAdded?.Invoke(part);
        return true;
    }

    public bool RemovePart(GunPart part)
    {
        if (part == null) return false;

        bool removed = parts.Remove(part);
        if (removed)
        {
            PartRemoved?.Invoke(part);
        }

        return removed;
    }

    public bool HasPart(GunPart part)
    {
        if (part == null) return false;
        return parts.Contains(part);
    }

    public List<GunPart> GetPartsByType(GunPartType type)
    {
        List<GunPart> result = new();
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && parts[i].partType == type)
            {
                result.Add(parts[i]);
            }
        }
        return result;
    }

    public GunPart GetFirstPartByType(GunPartType type)
    {
        for (int i = 0; i < parts.Count; i++)
        {
            if (parts[i] != null && parts[i].partType == type)
            {
                return parts[i];
            }
        }
        return null;
    }
}
