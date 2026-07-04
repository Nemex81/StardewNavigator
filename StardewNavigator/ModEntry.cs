using System;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewNavigator.Features.Navigator;

namespace StardewNavigator
{
    public class ModEntry : Mod
    {
        public static ModEntry Instance { get; private set; } = null!;
        public static new IModHelper Helper => ((Mod)Instance).Helper;
        public static new IMonitor Monitor => ((Mod)Instance).Monitor;
        public static ModConfig Config { get; private set; } = null!;

        private NavigatorFeature? _navigatorFeature;

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            Config = helper.ReadConfig<ModConfig>();

            // Inizializza la feature del navigatore
            _navigatorFeature = new NavigatorFeature(helper);

            helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            var configMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (configMenu != null)
            {
                configMenu.Register(
                    mod: ModManifest,
                    reset: () => {
                        Config = new ModConfig();
                        Helper.WriteConfig(Config);
                    },
                    save: () => Helper.WriteConfig(Config)
                );

                configMenu.AddKeybindList(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("config.navigator-menu-key.name"),
                    tooltip: () => Helper.Translation.Get("config.navigator-menu-key.tooltip"),
                    getValue: () => Config.NavigatorMenuKey,
                    setValue: value => Config.NavigatorMenuKey = value
                );

                configMenu.AddNumberOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("config.hud-duration.name"),
                    tooltip: () => Helper.Translation.Get("config.hud-duration.tooltip"),
                    getValue: () => Config.HudMessageDuration,
                    setValue: value => Config.HudMessageDuration = value,
                    min: 1.0f,
                    max: 10.0f,
                    interval: 0.5f
                );
            }
        }
    }

    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatVal = null, string? fieldId = null);
    }
}
