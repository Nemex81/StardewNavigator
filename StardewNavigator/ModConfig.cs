using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace StardewNavigator
{
    public class ModConfig
    {
        // Tasto per aprire il menu Navigator (default: G)
        public KeybindList NavigatorMenuKey { get; set; } = new(SButton.G);

        // Durata in secondi dei messaggi HUD per giocatori normovedenti (default: 4.0)
        // Range consigliato: 1.0 - 10.0
        public float HudMessageDuration { get; set; } = 4.0f;

        // Controlla la presenza di aggiornamenti all'avvio
        public bool CheckForUpdatesOnStartup { get; set; } = true;

        // Abilita i controlli tramite tastierino numerico (Numpad)
        public bool NumpadControlsActive { get; set; } = true;
    }
}
