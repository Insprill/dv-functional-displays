using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CommandTerminal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FunctionalDisplays.Items;

public abstract class DisplayableItem
{
    public static readonly IReadOnlyDictionary<string, DisplayableItem> DisplayItems = Assembly.GetAssembly(typeof(DisplayableItem))
        .GetTypes()
        .Where(t => t.IsSubclassOf(typeof(DisplayableItem)) && t != typeof(DisplayableItem))
        .Select(t =>
        {
            ConstructorInfo constructor = t.GetConstructor(Type.EmptyTypes);
            if (constructor != null) return (DisplayableItem)constructor.Invoke(null);
            FunctionalDisplays.Instance.Logger.LogError($"Could not find a parameterless constructor for type {t.FullName}");
            return null;
        })
        .Where(i => i != null)
        .ToDictionary(i => i.PrefabName, i => i);

    protected abstract string PrefabName { get; }

    public abstract void Setup(Transform transform);

    protected DisplayableItem()
    {
        RegisterCommand();
    }

    private void RegisterCommand()
    {
        Terminal.Shell.AddCommand(PrefabName.ToLower(), OnCommand, 0, 0, $"Summons a {PrefabName}");
    }

    private void OnCommand(CommandArg[] args)
    {
        if (!WorldStreamingInit.IsLoaded)
        {
            Debug.LogError($"You can't summon a {PrefabName} before the world is loaded!");
            return;
        }

        GameObject laptop = Object.Instantiate(Resources.Load<GameObject>(PrefabName), WorldMover.Instance.originShiftParent);
        laptop.transform.position = PlayerManager.PlayerTransform.position + PlayerManager.PlayerTransform.forward * 1f;
    }
}
