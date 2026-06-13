using MelonLoader;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using BoneLib;
using BoneLib.BoneMenu;
using SLZ.Marrow.SceneStreaming;

[assembly: MelonInfo(typeof(PlayerHighlighter.PlayerHighlighterMod), "PlayerHighlighter", "1.0.0", "YourName")]
[assembly: MelonGame("Stress Level Zero", "BONELAB")]

namespace PlayerHighlighter
{
    public class PlayerHighlighterMod : MelonMod
    {
        public static PlayerHighlighterMod Instance { get; private set; }

        // Config / preferences
        public static MelonPreferences_Category PrefCategory;
        public static MelonPreferences_Entry<bool>  PrefEnabled;
        public static MelonPreferences_Entry<float> PrefHighlightR;
        public static MelonPreferences_Entry<float> PrefHighlightG;
        public static MelonPreferences_Entry<float> PrefHighlightB;
        public static MelonPreferences_Entry<float> PrefHighlightA;
        public static MelonPreferences_Entry<float> PrefOutlineWidth;
        public static MelonPreferences_Entry<bool>  PrefShowNametags;
        public static MelonPreferences_Entry<float> PrefNametagScale;
        public static MelonPreferences_Entry<bool>  PrefPulseEffect;
        public static MelonPreferences_Entry<float> PrefPulseSpeed;

        // Runtime
        internal static HighlightManager HighlightMgr;
        internal static BoneMenuManager  BoneMenuMgr;

        public override void OnInitializeMelon()
        {
            Instance = this;

            // Register preferences
            PrefCategory    = MelonPreferences.CreateCategory("PlayerHighlighter");
            PrefEnabled     = PrefCategory.CreateEntry("Enabled",       true,  "Enable Highlighting");
            PrefHighlightR  = PrefCategory.CreateEntry("ColorR",        0f,    "Highlight Color R");
            PrefHighlightG  = PrefCategory.CreateEntry("ColorG",        0.8f,  "Highlight Color G");
            PrefHighlightB  = PrefCategory.CreateEntry("ColorB",        1f,    "Highlight Color B");
            PrefHighlightA  = PrefCategory.CreateEntry("ColorA",        0.85f, "Highlight Color A");
            PrefOutlineWidth= PrefCategory.CreateEntry("OutlineWidth",  0.012f,"Outline Width");
            PrefShowNametags= PrefCategory.CreateEntry("ShowNametags",  true,  "Show Nametags");
            PrefNametagScale= PrefCategory.CreateEntry("NametagScale",  0.006f,"Nametag Scale");
            PrefPulseEffect = PrefCategory.CreateEntry("PulseEffect",   true,  "Pulse Effect");
            PrefPulseSpeed  = PrefCategory.CreateEntry("PulseSpeed",    1.5f,  "Pulse Speed");

            // Hook BoneLib scene & player events
            Player.OnPlayerCreated += OnPlayerCreated;
            BoneLib.Hooking.OnLevelInitialized += OnLevelInitialized;
            BoneLib.Hooking.OnLevelUnloaded    += OnLevelUnloaded;

            // BoneMenu integration
            BoneMenuMgr = new BoneMenuManager();
            BoneMenuMgr.Setup();

            MelonLogger.Msg("[PlayerHighlighter] Initialized.");
        }

        public override void OnDeinitializeMelon()
        {
            Player.OnPlayerCreated -= OnPlayerCreated;
            BoneLib.Hooking.OnLevelInitialized -= OnLevelInitialized;
            BoneLib.Hooking.OnLevelUnloaded    -= OnLevelUnloaded;
        }

        private void OnLevelInitialized(LevelInfo info)
        {
            MelonLogger.Msg($"[PlayerHighlighter] Level loaded: {info.title}");
            // Spawn the highlight manager component once a scene is ready
            if (HighlightMgr == null)
            {
                var go = new GameObject("PlayerHighlighter_Manager");
                GameObject.DontDestroyOnLoad(go);
                HighlightMgr = go.AddComponent<HighlightManager>();
            }

            // Check for Fusion availability and subscribe
            FusionBridge.TryHookFusion();
        }

        private void OnLevelUnloaded()
        {
            HighlightMgr?.ClearAllHighlights();
        }

        private void OnPlayerCreated(Player player)
        {
            // Local player is not highlighted
        }

        // Called by FusionBridge when a networked player spawns
        public static void OnNetworkPlayerJoined(GameObject playerRoot, string username, byte playerId)
        {
            if (!PrefEnabled.Value) return;
            HighlightMgr?.AddHighlight(playerRoot, username, playerId);
        }

        public static void OnNetworkPlayerLeft(byte playerId)
        {
            HighlightMgr?.RemoveHighlight(playerId);
        }
    }
}
