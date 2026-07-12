using System;
using System.Collections.Generic;
using System.Linq;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Registry providing pre-defined binding profiles.
    /// </summary>
    public static class NumpadProfileRegistry
    {
        /// <summary>
        /// Blind profile: contains all bindings defined in DefaultBindingTable.
        /// </summary>
        public static readonly NumpadProfile Blind = new NumpadProfile(
            NumpadProfileId.Blind,
            DefaultBindingTable.Bindings
        );

        /// <summary>
        /// Sighted profile: contains only base movement, interaction, hotbar slots,
        /// inventory, navigator menu, cancel navigation, and navigation status feedback.
        /// Excludes all other screen-reader specific readouts, Object Tracker controls,
        /// and TileViewer movements.
        /// </summary>
        public static readonly NumpadProfile Sighted = new NumpadProfile(
            NumpadProfileId.Sighted,
            GetSightedBindings()
        );

        private static IEnumerable<NumpadBinding> GetSightedBindings()
        {
            // Filtro inclusivo (whitelist) basato sull'analisi architetturale approvata
            return DefaultBindingTable.Bindings.Where(b =>
                b.ActionId == NumpadActionId.GridMoveUp ||
                b.ActionId == NumpadActionId.GridMoveDown ||
                b.ActionId == NumpadActionId.GridMoveLeft ||
                b.ActionId == NumpadActionId.GridMoveRight ||
                b.ActionId == NumpadActionId.UseTool ||
                b.ActionId == NumpadActionId.Interact ||
                b.ActionId == NumpadActionId.SlotPrevious ||
                b.ActionId == NumpadActionId.SlotNext ||
                b.ActionId == NumpadActionId.OpenInventory ||
                b.ActionId == NumpadActionId.OpenNavigatorMenu ||
                b.ActionId == NumpadActionId.CancelNavigation ||
                b.ActionId == NumpadActionId.ReadNavStatus
            );
        }

        /// <summary>
        /// Resolves a profile instance from its identifier.
        /// </summary>
        public static NumpadProfile GetProfile(NumpadProfileId id)
        {
            return id switch
            {
                NumpadProfileId.Blind => Blind,
                NumpadProfileId.Sighted => Sighted,
                _ => Blind
            };
        }

        /// <summary>
        /// Risolve l'azione per un dato chord unendo il profilo base con un dizionario di overrides.
        /// Questo metodo centralizza la risoluzione di runtime e la simulazione UI, garantendo parità.
        /// </summary>
        public static bool TryResolveAction(
            InputChord chord,
            NumpadProfile baseProfile,
            Dictionary<string, string>? overrides,
            out NumpadActionId actionId)
        {
            string chordString = chord.ToString();

            // 1. Risoluzione tramite override
            if (overrides != null && overrides.TryGetValue(chordString, out string? actionName))
            {
                if (actionName == "None")
                {
                    // Fallback di emergenza: se l'azione base di questo chord era critica
                    // e non è mappata da nessun altro tasto negli override, ignoriamo "None"
                    if (baseProfile.TryGetAction(chord, out var defaultAction) && IsCriticalAction(defaultAction) && !IsActionCoveredInOverrides(defaultAction, overrides))
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
            if (baseProfile.TryGetAction(chord, out actionId))
            {
                // Se l'azione di fabbrica è stata spostata altrove, questo chord originale non la esegue più (cardinalità 1:1)
                if (IsActionOverriddenElsewhere(actionId, chordString, overrides))
                {
                    actionId = NumpadActionId.None;
                    return true;
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Valida se un set di overrides simulato garantisce la copertura minima delle azioni critiche.
        /// </summary>
        public static bool ValidateOverrides(NumpadProfile baseProfile, Dictionary<string, string> overrides, out NumpadActionId unmappedCriticalAction)
        {
            unmappedCriticalAction = NumpadActionId.None;
            var criticalActions = new[] {
                NumpadActionId.GridMoveUp,
                NumpadActionId.GridMoveDown,
                NumpadActionId.GridMoveLeft,
                NumpadActionId.GridMoveRight,
                NumpadActionId.OpenNavigatorMenu
            };

            foreach (var act in criticalActions)
            {
                bool isCovered = false;
                foreach (var b in DefaultBindingTable.Bindings)
                {
                    if (TryResolveAction(b.Chord, baseProfile, overrides, out var resolvedAct) && resolvedAct == act)
                    {
                        isCovered = true;
                        break;
                    }
                }

                if (!isCovered)
                {
                    unmappedCriticalAction = act;
                    return false;
                }
            }
            return true;
        }

        private static bool IsCriticalAction(NumpadActionId actionId)
        {
            return actionId == NumpadActionId.GridMoveUp ||
                   actionId == NumpadActionId.GridMoveDown ||
                   actionId == NumpadActionId.GridMoveLeft ||
                   actionId == NumpadActionId.GridMoveRight ||
                   actionId == NumpadActionId.OpenNavigatorMenu;
        }

        private static bool IsTileViewerAction(NumpadActionId actionId)
        {
            return actionId == NumpadActionId.TileViewerMoveUp ||
                   actionId == NumpadActionId.TileViewerMoveDown ||
                   actionId == NumpadActionId.TileViewerMoveLeft ||
                   actionId == NumpadActionId.TileViewerMoveRight;
        }

        private static bool IsActionCoveredInOverrides(NumpadActionId actionId, Dictionary<string, string> overrides)
        {
            string actionName = actionId.ToString();
            foreach (var kvp in overrides)
            {
                if (kvp.Value == actionName)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool IsActionOverriddenElsewhere(NumpadActionId actionId, string currentChordString, Dictionary<string, string>? overrides)
        {
            if (IsTileViewerAction(actionId)) return false;
            if (overrides == null) return false;
            string actionName = actionId.ToString();
            foreach (var kvp in overrides)
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
