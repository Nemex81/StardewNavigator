using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;

namespace StardewNavigator.Features.Navigator
{
    /// <summary>
    /// Accessible three-level text-based menu for consulting, switching profiles, and mapping numpad bindings.
    /// Acts strictly as the View/Input layer, delegating state and logic to <see cref="NumpadConfigSession"/>.
    /// </summary>
    public class NumpadConfigMenu : IClickableMenu
    {
        private readonly NumpadConfigSession _session;

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
            _session = new NumpadConfigSession();
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
                    _session.MoveCursor(-1);
                    break;

                case Keys.Down:
                case Keys.S:
                    _session.MoveCursor(1);
                    break;

                case Keys.Enter:
                    _session.ConfirmSelection();
                    break;

                case Keys.Escape:
                    if (_session.GoBack())
                        exitThisMenu();
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
                    _session.MoveCursor(-1);
                    break;
                case Buttons.DPadDown:
                    _session.MoveCursor(1);
                    break;
                case Buttons.A:
                    _session.ConfirmSelection();
                    break;
                case Buttons.B:
                    if (_session.GoBack())
                        exitThisMenu();
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
            string title = _session.GetTitleText();
            Utility.drawTextWithShadow(b, title,
                Game1.dialogueFont,
                new Vector2(xPositionOnScreen + MenuPadding, yPositionOnScreen + MenuPadding),
                Color.Black);

            // Current visible items
            List<string> items = _session.GetCurrentItems();
            int startY = yPositionOnScreen + MenuPadding + TitleHeight;
            
            int currentIndex = _session.GetCurrentIndex();
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
            string levelIndicator = _session.CurrentLevel switch
            {
                NumpadConfigSession.MenuLevel.Level1 => ModEntry.Helper.Translation.Get("numpad-config-category-indicator").ToString(),
                NumpadConfigSession.MenuLevel.Level2 => ModEntry.Helper.Translation.Get("numpad-config-binding-indicator").ToString(),
                NumpadConfigSession.MenuLevel.Level3 => "[Edit]",
                _ => string.Empty
            };
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
            List<string> items = _session.GetCurrentItems();
            
            int currentIndex = _session.GetCurrentIndex();
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
                        _session.SetCurrentIndex(targetIdx);
                    }
                    break;
                }
            }
        }

        public override void receiveLeftClick(int x, int y, bool playSound = true)
        {
            base.receiveLeftClick(x, y, playSound);

            int startY = yPositionOnScreen + MenuPadding + TitleHeight;
            List<string> items = _session.GetCurrentItems();
            
            int currentIndex = _session.GetCurrentIndex();
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
                        _session.SetCurrentIndex(targetIdx);
                        _session.ConfirmSelection();
                    }
                    break;
                }
            }
        }

        private int GetScrollOffset(int index, int totalCount)
        {
            if (totalCount <= MaxVisibleItems) return 0;
            if (index < MaxVisibleItems / 2) return 0;
            if (index >= totalCount - MaxVisibleItems / 2) return totalCount - MaxVisibleItems;
            return index - MaxVisibleItems / 2;
        }
    }
}
