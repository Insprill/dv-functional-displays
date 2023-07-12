using System;
using BepInEx.Configuration;

/// <remarks>
///     You can read more and see examples in the readme at https://github.com/BepInEx/BepInEx.ConfigurationManager
/// </remarks>
#pragma warning disable 0169, 0414, 0649
internal sealed class ConfigurationManagerAttributes
{
    public Action<ConfigEntryBase> CustomDrawer;
}
