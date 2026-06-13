using System;
using System.Reflection;
using MelonLoader;
using UnityEngine;

namespace PlayerHighlighter
{
    /// <summary>
    /// Bridges LabFusion (Fusion) multiplayer events without a hard dependency.
    /// All reflection is done at runtime so the mod loads fine in solo play.
    /// </summary>
    public static class FusionBridge
    {
        private static bool _fusionPresent  = false;
        private static bool _hooksAttached  = false;

        // Cached reflection references
        private static Type   _networkPlayerType;
        private static Type   _playerIdManagerType;
        private static Type   _playerId_Type;
        private static EventInfo _onPlayerRepJoinedEvent;
        private static EventInfo _onPlayerRepLeftEvent;
        private static MethodInfo _getSmallIdMethod;
        private static MethodInfo _getDisplayNameMethod;
        private static PropertyInfo _playerGameObjectProp;

        public static bool IsFusionRunning => _fusionPresent;

        public static void TryHookFusion()
        {
            if (_hooksAttached) return;

            // Check if LabFusion assembly is loaded
            Assembly fusionAsm = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.GetName().Name.Contains("LabFusion"))
                {
                    fusionAsm = asm;
                    break;
                }
            }

            if (fusionAsm == null)
            {
                MelonLogger.Msg("[PlayerHighlighter] LabFusion not detected — solo mode.");
                return;
            }

            _fusionPresent = true;
            MelonLogger.Msg("[PlayerHighlighter] LabFusion detected! Hooking player events...");

            try
            {
                // ----- Resolve types -----
                _networkPlayerType   = fusionAsm.GetType("LabFusion.Network.NetworkPlayer");
                _playerIdManagerType = fusionAsm.GetType("LabFusion.Data.PlayerIdManager");
                _playerId_Type       = fusionAsm.GetType("LabFusion.Data.PlayerId");

                if (_networkPlayerType == null || _playerIdManagerType == null || _playerId_Type == null)
                {
                    MelonLogger.Warning("[PlayerHighlighter] Could not resolve Fusion types. Version mismatch?");
                    return;
                }

                // PlayerId helpers
                _getSmallIdMethod      = _playerId_Type.GetMethod("GetSmallId",     BindingFlags.Public | BindingFlags.Instance);
                _getDisplayNameMethod  = _playerId_Type.GetMethod("GetDisplayName", BindingFlags.Public | BindingFlags.Instance);

                // NetworkPlayer.GameObject property
                _playerGameObjectProp  = _networkPlayerType.GetProperty("GameObject",
                    BindingFlags.Public | BindingFlags.Instance);

                // Subscribe to join / leave events via reflection
                _onPlayerRepJoinedEvent = fusionAsm
                    .GetType("LabFusion.Network.FusionPlayer")
                    ?.GetEvent("OnPlayerRepCreated", BindingFlags.Public | BindingFlags.Static);

                _onPlayerRepLeftEvent = fusionAsm
                    .GetType("LabFusion.Network.FusionPlayer")
                    ?.GetEvent("OnPlayerRepDestroyed", BindingFlags.Public | BindingFlags.Static);

                if (_onPlayerRepJoinedEvent != null)
                {
                    var handler = new Action<object>(OnRepCreated);
                    var del = ConvertDelegate(handler, _onPlayerRepJoinedEvent.EventHandlerType);
                    _onPlayerRepJoinedEvent.AddEventHandler(null, del);
                }

                if (_onPlayerRepLeftEvent != null)
                {
                    var handler = new Action<object>(OnRepDestroyed);
                    var del = ConvertDelegate(handler, _onPlayerRepLeftEvent.EventHandlerType);
                    _onPlayerRepLeftEvent.AddEventHandler(null, del);
                }

                _hooksAttached = true;
                MelonLogger.Msg("[PlayerHighlighter] Fusion hooks attached successfully.");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PlayerHighlighter] Failed to hook Fusion: {ex}");
            }
        }

        // Receives a NetworkPlayer / PlayerRep object
        private static void OnRepCreated(object rep)
        {
            try
            {
                // Try to get the PlayerId from the rep
                var idField = rep.GetType()
                    .GetProperty("PlayerId", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(rep);

                if (idField == null) return;

                byte   smallId     = (byte)(_getSmallIdMethod?.Invoke(idField, null) ?? 0);
                string displayName = (string)(_getDisplayNameMethod?.Invoke(idField, null) ?? $"Player {smallId}");

                GameObject playerGO = _playerGameObjectProp?.GetValue(rep) as GameObject;
                if (playerGO == null)
                {
                    // Fallback: search by common Fusion NPC root name
                    playerGO = FindPlayerRoot(smallId);
                }

                if (playerGO != null)
                    PlayerHighlighterMod.OnNetworkPlayerJoined(playerGO, displayName, smallId);
                else
                    MelonLogger.Warning($"[PlayerHighlighter] Could not find GameObject for {displayName}");
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PlayerHighlighter] OnRepCreated error: {ex}");
            }
        }

        private static void OnRepDestroyed(object rep)
        {
            try
            {
                var idField = rep.GetType()
                    .GetProperty("PlayerId", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(rep);

                if (idField == null) return;
                byte smallId = (byte)(_getSmallIdMethod?.Invoke(idField, null) ?? 0);
                PlayerHighlighterMod.OnNetworkPlayerLeft(smallId);
            }
            catch (Exception ex)
            {
                MelonLogger.Error($"[PlayerHighlighter] OnRepDestroyed error: {ex}");
            }
        }

        // Helper: wrap an Action<object> into whatever delegate type Fusion expects
        private static Delegate ConvertDelegate(Action<object> action, Type targetType)
        {
            return Delegate.CreateDelegate(targetType, action.Target, action.Method);
        }

        // Fallback search for a player root GameObject using common naming patterns
        private static GameObject FindPlayerRoot(byte smallId)
        {
            // Fusion typically names PlayerRep roots "[RigManager (Player X)]" or "PlayerRep"
            foreach (var go in GameObject.FindObjectsOfType<GameObject>())
            {
                var name = go.name;
                if (name.Contains("PlayerRep") || name.Contains($"Player {smallId}") || name.Contains($"[NetworkPlayer_{smallId}]"))
                    return go;
            }
            return null;
        }
    }
}
