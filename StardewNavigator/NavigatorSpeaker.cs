using StardewNavigator.Integration;
using StardewValley;

namespace StardewNavigator
{
    public static class NavigatorSpeaker
    {
        public static void Say(string text, bool interrupt = false)
        {
            if (StardewAccessBridge.TrySpeak(text, interrupt))
            {
                return;
            }

            if (interrupt)
            {
                Game1.hudMessages.Clear();
            }

            var msg = new HUDMessage(text, HUDMessage.newQuest_type);
            msg.timeLeft = ModEntry.Config.HudMessageDuration * 1000f; // millisecondi
            Game1.addHUDMessage(msg);
        }
    }
}
