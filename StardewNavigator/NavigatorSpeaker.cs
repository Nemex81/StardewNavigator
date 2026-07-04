using System;
using System.Linq;
using StardewValley;

namespace StardewNavigator
{
    public static class NavigatorSpeaker
    {
        private static Type? mainClassType = null;
        private static object? screenReaderInstance = null;
        private static System.Reflection.MethodInfo? sayMethod = null;
        private static bool reflectionAttempted = false;

        private static void InitReflection()
        {
            if (reflectionAttempted) return;
            reflectionAttempted = true;
            try
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "stardew-access");
                if (assembly == null) return;

                mainClassType = assembly.GetType("stardew_access.MainClass");
                if (mainClassType == null) return;

                var screenReaderProp = mainClassType.GetProperty("ScreenReader", 
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (screenReaderProp == null) return;

                screenReaderInstance = screenReaderProp.GetValue(null);
                if (screenReaderInstance == null) return;

                sayMethod = screenReaderInstance.GetType().GetMethod("Say", 
                    new[] { typeof(string), typeof(bool) });
            }
            catch (Exception ex)
            {
                ModEntry.Monitor.Log($"Errore di riflessione per stardew-access: {ex}", StardewModdingAPI.LogLevel.Warn);
            }
        }

        public static void Say(string text, bool interrupt = false)
        {
            bool isStardewAccessLoaded = ModEntry.Helper.ModRegistry.IsLoaded("shoaib.stardewaccess") 
                                         || ModEntry.Helper.ModRegistry.IsLoaded("stardew.access");

            if (isStardewAccessLoaded)
            {
                InitReflection();
                if (sayMethod != null && screenReaderInstance != null)
                {
                    try
                    {
                        sayMethod.Invoke(screenReaderInstance, new object[] { text, interrupt });
                        return;
                    }
                    catch (Exception ex)
                    {
                        ModEntry.Monitor.Log($"Impossibile chiamare ScreenReader.Say: {ex.Message}", StardewModdingAPI.LogLevel.Warn);
                    }
                }
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
