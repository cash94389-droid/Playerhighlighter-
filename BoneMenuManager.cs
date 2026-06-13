using BoneLib.BoneMenu;
using MelonLoader;
using UnityEngine;

namespace PlayerHighlighter
{
    /// <summary>
    /// Registers a BoneMenu page with live-updating controls for all highlight settings.
    /// </summary>
    public class BoneMenuManager
    {
        private Page  _mainPage;
        private Page  _colorPage;
        private Page  _advancedPage;

        // Live color cache (to avoid hammering prefs every frame)
        private float _r, _g, _b, _a;

        public void Setup()
        {
            try
            {
                // Root page
                _mainPage = Page.Root.CreatePage("Player Highlighter", Color.cyan);

                // ── Enable toggle ──────────────────────────────────────────────
                _mainPage.CreateBool("Enabled",
                    Color.green,
                    PlayerHighlighterMod.PrefEnabled.Value,
                    v =>
                    {
                        PlayerHighlighterMod.PrefEnabled.Value = v;
                        MelonPreferences.Save();

                        if (!v)
                            PlayerHighlighterMod.HighlightMgr?.ClearAllHighlights();
                    });

                // ── Outline width ──────────────────────────────────────────────
                _mainPage.CreateFloat("Outline Width",
                    Color.white,
                    PlayerHighlighterMod.PrefOutlineWidth.Value,
                    0.001f, 0.001f, 0.05f,
                    v =>
                    {
                        PlayerHighlighterMod.PrefOutlineWidth.Value = v;
                        MelonPreferences.Save();
                    });

                // ── Show nametags ──────────────────────────────────────────────
                _mainPage.CreateBool("Show Nametags",
                    Color.yellow,
                    PlayerHighlighterMod.PrefShowNametags.Value,
                    v =>
                    {
                        PlayerHighlighterMod.PrefShowNametags.Value = v;
                        MelonPreferences.Save();
                    });

                // ── Nametag scale ──────────────────────────────────────────────
                _mainPage.CreateFloat("Nametag Scale",
                    Color.yellow,
                    PlayerHighlighterMod.PrefNametagScale.Value,
                    0.001f, 0.001f, 0.02f,
                    v =>
                    {
                        PlayerHighlighterMod.PrefNametagScale.Value = v;
                        MelonPreferences.Save();
                    });

                // ── Pulse effect ───────────────────────────────────────────────
                _mainPage.CreateBool("Pulse Effect",
                    Color.magenta,
                    PlayerHighlighterMod.PrefPulseEffect.Value,
                    v =>
                    {
                        PlayerHighlighterMod.PrefPulseEffect.Value = v;
                        MelonPreferences.Save();
                    });

                _mainPage.CreateFloat("Pulse Speed",
                    Color.magenta,
                    PlayerHighlighterMod.PrefPulseSpeed.Value,
                    0.1f, 0.1f, 5f,
                    v =>
                    {
                        PlayerHighlighterMod.PrefPulseSpeed.Value = v;
                        MelonPreferences.Save();
                    });

                // ── Color sub-page ─────────────────────────────────────────────
                _colorPage = _mainPage.CreatePage("Highlight Color", Color.cyan);
                BuildColorPage();

                // ── Advanced / presets sub-page ────────────────────────────────
                _advancedPage = _mainPage.CreatePage("Presets", Color.gray);
                BuildPresetsPage();

                MelonLogger.Msg("[PlayerHighlighter] BoneMenu registered.");
            }
            catch (System.Exception ex)
            {
                MelonLogger.Error($"[PlayerHighlighter] BoneMenu setup failed: {ex}");
            }
        }

        private void BuildColorPage()
        {
            _colorPage.CreateFloat("Red",
                Color.red,
                PlayerHighlighterMod.PrefHighlightR.Value,
                0.05f, 0f, 1f,
                v => { PlayerHighlighterMod.PrefHighlightR.Value = v; MelonPreferences.Save(); });

            _colorPage.CreateFloat("Green",
                Color.green,
                PlayerHighlighterMod.PrefHighlightG.Value,
                0.05f, 0f, 1f,
                v => { PlayerHighlighterMod.PrefHighlightG.Value = v; MelonPreferences.Save(); });

            _colorPage.CreateFloat("Blue",
                Color.blue,
                PlayerHighlighterMod.PrefHighlightB.Value,
                0.05f, 0f, 1f,
                v => { PlayerHighlighterMod.PrefHighlightB.Value = v; MelonPreferences.Save(); });

            _colorPage.CreateFloat("Alpha",
                Color.white,
                PlayerHighlighterMod.PrefHighlightA.Value,
                0.05f, 0.1f, 1f,
                v => { PlayerHighlighterMod.PrefHighlightA.Value = v; MelonPreferences.Save(); });
        }

        private void BuildPresetsPage()
        {
            // Quick color presets as function buttons
            _advancedPage.CreateFunction("Preset: Cyan (Default)",   Color.cyan,    () => ApplyPreset(0f, 0.8f, 1f, 0.85f));
            _advancedPage.CreateFunction("Preset: Red",              Color.red,     () => ApplyPreset(1f, 0.1f, 0.1f, 0.85f));
            _advancedPage.CreateFunction("Preset: Gold",             Color.yellow,  () => ApplyPreset(1f, 0.84f, 0f, 0.85f));
            _advancedPage.CreateFunction("Preset: Green",            Color.green,   () => ApplyPreset(0.1f, 1f, 0.2f, 0.85f));
            _advancedPage.CreateFunction("Preset: Purple",           Color.magenta, () => ApplyPreset(0.6f, 0f, 1f, 0.85f));
            _advancedPage.CreateFunction("Preset: White",            Color.white,   () => ApplyPreset(1f, 1f, 1f, 0.75f));

            _advancedPage.CreateFunction("Reset to Defaults", Color.gray, ResetDefaults);
        }

        private static void ApplyPreset(float r, float g, float b, float a)
        {
            PlayerHighlighterMod.PrefHighlightR.Value = r;
            PlayerHighlighterMod.PrefHighlightG.Value = g;
            PlayerHighlighterMod.PrefHighlightB.Value = b;
            PlayerHighlighterMod.PrefHighlightA.Value = a;
            MelonPreferences.Save();
        }

        private static void ResetDefaults()
        {
            PlayerHighlighterMod.PrefEnabled.Value      = true;
            PlayerHighlighterMod.PrefHighlightR.Value   = 0f;
            PlayerHighlighterMod.PrefHighlightG.Value   = 0.8f;
            PlayerHighlighterMod.PrefHighlightB.Value   = 1f;
            PlayerHighlighterMod.PrefHighlightA.Value   = 0.85f;
            PlayerHighlighterMod.PrefOutlineWidth.Value = 0.012f;
            PlayerHighlighterMod.PrefShowNametags.Value = true;
            PlayerHighlighterMod.PrefNametagScale.Value = 0.006f;
            PlayerHighlighterMod.PrefPulseEffect.Value  = true;
            PlayerHighlighterMod.PrefPulseSpeed.Value   = 1.5f;
            MelonPreferences.Save();
        }
    }
}
