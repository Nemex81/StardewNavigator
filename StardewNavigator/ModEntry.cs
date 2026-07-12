using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
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
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            if (Config.CheckForUpdatesOnStartup)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        using (var client = new HttpClient())
                        {
                            client.DefaultRequestHeaders.Add("User-Agent", "StardewNavigator");
                            var response = await client.GetStringAsync("https://api.github.com/repos/Nemex81/StardewNavigator/releases/latest");
                            
                            using (var doc = JsonDocument.Parse(response))
                            {
                                if (doc.RootElement.TryGetProperty("tag_name", out var tagProp))
                                {
                                    string latestTag = tagProp.GetString() ?? string.Empty;
                                    string cleanTag = latestTag.TrimStart('v');
                                    var latestVersion = new Version(cleanTag);
                                    
                                    var currentVer = ModManifest.Version;
                                    var currentVersion = new Version(currentVer.MajorVersion, currentVer.MinorVersion, currentVer.PatchVersion);

                                    if (latestVersion > currentVersion)
                                    {
                                        // Delay per attendere il caricamento completo del mondo
                                        await Task.Delay(4000);
                                        
                                        string message = Helper.Translation.Get("update-available", new { version = latestTag }).ToString();
                                        NavigatorSpeaker.Say(message, interrupt: false);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Monitor.Log($"Errore nel controllo aggiornamenti: {ex.Message}", LogLevel.Debug);
                    }
                });
            }
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

                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("config.check-updates.name"),
                    tooltip: () => Helper.Translation.Get("config.check-updates.tooltip"),
                    getValue: () => Config.CheckForUpdatesOnStartup,
                    setValue: value => Config.CheckForUpdatesOnStartup = value
                );

                configMenu.AddBoolOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("config.numpad-active.name"),
                    tooltip: () => Helper.Translation.Get("config.numpad-active.tooltip"),
                    getValue: () => Config.NumpadControlsActive,
                    setValue: value => Config.NumpadControlsActive = value
                );

                configMenu.AddTextOption(
                    mod: ModManifest,
                    name: () => Helper.Translation.Get("config.numpad-profile.name"),
                    tooltip: () => Helper.Translation.Get("config.numpad-profile.tooltip"),
                    getValue: () => Config.ActiveNumpadProfile.ToString(),
                    setValue: value => {
                        if (Enum.TryParse<NumpadProfileId>(value, out var profileId))
                        {
                            Config.ActiveNumpadProfile = profileId;
                        }
                    },
                    allowedValues: new[] { "Blind", "Sighted" }
                );
            }
        }
    }

    public interface IGenericModConfigMenuApi
    {
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
        void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        void AddNumberOption(IManifest mod, Func<float> getValue, Action<float> setValue, Func<string> name, Func<string>? tooltip = null, float? min = null, float? max = null, float? interval = null, Func<float, string>? formatVal = null, string? fieldId = null);
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);
        void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string>? tooltip = null, string[]? allowedValues = null, Func<string, string>? formatAllowedValue = null, string? fieldId = null);
    }
}
