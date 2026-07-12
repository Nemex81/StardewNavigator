using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewValley;
using StardewValley.Menus;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Accessible two-level text-based menu for consulting and switching numpad profiles.
    /// Follows the same navigation and screen-reader announcement patterns as <see cref="NavigatorMenu"/>.
    /// </summary>
    public class NumpadConfigMenu : IClickableMenu
    {
        private enum MenuLevel { Level1, Level2 }

        private MenuLevel _currentLevel = MenuLevel.Level1;
        private int _categoryIndex = 0; // selected item in Level1
        private int _detailIndex = 0;   // selected item in Level2

        // Level 1 category identifiers
        private const int CatActive = 0;
        private const int CatInactive = 1;
        private const int CatGlobal = 2;
        private const int CatProfile = 3;
        private const int CategoryCount = 4;



        // Layout measurements (isomorphic to NavigatorMenu)
        private const int ItemHeight = 56;
        private const int MenuPadding = 32;
        private const int TitleHeight = 64;
        private const int MaxVisibleItems = 10;

        private int _lastMouseX = -1;
        private int _lastMouseY = -1;

        public NumpadConfigMenu()
            : base(
                x: Game1.uiViewport.Width / 2 - 350,
                y: Game1.uiViewport.Height / 2 - (MaxVisibleItems * ItemHeight / 2) - MenuPadding,
                width: 700,
                height: MaxVisibleItems * ItemHeight + MenuPadding * 2 + TitleHeight,
                showUpperRightCloseButton: true)
        {
            AnnounceCurrentView();
        }

        /// <summary>
        /// Handles keyboard and numpad shortcuts for navigation.
        /// </summary>
        public override void receiveKeyPress(Keys key)
        {
            switch (key)
            {
                case Keys.Up:
                case Keys.W:
                    MoveCursor(-1);
                    break;

                case Keys.Down:
                case Keys.S:
                    MoveCursor(1);
                    break;

                case Keys.Enter:
                    ConfirmSelection();
                    break;

                case Keys.Escape:
                    HandleEscape();
                    break;
            }
        }

        /// <summary>
        /// Support for controller D-Pad + A/B.
        /// </summary>
        public override void receiveGamePadButton(Buttons b)
        {
            switch (b)
            {
                case Buttons.DPadUp:
                    MoveCursor(-1);
                    break;
                case Buttons.DPadDown:
                    MoveCursor(1);
                    break;
                case Buttons.A:
                    ConfirmSelection();
                    break;
                case Buttons.B:
                    HandleEscape();
                    break;
            }
        }

        /// <summary>
        /// Custom text-based rendering identical in aesthetic to NavigatorMenu.
        /// </summary>
        public override void draw(SpriteBatch b)
        {
            // Menu background
            drawTextureBox(b, xPositionOnScreen, yPositionOnScreen, width, height, Color.White);

            // Title
            string title = GetTitleText();
            Utility.drawTextWithShadow(b, title,
                Game1.dialogueFont,
                new Vector2(xPositionOnScreen + MenuPadding, yPositionOnScreen + MenuPadding),
                Color.Black);

            // Current visible items
            List<string> items = GetCurrentItems();
            int startY = yPositionOnScreen + MenuPadding + TitleHeight;
            int currentIndex = _currentLevel == MenuLevel.Level1 ? _categoryIndex : _detailIndex;
            int scrollOffset = GetScrollOffset(currentIndex, items.Count);
            int visibleCount = Math.Min(items.Count, MaxVisibleItems);

            for (int i = 0; i < visibleCount; i++)
            {
                int idx = i + scrollOffset;
                if (idx >= items.Count) break;

                bool isSelected = idx == currentIndex;
                Color textColor = isSelected ? Color.DarkBlue : Color.Black;
                string prefix = isSelected ? "▶ " : "  ";

                Utility.drawTextWithShadow(b,
                    prefix + items[idx],
                    Game1.dialogueFont,
                    new Vector2(xPositionOnScreen + MenuPadding, startY + i * ItemHeight),
                    textColor);
            }

            // Scroll arrows
            if (items.Count > MaxVisibleItems)
            {
                if (scrollOffset > 0)
                    Utility.drawTextWithShadow(b, "▲", Game1.dialogueFont,
                        new Vector2(xPositionOnScreen + width - MenuPadding - 20, startY - 20), Color.Gray);
                if (scrollOffset + MaxVisibleItems < items.Count)
                    Utility.drawTextWithShadow(b, "▼", Game1.dialogueFont,
                        new Vector2(xPositionOnScreen + width - MenuPadding - 20, startY + visibleCount * ItemHeight), Color.Gray);
            }

            // Bottom-right indicator
            string levelIndicator = _currentLevel == MenuLevel.Level1
                ? ModEntry.Helper.Translation.Get("numpad-config-category-indicator").ToString()
                : ModEntry.Helper.Translation.Get("numpad-config-binding-indicator").ToString();
            Utility.drawTextWithShadow(b, levelIndicator, Game1.smallFont,
                new Vector2(xPositionOnScreen + width - MenuPadding - 100,
                            yPositionOnScreen + height - MenuPadding - 20),
                Color.Gray);

            base.draw(b);
            drawMouse(b);
        }

        /// <summary>
        /// Basic mouse hover/scroll navigation.
        /// </summary>
        public override void performHoverAction(int x, int y)
        {
            base.performHoverAction(x, y);

            if (x == _lastMouseX && y == _lastMouseY) return;
            _lastMouseX = x;
            _lastMouseY = y;

            int startY = yPositionOnScreen + MenuPadding + TitleHeight;
            List<string> items = GetCurrentItems();
            int currentIndex = _currentLevel == MenuLevel.Level1 ? _categoryIndex : _detailIndex;
            int scrollOffset = GetScrollOffset(currentIndex, items.Count);
            int visibleCount = Math.Min(items.Count, MaxVisibleItems);

            for (int i = 0; i < visibleCount; i++)
            {
                var rect = new Rectangle(
                    xPositionOnScreen + MenuPadding,
                    startY + i * ItemHeight,
                    width - MenuPadding * 2,
                    ItemHeight
                );

                if (rect.Contains(x, y))
                {
                    int targetIdx = i + scrollOffset;
                    if (targetIdx < items.Count && targetIdx != currentIndex)
                    {
                        if (_currentLevel == MenuLevel.Level1)
                        {
                            _categoryIndex = targetIdx;
                        }
                        else
                        {
                            _detailIndex = targetIdx;
                        }
                        AnnounceCurrentSelection();
                    }
                    break;
                }
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            int startY = yPositionOnScreen + MenuPadding + TitleHeight;
            List<string> items = GetCurrentItems();
            int currentIndex = _currentLevel == MenuLevel.Level1 ? _categoryIndex : _detailIndex;
            int scrollOffset = GetScrollOffset(currentIndex, items.Count);
            int visibleCount = Math.Min(items.Count, MaxVisibleItems);

            for (int i = 0; i < visibleCount; i++)
            {
                var rect = new Rectangle(
                    xPositionOnScreen + MenuPadding,
                    startY + i * ItemHeight,
                    width - MenuPadding * 2,
                    ItemHeight
                );

                if (rect.Contains(x, y))
                {
                    int targetIdx = i + scrollOffset;
                    if (targetIdx < items.Count)
                    {
                        if (_currentLevel == MenuLevel.Level1)
                            _categoryIndex = targetIdx;
                        else
                            _detailIndex = targetIdx;

                        ConfirmSelection();
                    }
                    break;
                }
            }
        }

        // ─── Internal Logic ───────────────────────────────────────────────────────

        private string GetTitleText()
        {
            if (_currentLevel == MenuLevel.Level1)
            {
                return ModEntry.Helper.Translation.Get("numpad-config-title").ToString();
            }

            return _categoryIndex switch
            {
                CatActive => ModEntry.Helper.Translation.Get("numpad-config-active-bindings").ToString(),
                CatInactive => ModEntry.Helper.Translation.Get("numpad-config-inactive-bindings").ToString(),
                CatGlobal => ModEntry.Helper.Translation.Get("numpad-config-global-bindings").ToString(),
                CatProfile => ModEntry.Helper.Translation.Get("numpad-config-change-profile").ToString(),
                _ => string.Empty
            };
        }

        private List<string> GetCurrentItems()
        {
            if (_currentLevel == MenuLevel.Level1)
            {
                return new List<string>
                {
                    ModEntry.Helper.Translation.Get("numpad-config-active-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-inactive-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-global-bindings").ToString(),
                    ModEntry.Helper.Translation.Get("numpad-config-change-profile").ToString()
                };
            }

            var currentProfile = NumpadProfileRegistry.GetProfile(ModEntry.Config.ActiveNumpadProfile);

            switch (_categoryIndex)
            {
                case CatActive:
                    return DefaultBindingTable.Bindings
                        .Where(b => currentProfile.TryGetAction(b.Chord, out var act) && act == b.ActionId)
                        .Select(b => $"{b.Chord} → {NumpadActionMetadata.GetDescription(b.ActionId)}")
                        .ToList();

                case CatInactive:
                    var inactive = DefaultBindingTable.Bindings
                        .Where(b => !currentProfile.TryGetAction(b.Chord, out var act) || act != b.ActionId)
                        .Select(b => $"{b.Chord} → {NumpadActionMetadata.GetDescription(b.ActionId)}")
                        .ToList();

                    if (inactive.Count == 0)
                        return new List<string> { ModEntry.Helper.Translation.Get("numpad-config-no-inactive").ToString() };

                    return inactive;

                case CatGlobal:
                    return NumpadActionMetadata.GlobalBindings
                        .Select(b => $"{b.Keys} → {b.Description}")
                        .ToList();

                case CatProfile:
                    string blindLabel = (ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Blind ? "[X] " : "[  ] ") + "Blind";
                    string sightedLabel = (ModEntry.Config.ActiveNumpadProfile == NumpadProfileId.Sighted ? "[X] " : "[  ] ") + "Sighted";
                    return new List<string> { blindLabel, sightedLabel };

                default:
                    return new List<string>();
            }
        }

        private void MoveCursor(int direction)
        {
            List<string> items = GetCurrentItems();
            if (items.Count == 0) return;

            if (_currentLevel == MenuLevel.Level1)
            {
                _categoryIndex = (_categoryIndex + direction + CategoryCount) % CategoryCount;
            }
            else
            {
                _detailIndex = (_detailIndex + direction + items.Count) % items.Count;
            }

            AnnounceCurrentSelection();
        }

        private void ConfirmSelection()
        {
            if (_currentLevel == MenuLevel.Level1)
            {
                _currentLevel = MenuLevel.Level2;
                _detailIndex = 0;
                AnnounceCurrentView();
            }
            else
            {
                if (_categoryIndex == CatProfile)
                {
                    // Switch profile
                    var newProfile = _detailIndex == 0 ? NumpadProfileId.Blind : NumpadProfileId.Sighted;
                    ModEntry.Config.ActiveNumpadProfile = newProfile;
                    ModEntry.Helper.WriteConfig(ModEntry.Config);

                    string profileName = newProfile.ToString();
                    string msg = ModEntry.Helper.Translation.Get("numpad-config-profile-changed", new { profile = profileName }).ToString();
                    NavigatorSpeaker.Say(msg, true);

                    // Return to Level 1
                    _currentLevel = MenuLevel.Level1;
                    _detailIndex = 0;
                    AnnounceCurrentView();
                }
            }
        }

        private void HandleEscape()
        {
            if (_currentLevel == MenuLevel.Level2)
            {
                _currentLevel = MenuLevel.Level1;
                _detailIndex = 0;
                AnnounceCurrentView();
            }
            else
            {
                exitThisMenu();
            }
        }

        private int GetScrollOffset(int index, int totalCount)
        {
            if (totalCount <= MaxVisibleItems) return 0;
            if (index < MaxVisibleItems / 2) return 0;
            if (index >= totalCount - MaxVisibleItems / 2) return totalCount - MaxVisibleItems;
            return index - MaxVisibleItems / 2;
        }

        // ─── Accessibility Vocalizations ──────────────────────────────────────────

        private void AnnounceCurrentView()
        {
            string viewName = GetTitleText();
            string profileName = ModEntry.Config.ActiveNumpadProfile.ToString();
            string profileIndicator = ModEntry.Helper.Translation.Get("numpad-config-current-profile", new { profile = profileName }).ToString();
            
            if (_currentLevel == MenuLevel.Level1)
            {
                string help = ModEntry.Helper.Translation.Get("numpad-config-choose-category").ToString();
                NavigatorSpeaker.Say($"{viewName}. {profileIndicator}. {help}", true);
            }
            else
            {
                List<string> items = GetCurrentItems();
                string countAnnouncement = $"{items.Count} items.";
                if (items.Count > 0)
                {
                    NavigatorSpeaker.Say($"{viewName}. {countAnnouncement}. {items[_detailIndex]}", true);
                }
                else
                {
                    NavigatorSpeaker.Say($"{viewName}. Empty.", true);
                }
            }
        }

        private void AnnounceCurrentSelection()
        {
            List<string> items = GetCurrentItems();
            int idx = _currentLevel == MenuLevel.Level1 ? _categoryIndex : _detailIndex;
            if (idx >= 0 && idx < items.Count)
            {
                NavigatorSpeaker.Say(items[idx], true);
            }
        }
    }
}
