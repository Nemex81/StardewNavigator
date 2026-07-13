using System;
using System.Collections.Generic;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Metadata helper providing localized human-readable names and descriptions for logical numpad actions.
    /// Acts as the single source of truth for action descriptions in the configuration UI.
    /// </summary>
    internal static class NumpadActionMetadata
    {
        /// <summary>
        /// Retrieves the localized description for a logical action ID.
        /// </summary>
        public static string GetDescription(NumpadActionId actionId)
        {
            string translationKey = $"numpad-action-desc-{actionId}";
            var translation = ModEntry.Helper.Translation.Get(translationKey);
            if (translation.HasValue())
            {
                return translation.ToString();
            }
            return actionId.ToString();
        }

        /// <summary>
        /// Represents a static metadata mapping of global/non-profiled shortcuts to expose in the UI.
        /// </summary>
        public static readonly IReadOnlyList<GlobalBindingInfo> GlobalBindings = new List<GlobalBindingInfo>
        {
            new("Decimal", "AliasEnter", ModEntry.Helper.Translation.Get("numpad-config-global-desc-decimal").ToString()),
            new("Ctrl + NumPad8/2/4/6", "MicroMove", ModEntry.Helper.Translation.Get("numpad-config-global-desc-micromove").ToString()),
            new("NumPad8/2 (in Menu)", "NavMenuCursorUp/Down", ModEntry.Helper.Translation.Get("numpad-config-global-desc-navmenu-cursor").ToString()),
            new("Ctrl + NumPad5 (in Menu)", "NavMenuConfirm", ModEntry.Helper.Translation.Get("numpad-config-global-desc-navmenu-confirm").ToString()),
            new("Ctrl + NumPad5 (other Menu)", "AliasCtrlEnter", ModEntry.Helper.Translation.Get("numpad-config-global-desc-ctrl-enter").ToString())
        };
    }

    /// <summary>
    /// Container for global non-configurable binding display metadata.
    /// </summary>
    internal sealed class GlobalBindingInfo
    {
        public string Keys { get; }
        public string ActionName { get; }
        public string Description { get; }

        public GlobalBindingInfo(string keys, string actionName, string description)
        {
            Keys = keys;
            ActionName = actionName;
            Description = description;
        }

        public override string ToString() => $"{Keys} → {Description}";
    }
}
