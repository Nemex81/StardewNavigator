using System;
using System.IO;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace StardewNavigator.Features.Navigator
{
    public class NavigatorFeature
    {
        public static NavigatorFeature Instance { get; private set; } = null!;

        private readonly RouteEngine _routeEngine;
        private readonly Navigator _navigator;
        private readonly DestinationRegistry _destinationRegistry;
        private readonly IModHelper _helper;

        public NavigatorFeature(IModHelper helper)
        {
            Instance = this;
            _helper = helper;

            _routeEngine = new RouteEngine();
            _navigator = new Navigator();

            // Carica il registro dei POI da assets/navigator_destinations.json
            string assetsPath = Path.Combine(helper.DirectoryPath, "assets");
            _destinationRegistry = new DestinationRegistry(assetsPath);
            _destinationRegistry.Load();

            // Registra eventi SMAPI
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Input.ButtonPressed += OnButtonPressed;
            helper.Events.Player.Warped += OnPlayerWarped;

#if DEBUG
            // Registra il comando di console SMAPI per il test di fallback deterministico
            helper.ConsoleCommands.Add("navigator_test_fallback", "Testa il funzionamento del fallback di collisione per BusStop (11,6)", (cmd, args) =>
            {
                if (!Context.IsWorldReady)
                {
                    Log.Info("[TEST] Il mondo deve essere caricato con un salvataggio attivo per eseguire questo test.");
                    return;
                }
                GameLocation busStop = Game1.getLocationFromName("BusStop");
                if (busStop == null)
                {
                    Log.Error("[TEST] Mappa BusStop non trovata!");
                    return;
                }
                
                Point target = new Point(11, 6);
                Point result = _navigator.TestGetNearestPassableTile(busStop, target, 35, 30);
                
                Log.Info($"[TEST] Target originale: {target} | Risultato: {result}");
                
                bool targetCollides = _navigator.TestIsTileCollidingInGame(busStop, target.X, target.Y);
                bool resultCollides = _navigator.TestIsTileCollidingInGame(busStop, result.X, result.Y);
                
                if (targetCollides && !resultCollides && result != target)
                {
                    Log.Info("TEST RISULTATO: PASS ✅");
                }
                else
                {
                    Log.Error($"TEST RISULTATO: FAIL ❌ (targetCollides={targetCollides}, resultCollides={resultCollides}, result={result})");
                }
            });
#endif
        }

        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            _navigator.OnUpdateTicked(e);
        }

        private void OnButtonPressed(object? sender, ButtonPressedEventArgs e)
        {
            if (!Context.IsWorldReady) return;

            // Intercetta e gestisce i tasti del tastierino numerico (Numpad)
            if (NumpadController.HandleButton(e, _navigator, _destinationRegistry, _routeEngine))
            {
                _helper.Input.Suppress(e.Button);
                return;
            }

            // Se la navigazione è attiva, intercetta i tasti di movimento manuali per annullare
            if (_navigator.IsActive)
            {
                _navigator.OnButtonPressed(e);
            }

            // Tasto di apertura menu Navigator
            if (ModEntry.Config.NavigatorMenuKey.JustPressed())
            {
                if (!Context.IsPlayerFree) return;

                Game1.activeClickableMenu = new NavigatorMenu(
                    _destinationRegistry.Maps,
                    (map, poi) => _navigator.StartNavigation(map, poi, _routeEngine),
                    text => NavigatorSpeaker.Say(text, true)
                );
            }
        }

        private void OnPlayerWarped(object? sender, WarpedEventArgs e)
        {
            if (!Context.IsWorldReady) return;
            _navigator.OnPlayerWarped(e);
        }

        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // Ricostruisce il grafo al caricamento del salvataggio
            _routeEngine.BuildGraph();
            _destinationRegistry.ResolveCoordinates(_routeEngine);
        }
    }
}
