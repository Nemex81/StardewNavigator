using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Coordinatore logico della sessione del menu di configurazione Numpad.
    /// Incapsula lo stato di navigazione, i cursori, i filtri di visualizzazione e i side effect,
    /// lasciando la geometria e il rendering a <see cref="NumpadConfigMenu"/>.
    /// </summary>
    internal class NumpadConfigSession
    {
        public enum MenuLevel { Level1, Level2, Level3 }

        public MenuLevel CurrentLevel { get; private set; } = MenuLevel.Level1;

        private int _categoryIndex = 0;
        private int _detailIndex = 0;
        private int _actionIndex = 0;

        private const int CatActive = 0;
        private const int CatInactive = 1;
        private const int CatGlobal = 2;
        private const int CatProfile = 3;
        private const int CatReset = 4;
        private const int CategoryCount = 5;

        private readonly List<NumpadBinding> _visibleBindings = new();
        private NumpadBinding _selectedBinding;

        private static readonly NumpadActionId[] AssignableActions = {
            NumpadActionId.GridMoveUp, NumpadActionId.GridMoveDown, NumpadActionId.GridMoveLeft, NumpadActionId.GridMoveRight,
            NumpadActionId.UseTool, NumpadActionId.Interact,
            NumpadActionId.ReadTileFacing, NumpadActionId.ReadTileStanding, NumpadActionId.ReadCoords, NumpadActionId.ReadHealthStamina, NumpadActionId.ReadCurrentItem, NumpadActionId.ReadNavStatus,
            NumpadActionId.SlotPrevious, NumpadActionId.SlotNext,
            NumpadActionId.OpenInventory, NumpadActionId.OpenNavigatorMenu,
            NumpadActionId.CancelNavigation, NumpadActionId.AutoWalkToObject,
            NumpadActionId.ScannerObjectGroupUp, NumpadActionId.ScannerObjectGroupDown,
            NumpadActionId.ScannerCategoryUp, NumpadActionId.ScannerCategoryDown,
            NumpadActionId.ScannerInGroupUp, NumpadActionId.ScannerInGroupDown,
            NumpadActionId.TileViewerMoveUp, NumpadActionId.TileViewerMoveDown, NumpadActionId.TileViewerMoveLeft, NumpadActionId.TileViewerMoveRight
        };

        public NumpadConfigSession()
        {
            AnnounceCurrentView();
        }

        public int GetCurrentIndex()
        {
            return CurrentLevel switch
            {
                MenuLevel.Level1 => _categoryIndex,
                MenuLevel.Level2 => _detailIndex,
                MenuLevel.Level3 => _actionIndex,
                _ => 0
            };
        }

        public void SetCurrentIndex(int index)
        {
            switch (CurrentLevel)
            {
                case MenuLevel.Level1:
                    _categoryIndex = index;
                    break;
                case MenuLevel.Level2:
                    _detailIndex = index;
                    break;
                case MenuLevel.Level3:
                    _actionIndex = index;
                    break;
            }
            AnnounceCurrentSelection();
        }

        public string GetTitleText()
        {
            if (CurrentLevel == MenuLevel.Level1)
            {
                string baseTitle = ModEntry.Helper.Translation.Get("numpad-config-title").ToString();
                string profileKey = ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Blind 
                    ? "numpad-config-profile-blind" 
                    : "numpad-config-profile-sighted";
                string profileName = ModEntry.Helper.Translation.Get(profileKey).ToString();
                return $"{baseTitle} ({profileName})";
            }

            if (CurrentLevel == MenuLevel.Level2)
            {
                return _categoryIndex switch
                {
                    CatActive => ModEntry.Helper.Translation.Get("numpad-config-active-bindings").ToString(),
                    CatInactive => ModEntry.Helper.Translation.Get("numpad-config-inactive-bindings").ToString(),
                    CatGlobal => ModEntry.Helper.Translation.Get("numpad-config-global-bindings").ToString(),
                    CatProfile => ModEntry.Helper.Translation.Get("numpad-config-change-profile").ToString(),
                    _ => string.Empty
                };
            }

            return $"{_selectedBinding.Chord}";
        }

        public List<string> GetCurrentItems()
        {
            if (CurrentLevel == MenuLevel.Level1)
            {
                return new List<string>
                {
                    ModEntry.Helper.Translation.Get("numpad-config-active-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-inactive-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-global-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-change-profile").ToString(),
                    "* " + ModEntry.Helper.Translation.Get("numpad-config-restore-label").ToString() + " *"
                };
            }

            var currentProfile = NumpadProfileRegistry.GetProfile(ModEntry.Config.ActiveNumpadProfile);

            if (CurrentLevel == MenuLevel.Level2)
            {
                switch (_categoryIndex)
                {
                    case CatActive:
                        _visibleBindings.Clear();
                        foreach (var b in DefaultBindingTable.Bindings)
                        {
                            if (NumpadProfileRegistry.TryResolveAction(b.Chord, currentProfile, ModEntry.Config.NumpadOverrides, out var act) && act == b.ActionId)
                            {
                                _visibleBindings.Add(b);
                            }
                        }
                        if (ModEntry.Config.NumpadOverrides != null)
                        {
                            foreach (var ov in ModEntry.Config.NumpadOverrides)
                            {
                                if (ov.Value != "None" && Enum.TryParse<NumpadActionId>(ov.Value, out var actId))
                                {
                                    var chord = InputChord.TryParse(ov.Key);
                                    if (chord.HasValue && !_visibleBindings.Any(b => b.Chord == chord.Value))
                                    {
                                        _visibleBindings.Add(new NumpadBinding(chord.Value, actId));
                                    }
                                }
                            }
                        }
                        return _visibleBindings
                            .Select(b => $"{b.Chord} → {NumpadActionMetadata.GetDescription(GetRuntimeAction(b.Chord, currentProfile))}")
                            .ToList();

                    case CatInactive:
                        _visibleBindings.Clear();
                        foreach (var b in DefaultBindingTable.Bindings)
                        {
                            var currentAct = GetRuntimeAction(b.Chord, currentProfile);
                            if (currentAct == NumpadActionId.None)
                            {
                                _visibleBindings.Add(b);
                            }
                        }
                        if (_visibleBindings.Count == 0)
                            return new List<string> { ModEntry.Helper.Translation.Get("numpad-config-no-inactive").ToString() };
                        return _visibleBindings
                            .Select(b => $"{b.Chord} → ({NumpadActionMetadata.GetDescription(b.ActionId)})")
                            .ToList();

                    case CatGlobal:
                        return NumpadActionMetadata.GlobalBindings
                            .Select(b => $"{b.Keys} → {b.Description}")
                            .ToList();

                    case CatProfile:
                        string blindLabel = (ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Blind ? "[X] " : "[  ] ") + ModEntry.Helper.Translation.Get("numpad-config-profile-blind").ToString();
                        string sightedLabel = (ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Sighted ? "[X] " : "[  ] ") + ModEntry.Helper.Translation.Get("numpad-config-profile-sighted").ToString();
                        return new List<string> { blindLabel, sightedLabel };

                    default:
                        return new List<string>();
                }
            }

            var list = new List<string>(AssignableActions.Length + 2);
            var currentAssigned = GetRuntimeAction(_selectedBinding.Chord, currentProfile);

            foreach (var act in AssignableActions)
            {
                string activeMarker = (currentAssigned == act) ? "[X] " : "[  ] ";
                list.Add(activeMarker + NumpadActionMetadata.GetDescription(act));
            }

            string noneMarker = (currentAssigned == NumpadActionId.None) ? "[X] " : "[  ] ";
            list.Add(noneMarker + "[" + ModEntry.Helper.Translation.Get("numpad-config-disable-label").ToString() + "]");
            
            bool isOverridden = ModEntry.Config.NumpadOverrides.ContainsKey(_selectedBinding.Chord.ToString());
            string defaultMarker = isOverridden ? "    " : "[X] ";
            list.Add(defaultMarker + "[" + ModEntry.Helper.Translation.Get("numpad-config-restore-label").ToString() + "]");

            return list;
        }

        private NumpadActionId GetRuntimeAction(InputChord chord, NumpadProfile baseProfile)
        {
            if (NumpadProfileRegistry.TryResolveAction(chord, baseProfile, ModEntry.Config.NumpadOverrides, out var act))
            {
                return act;
            }
            return NumpadActionId.None;
        }

        public void MoveCursor(int direction)
        {
            List<string> items = GetCurrentItems();
            if (items.Count == 0) return;

            switch (CurrentLevel)
            {
                case MenuLevel.Level1:
                    _categoryIndex = (_categoryIndex + direction + CategoryCount) % CategoryCount;
                    break;
                case MenuLevel.Level2:
                    _detailIndex = (_detailIndex + direction + items.Count) % items.Count;
                    break;
                case MenuLevel.Level3:
                    _actionIndex = (_actionIndex + direction + items.Count) % items.Count;
                    break;
            }

            AnnounceCurrentSelection();
        }

        public void ConfirmSelection()
        {
            if (CurrentLevel == MenuLevel.Level1)
            {
                if (_categoryIndex == CatReset)
                {
                    ModEntry.Config.NumpadOverrides.Clear();
                    ModEntry.Helper.WriteConfig(ModEntry.Config);
                    string msg = ModEntry.Helper.Translation.Get("numpad-info-reset-complete").ToString();
                    NavigatorSpeaker.Say(msg, true);
                    return;
                }

                CurrentLevel = MenuLevel.Level2;
                _detailIndex = 0;
                AnnounceCurrentView();
            }
            else if (CurrentLevel == MenuLevel.Level2)
            {
                if (_categoryIndex == CatProfile)
                {
                    var newProfile = _detailIndex == 0 ? NumpadProfileId.Blind : NumpadProfileId.Sighted;
                    ModEntry.Config.ActiveNumpadProfile = newProfile;
                    ModEntry.Helper.WriteConfig(ModEntry.Config);

                    string profileName = newProfile.ToString();
                    string msg = ModEntry.Helper.Translation.Get("numpad-config-profile-changed", new { profile = profileName }).ToString();
                    NavigatorSpeaker.Say(msg, true);

                    CurrentLevel = MenuLevel.Level1;
                    _detailIndex = 0;
                    AnnounceCurrentView();
                }
                else if (_categoryIndex == CatActive || _categoryIndex == CatInactive)
                {
                    if (_visibleBindings.Count > 0 && _detailIndex < _visibleBindings.Count)
                    {
                        _selectedBinding = _visibleBindings[_detailIndex];
                        CurrentLevel = MenuLevel.Level3;
                        _actionIndex = 0;
                        AnnounceCurrentView();
                    }
                }
            }
            else
            {
                int totalActions = AssignableActions.Length;
                string targetActionName = "None";

                if (_actionIndex < totalActions)
                {
                    targetActionName = AssignableActions[_actionIndex].ToString();
                }
                else if (_actionIndex == totalActions)
                {
                    targetActionName = "None";
                }
                else if (_actionIndex == totalActions + 1)
                {
                    targetActionName = "Default";
                }

                if (ValidateAndApplyOverride(_selectedBinding.Chord, targetActionName))
                {
                    string actionNameLocalized = targetActionName == "None" 
                        ? ModEntry.Helper.Translation.Get("numpad-config-disable-label").ToString()
                        : targetActionName == "Default" 
                            ? ModEntry.Helper.Translation.Get("numpad-config-restore-label").ToString()
                            : NumpadActionMetadata.GetDescription(AssignableActions[_actionIndex]);

                    string keyLabel = _selectedBinding.Chord.ToString();
                    string infoMsg = ModEntry.Helper.Translation.Get("numpad-info-binding-moved", new { action = actionNameLocalized, newKey = keyLabel }).ToString();
                    NavigatorSpeaker.Say(infoMsg, true);

                    CurrentLevel = MenuLevel.Level2;
                    _detailIndex = 0;
                    AnnounceCurrentView();
                }
            }
        }

        /// <summary>
        /// Ritorna true se il menu padre (IClickableMenu) deve essere chiuso (exitThisMenu).
        /// </summary>
        public bool GoBack()
        {
            if (CurrentLevel == MenuLevel.Level3)
            {
                CurrentLevel = MenuLevel.Level2;
                _actionIndex = 0;
                AnnounceCurrentView();
                return false;
            }
            if (CurrentLevel == MenuLevel.Level2)
            {
                CurrentLevel = MenuLevel.Level1;
                _detailIndex = 0;
                AnnounceCurrentView();
                return false;
            }
            
            return true;
        }

        private bool ValidateAndApplyOverride(InputChord chord, string targetActionName)
        {
            string chordString = chord.ToString();
            
            var tempOverrides = new Dictionary<string, string>(ModEntry.Config.NumpadOverrides);
            
            if (targetActionName != "None" && targetActionName != "Default")
            {
                var keysToRemove = tempOverrides.Where(kvp => kvp.Value == targetActionName && kvp.Key != chordString).Select(kvp => kvp.Key).ToList();
                foreach (var key in keysToRemove)
                {
                    tempOverrides[key] = "None";
                }
            }

            if (targetActionName == "Default")
            {
                tempOverrides.Remove(chordString);
            }
            else
            {
                tempOverrides[chordString] = targetActionName;
            }

            var baseProfile = NumpadProfileRegistry.GetProfile(ModEntry.Config.ActiveNumpadProfile);
            if (!NumpadProfileRegistry.ValidateOverrides(baseProfile, tempOverrides, out var unmappedCriticalAction))
            {
                string actionLabel = NumpadActionMetadata.GetDescription(unmappedCriticalAction);
                string errMsg = ModEntry.Helper.Translation.Get("numpad-err-critical-unmapped", new { action = actionLabel }).ToString();
                NavigatorSpeaker.Say(errMsg, true);
                return false;
            }

            ModEntry.Config.NumpadOverrides = tempOverrides;
            ModEntry.Helper.WriteConfig(ModEntry.Config);
            return true;
        }

        private void AnnounceCurrentView()
        {
            string viewName = GetTitleText();
            string profileKey = ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Blind 
                ? "numpad-config-profile-blind" 
                : "numpad-config-profile-sighted";
            string profileName = ModEntry.Helper.Translation.Get(profileKey).ToString();
            string profileIndicator = ModEntry.Helper.Translation.Get("numpad-config-current-profile", new { profile = profileName }).ToString();
            
            if (CurrentLevel == MenuLevel.Level1)
            {
                string help = ModEntry.Helper.Translation.Get("numpad-config-choose-category").ToString();
                NavigatorSpeaker.Say($"{viewName}. {profileIndicator}. {help}", true);
            }
            else
            {
                List<string> items = GetCurrentItems();
                string countAnnouncement = ModEntry.Helper.Translation.Get("numpad-config-item-count", new { count = items.Count }).ToString();
                int idx = GetCurrentIndex();

                if (items.Count > 0 && idx >= 0 && idx < items.Count)
                {
                    NavigatorSpeaker.Say($"{viewName}. {countAnnouncement}. {items[idx]}", true);
                }
                else
                {
                    NavigatorSpeaker.Say($"{viewName}. " + ModEntry.Helper.Translation.Get("numpad-config-empty").ToString(), true);
                }
            }
        }

        private void AnnounceCurrentSelection()
        {
            List<string> items = GetCurrentItems();
            int idx = GetCurrentIndex();

            if (idx >= 0 && idx < items.Count)
            {
                NavigatorSpeaker.Say(items[idx], true);
            }
        }
    }
}
