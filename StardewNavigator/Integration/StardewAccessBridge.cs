using System;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using StardewModdingAPI;

namespace StardewNavigator.Integration
{
    /// <summary>
    /// Componente dedicato alla comunicazione con stardew-access tramite reflection.
    /// Centralizza tutti i lookup degli assembly, i tipi, i metodi e le proprietà.
    /// Mantiene in cache esclusivamente i metadata stabili (Assembly, Type, MethodInfo, PropertyInfo, FieldInfo)
    /// e risolve dinamicamente le istanze runtime ad ogni chiamata per evitare riferimenti obsoleti o non validi.
    /// </summary>
    public static class StardewAccessBridge
    {
        // Cache dei metadata stabili
        private static Assembly? _assembly;
        
        private static Type? _mainClassType;
        private static PropertyInfo? _screenReaderProp;
        private static MethodInfo? _sayMethod;

        private static Type? _gridMovementType;
        private static PropertyInfo? _gridMovementInstanceProp;
        private static MethodInfo? _handleGridMovementMethod;

        private static Type? _tileViewerType;
        private static PropertyInfo? _tileViewerInstanceProp;
        private static MethodInfo? _cursorMoveInputMethod;
        private static MethodInfo? _startAutoWalkingMethod;

        private static Type? _objectTrackerType;
        private static PropertyInfo? _objectTrackerInstanceProp;
        private static Type? _cycleType;
        private static MethodInfo? _cycleMethod;
        private static MethodInfo? _moveToCurrentlySelectedObjectMethod;

        private static Type? _readTileType;
        private static PropertyInfo? _readTileInstanceProp;
        private static MethodInfo? _readTileRunMethod;

        private static MemberInfo? _configMember;
        private static PropertyInfo? _otWrapListsProp;
        private static FieldInfo? _lastGridMovementButtonPressedField;
        private static FieldInfo? _lastGridMovementDirectionField;

        /// <summary>
        /// Ritorna true se stardew-access è caricato nel gioco.
        /// </summary>
        public static bool IsModLoaded =>
            ModEntry.Helper.ModRegistry.IsLoaded("shoaib.stardewaccess") 
            || ModEntry.Helper.ModRegistry.IsLoaded("stardew.access");

        // Helper per la risoluzione lazy dell'assembly
        private static Assembly? GetAssembly()
        {
            if (_assembly != null) return _assembly;
            _assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "stardew-access");
            return _assembly;
        }

        // Helper per la risoluzione lazy dei tipi
        private static Type? GetType(string typeName, ref Type? cache)
        {
            if (cache != null) return cache;
            var assembly = GetAssembly();
            if (assembly == null) return null;
            try
            {
                cache = assembly.GetType(typeName);
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore nel caricamento del tipo {typeName}: {ex.Message}");
            }
            return cache;
        }

        // Helper per la risoluzione lazy delle proprietà
        private static PropertyInfo? GetProperty(Type? type, string propName, BindingFlags flags, ref PropertyInfo? cache)
        {
            if (cache != null) return cache;
            if (type == null) return null;
            try
            {
                cache = type.GetProperty(propName, flags);
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore nel caricamento della proprietà {propName} su {type.FullName}: {ex.Message}");
            }
            return cache;
        }

        // Helper per leggere la configurazione OTWrapLists in modo sicuro
        private static bool GetOTWrapLists()
        {
            try
            {
                var mainType = GetType("stardew_access.MainClass", ref _mainClassType);
                if (mainType == null) return false;

                if (_configMember == null)
                {
                    _configMember = (MemberInfo?)mainType.GetProperty("Config", BindingFlags.Public | BindingFlags.Static)
                        ?? mainType.GetField("Config", BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic);
                }

                if (_configMember == null) return false;

                object? configObj = null;
                if (_configMember is PropertyInfo pInfo) configObj = pInfo.GetValue(null);
                else if (_configMember is FieldInfo fInfo) configObj = fInfo.GetValue(null);

                if (configObj == null) return false;

                if (_otWrapListsProp == null)
                {
                    _otWrapListsProp = configObj.GetType().GetProperty("OTWrapLists");
                }

                return (bool)(_otWrapListsProp?.GetValue(configObj) ?? false);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tenta di verificare se l'ObjectTracker è disponibile a runtime.
        /// </summary>
        public static bool IsObjectTrackerAvailable()
        {
            if (!IsModLoaded) return false;
            try
            {
                var type = GetType("stardew_access.Features.ObjectTracker", ref _objectTrackerType);
                var prop = GetProperty(type, "Instance", BindingFlags.Public | BindingFlags.Static, ref _objectTrackerInstanceProp);
                if (prop == null) return false;

                var instance = prop.GetValue(null);
                return instance != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Tenta di pronunciare il testo tramite lo screen reader di stardew-access.
        /// </summary>
        public static bool TrySpeak(string text, bool interrupt)
        {
            if (!IsModLoaded) return false;
            try
            {
                var mainType = GetType("stardew_access.MainClass", ref _mainClassType);
                var prop = GetProperty(mainType, "ScreenReader", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static, ref _screenReaderProp);
                if (prop == null) return false;

                var instance = prop.GetValue(null);
                if (instance == null) return false;

                if (_sayMethod == null)
                {
                    _sayMethod = instance.GetType().GetMethod("Say", new[] { typeof(string), typeof(bool) });
                }

                if (_sayMethod == null) return false;

                _sayMethod.Invoke(instance, new object[] { text, interrupt });
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore nell'invocazione di ScreenReader.Say: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tenta di delegare il movimento a griglia a stardew-access.
        /// </summary>
        public static bool TryHandleGridMovement(int direction, SButton sButton)
        {
            if (!IsModLoaded) return false;
            try
            {
                var type = GetType("stardew_access.Features.GridMovement", ref _gridMovementType);
                var prop = GetProperty(type, "Instance", BindingFlags.Public | BindingFlags.Static, ref _gridMovementInstanceProp);
                if (prop == null) return false;

                var instance = prop.GetValue(null);
                if (instance == null) return false;

                if (_handleGridMovementMethod == null)
                {
                    _handleGridMovementMethod = instance.GetType().GetMethod("HandleGridMovement", BindingFlags.Public | BindingFlags.Instance);
                }

                if (_handleGridMovementMethod == null) return false;

                var key = (Microsoft.Xna.Framework.Input.Keys)sButton;
                var inputButton = new StardewValley.InputButton(key);

                _handleGridMovementMethod.Invoke(instance, new object[] { direction, inputButton });
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore in TryHandleGridMovement: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tenta di spostare il cursore del TileViewer di stardew-access.
        /// </summary>
        public static bool TryMoveTileViewerCursor(Vector2 delta, bool precise)
        {
            if (!IsModLoaded) return false;
            try
            {
                var type = GetType("stardew_access.Features.TileViewer", ref _tileViewerType);
                var prop = GetProperty(type, "Instance", BindingFlags.Public | BindingFlags.Static, ref _tileViewerInstanceProp);
                if (prop == null) return false;

                var instance = prop.GetValue(null);
                if (instance == null) return false;

                if (_cursorMoveInputMethod == null)
                {
                    _cursorMoveInputMethod = instance.GetType().GetMethod("CursorMoveInput", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (_cursorMoveInputMethod == null) return false;

                _cursorMoveInputMethod.Invoke(instance, new object[] { delta, precise });
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore in TryMoveTileViewerCursor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tenta di avviare l'auto-walk del TileViewer.
        /// </summary>
        public static bool TryStartAutoWalkToTileViewerCursor()
        {
            if (!IsModLoaded) return false;
            try
            {
                var type = GetType("stardew_access.Features.TileViewer", ref _tileViewerType);
                var prop = GetProperty(type, "Instance", BindingFlags.Public | BindingFlags.Static, ref _tileViewerInstanceProp);
                if (prop == null) return false;

                var instance = prop.GetValue(null);
                if (instance == null) return false;

                if (_startAutoWalkingMethod == null)
                {
                    _startAutoWalkingMethod = instance.GetType().GetMethod("StartAutoWalking", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (_startAutoWalkingMethod == null) return false;

                _startAutoWalkingMethod.Invoke(instance, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore in TryStartAutoWalkToTileViewerCursor: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tenta di ciclare l'ObjectTracker di stardew-access.
        /// </summary>
        public static bool TryCycleObjectTracker(int cycleLevel, bool back)
        {
            if (!IsModLoaded) return false;
            try
            {
                var type = GetType("stardew_access.Features.ObjectTracker", ref _objectTrackerType);
                var prop = GetProperty(type, "Instance", BindingFlags.Public | BindingFlags.Static, ref _objectTrackerInstanceProp);
                if (prop == null) return false;

                var instance = prop.GetValue(null);
                if (instance == null) return false;

                if (_cycleType == null)
                {
                    _cycleType = instance.GetType().GetNestedType("CycleType", BindingFlags.NonPublic);
                }

                if (_cycleType == null) return false;

                var cycleVal = Enum.ToObject(_cycleType, cycleLevel);
                bool wrapAround = GetOTWrapLists();

                if (_cycleMethod == null)
                {
                    _cycleMethod = instance.GetType().GetMethod("Cycle", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (_cycleMethod == null) return false;

                _cycleMethod.Invoke(instance, new object[] { cycleVal, back, wrapAround });
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore in TryCycleObjectTracker: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tenta di avviare l'auto-walk all'oggetto correntemente selezionato nell'ObjectTracker.
        /// </summary>
        public static bool TryMoveToSelectedObject()
        {
            if (!IsModLoaded) return false;
            try
            {
                var type = GetType("stardew_access.Features.ObjectTracker", ref _objectTrackerType);
                var prop = GetProperty(type, "Instance", BindingFlags.Public | BindingFlags.Static, ref _objectTrackerInstanceProp);
                if (prop == null) return false;

                var instance = prop.GetValue(null);
                if (instance == null) return false;

                if (_moveToCurrentlySelectedObjectMethod == null)
                {
                    _moveToCurrentlySelectedObjectMethod = instance.GetType().GetMethod("MoveToCurrentlySelectedObject", BindingFlags.NonPublic | BindingFlags.Instance);
                }

                if (_moveToCurrentlySelectedObjectMethod == null) return false;

                _moveToCurrentlySelectedObjectMethod.Invoke(instance, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore in TryMoveToSelectedObject: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Tenta di delegare la lettura della tile a stardew-access.
        /// </summary>
        public static bool TryReadTile(bool standing)
        {
            if (!IsModLoaded) return false;
            try
            {
                var type = GetType("stardew_access.Features.ReadTile", ref _readTileType);
                if (type == null)
                {
                    Log.Warn("[StardewAccessBridge] Failed to delegate ReadTile: stardew_access.Features.ReadTile class not found.");
                    return false;
                }

                var prop = GetProperty(type, "Instance", BindingFlags.Public | BindingFlags.Static, ref _readTileInstanceProp);
                if (prop == null)
                {
                    Log.Warn("[StardewAccessBridge] Failed to delegate ReadTile: ReadTile.Instance property not found.");
                    return false;
                }

                var instance = prop.GetValue(null);
                if (instance == null)
                {
                    Log.Warn("[StardewAccessBridge] Failed to delegate ReadTile: ReadTile.Instance property is null.");
                    return false;
                }

                if (_readTileRunMethod == null)
                {
                    _readTileRunMethod = instance.GetType().GetMethod("Run", BindingFlags.Public | BindingFlags.Instance);
                }

                if (_readTileRunMethod == null)
                {
                    Log.Warn("[StardewAccessBridge] Failed to delegate ReadTile: ReadTile.Run method not found.");
                    return false;
                }

                _readTileRunMethod.Invoke(instance, new object[] { true, standing });
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Failed to delegate ReadTile to stardew-access: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Resetta lo stato del movimento a griglia di stardew-access per fermare movimenti errati causati da soppressione.
        /// </summary>
        public static bool TryResetGridMovementState()
        {
            if (!IsModLoaded) return false;
            try
            {
                var type = GetType("stardew_access.Features.GridMovement", ref _gridMovementType);
                if (type == null) return false;

                if (_lastGridMovementButtonPressedField == null)
                {
                    _lastGridMovementButtonPressedField = type.GetField("LastGridMovementButtonPressed", BindingFlags.NonPublic | BindingFlags.Static);
                }
                if (_lastGridMovementDirectionField == null)
                {
                    _lastGridMovementDirectionField = type.GetField("LastGridMovementDirection", BindingFlags.NonPublic | BindingFlags.Static);
                }

                _lastGridMovementButtonPressedField?.SetValue(null, null);
                _lastGridMovementDirectionField?.SetValue(null, null);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warn($"[StardewAccessBridge] Errore in TryResetGridMovementState: {ex.Message}");
                return false;
            }
        }
    }
}
