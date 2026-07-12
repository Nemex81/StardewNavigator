using System.Collections.Generic;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Represents a specific set of physical-to-logical bindings for the numpad.
    /// Parametrises chord lookup for different user profiles (Blind, Sighted).
    /// </summary>
    public class NumpadProfile
    {
        public NumpadProfileId Id { get; }
        
        private readonly Dictionary<InputChord, NumpadActionId> _lookup;

        public NumpadProfile(NumpadProfileId id, IEnumerable<NumpadBinding> bindings)
        {
            Id = id;
            _lookup = new Dictionary<InputChord, NumpadActionId>();
            foreach (var b in bindings)
            {
                _lookup[b.Chord] = b.ActionId;
            }
        }

        public bool TryGetAction(InputChord chord, out NumpadActionId actionId)
        {
            string chordString = chord.ToString();
            
            // 1. Risoluzione tramite override utente
            if (ModEntry.Config.NumpadOverrides != null && ModEntry.Config.NumpadOverrides.TryGetValue(chordString, out string? actionName))
            {
                if (actionName == "None")
                {
                    // Fallback di emergenza: se l'azione base di questo chord era critica
                    // e non è mappata da nessun altro tasto negli override, ignoriamo "None"
                    if (_lookup.TryGetValue(chord, out var defaultAction) && IsCriticalAction(defaultAction) && !IsActionCoveredInOverrides(defaultAction))
                    {
                        actionId = defaultAction;
                        return true;
                    }

                    actionId = NumpadActionId.None;
                    return true;
                }

                if (Enum.TryParse<NumpadActionId>(actionName, out var parsedAction))
                {
                    actionId = parsedAction;
                    return true;
                }
            }

            // 2. Fallback sul profilo base con swap implicito
            if (_lookup.TryGetValue(chord, out actionId))
            {
                // Se l'azione di fabbrica è stata spostata altrove, questo chord originale non la esegue più (cardinalità 1:1)
                if (IsActionOverriddenElsewhere(actionId, chordString))
                {
                    actionId = NumpadActionId.None;
                    return true;
                }
                return true;
            }

            return false;
        }

        private bool IsCriticalAction(NumpadActionId actionId)
        {
            return actionId == NumpadActionId.GridMoveUp ||
                   actionId == NumpadActionId.GridMoveDown ||
                   actionId == NumpadActionId.GridMoveLeft ||
                   actionId == NumpadActionId.GridMoveRight ||
                   actionId == NumpadActionId.OpenNavigatorMenu;
        }

        private bool IsTileViewerAction(NumpadActionId actionId)
        {
            return actionId == NumpadActionId.TileViewerMoveUp ||
                   actionId == NumpadActionId.TileViewerMoveDown ||
                   actionId == NumpadActionId.TileViewerMoveLeft ||
                   actionId == NumpadActionId.TileViewerMoveRight;
        }

        private bool IsActionCoveredInOverrides(NumpadActionId actionId)
        {
            if (ModEntry.Config.NumpadOverrides == null) return false;
            string actionName = actionId.ToString();
            foreach (var kvp in ModEntry.Config.NumpadOverrides)
            {
                if (kvp.Value == actionName)
                {
                    return true;
                }
            }
            return false;
        }

        private bool IsActionOverriddenElsewhere(NumpadActionId actionId, string currentChordString)
        {
            if (IsTileViewerAction(actionId)) return false;
            if (ModEntry.Config.NumpadOverrides == null) return false;
            string actionName = actionId.ToString();
            foreach (var kvp in ModEntry.Config.NumpadOverrides)
            {
                if (kvp.Key != currentChordString && kvp.Value == actionName)
                {
                    return true;
                }
            }
            return false;
        }
    }
}
