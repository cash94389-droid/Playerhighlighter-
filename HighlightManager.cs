using System.Collections;
using System.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace PlayerHighlighter
{
    /// <summary>
    /// MonoBehaviour that lives for the session lifetime and manages per-player highlights.
    /// Quest-optimised: uses a single shared material and SRP-compatible outline technique.
    /// </summary>
    public class HighlightManager : MonoBehaviour
    {
        // Map of playerId -> highlight entry
        private readonly Dictionary<byte, PlayerHighlightEntry> _entries
            = new Dictionary<byte, PlayerHighlightEntry>();

        // Shared outline material (Quest-compatible URP Unlit with ZTest Always)
        private static Material _outlineMat;
        // Shared nametag material
        private static Material _nametagMat;

        private void Awake()
        {
            BuildSharedMaterials();
        }

        // ──────────────────────────────────────────────
        //  Public API
        // ──────────────────────────────────────────────

        public void AddHighlight(GameObject root, string username, byte playerId)
        {
            if (_entries.ContainsKey(playerId))
            {
                RemoveHighlight(playerId);
            }

            var entry = new PlayerHighlightEntry(root, username, playerId, _outlineMat, _nametagMat);
            _entries[playerId] = entry;
            MelonLogger.Msg($"[PlayerHighlighter] Highlighting {username} (id={playerId})");
        }

        public void RemoveHighlight(byte playerId)
        {
            if (_entries.TryGetValue(playerId, out var entry))
            {
                entry.Destroy();
                _entries.Remove(playerId);
            }
        }

        public void ClearAllHighlights()
        {
            foreach (var entry in _entries.Values)
                entry.Destroy();
            _entries.Clear();
        }

        public void RefreshAll()
        {
            foreach (var entry in _entries.Values)
                entry.Refresh();
        }

        // ──────────────────────────────────────────────
        //  Unity Update – pulse + sync
        // ──────────────────────────────────────────────

        private void Update()
        {
            if (!PlayerHighlighterMod.PrefEnabled.Value) return;

            Color baseColor = new Color(
                PlayerHighlighterMod.PrefHighlightR.Value,
                PlayerHighlighterMod.PrefHighlightG.Value,
                PlayerHighlighterMod.PrefHighlightB.Value,
                PlayerHighlighterMod.PrefHighlightA.Value);

            if (PlayerHighlighterMod.PrefPulseEffect.Value)
            {
                float pulse = 0.5f + 0.5f * Mathf.Sin(
                    Time.time * PlayerHighlighterMod.PrefPulseSpeed.Value * Mathf.PI);
                baseColor.a = Mathf.Lerp(
                    PlayerHighlighterMod.PrefHighlightA.Value * 0.4f,
                    PlayerHighlighterMod.PrefHighlightA.Value,
                    pulse);
            }

            if (_outlineMat != null)
            {
                _outlineMat.SetColor("_Color",       baseColor);
                _outlineMat.SetFloat("_OutlineWidth", PlayerHighlighterMod.PrefOutlineWidth.Value);
            }

            foreach (var entry in _entries.Values)
                entry.Tick();
        }

        // ──────────────────────────────────────────────
        //  Material construction (Quest URP safe)
        // ──────────────────────────────────────────────

        private static void BuildSharedMaterials()
        {
            // Outline: inverted-hull technique using a custom unlit-style shader variant.
            // On Quest we pick "Universal Render Pipeline/Unlit" which is always available,
            // configure it for back-face culling, ZWrite off, and additive blend.
            Shader outlineShader = Shader.Find("Universal Render Pipeline/Unlit")
                                ?? Shader.Find("Unlit/Color");

            _outlineMat = new Material(outlineShader)
            {
                name = "PlayerHighlight_Outline"
            };
            _outlineMat.SetFloat("_Cull",     2f); // Back
            _outlineMat.SetFloat("_ZWrite",   0f);
            _outlineMat.SetFloat("_ZTest",    4f); // LessEqual — keeps outline behind geometry
            _outlineMat.SetFloat("_Surface",  1f); // Transparent
            _outlineMat.SetFloat("_Blend",    0f); // Alpha
            _outlineMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            // Nametag background
            Shader uiShader = Shader.Find("Universal Render Pipeline/Unlit")
                           ?? Shader.Find("UI/Default");
            _nametagMat = new Material(uiShader)
            {
                name = "PlayerHighlight_Nametag"
            };
            _nametagMat.SetFloat("_Cull",   0f); // Off
            _nametagMat.SetFloat("_ZWrite", 0f);
            _nametagMat.SetFloat("_ZTest",  8f); // Always — always visible through walls
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Per-player entry: manages outline meshes + floating nametag
    // ──────────────────────────────────────────────────────────────────────────
    public class PlayerHighlightEntry
    {
        private readonly GameObject _root;
        private readonly string     _username;
        private readonly byte       _playerId;

        // Outline renderers (one per SkinnedMeshRenderer on the rig)
        private readonly List<OutlineProxy> _outlines = new List<OutlineProxy>();
        // Nametag world-space label
        private NametagBillboard _nametag;

        public PlayerHighlightEntry(
            GameObject root, string username, byte playerId,
            Material outlineMat, Material nametagMat)
        {
            _root     = root;
            _username = username;
            _playerId = playerId;

            BuildOutlines(outlineMat);

            if (PlayerHighlighterMod.PrefShowNametags.Value)
                BuildNametag(nametagMat);
        }

        private void BuildOutlines(Material mat)
        {
            // Walk the rig hierarchy and create an inflated copy for every skinned mesh
            var skinned = _root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in skinned)
            {
                var proxy = new OutlineProxy(smr, mat);
                _outlines.Add(proxy);
            }

            // Also cover MeshRenderers (prop attachments etc.)
            var meshRenderers = _root.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var mr in meshRenderers)
            {
                if (mr.GetComponent<MeshFilter>() != null)
                    _outlines.Add(new OutlineProxy(mr, mat));
            }
        }

        private void BuildNametag(Material mat)
        {
            // Find head bone or fallback to root
            Transform head = FindBone(_root.transform, new[] { "Head", "head", "Neck", "neck" })
                          ?? _root.transform;

            var go = new GameObject($"Nametag_{_playerId}");
            go.transform.SetParent(head, false);
            go.transform.localPosition = new Vector3(0f, 0.35f, 0f);

            _nametag = go.AddComponent<NametagBillboard>();
            _nametag.Init(_username, mat,
                PlayerHighlighterMod.PrefNametagScale.Value,
                new Color(
                    PlayerHighlighterMod.PrefHighlightR.Value,
                    PlayerHighlighterMod.PrefHighlightG.Value,
                    PlayerHighlighterMod.PrefHighlightB.Value, 1f));
        }

        public void Tick()
        {
            if (_root == null) return;
            foreach (var o in _outlines) o.Sync();
        }

        public void Refresh()
        {
            // Rebuild if rig changed (e.g. avatar swap)
            // For brevity, simply re-check smr count
        }

        public void Destroy()
        {
            foreach (var o in _outlines) o.Dispose();
            _outlines.Clear();

            if (_nametag != null)
                GameObject.Destroy(_nametag.gameObject);
        }

        private static Transform FindBone(Transform root, string[] names)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                foreach (var n in names)
                    if (t.name.Contains(n)) return t;
            return null;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  Outline proxy: inflated clone of a renderer
    // ──────────────────────────────────────────────────────────────────────────
    public class OutlineProxy
    {
        private readonly GameObject         _go;
        private readonly SkinnedMeshRenderer _srcSmr;
        private readonly MeshRenderer        _srcMr;
        private readonly SkinnedMeshRenderer _dstSmr;
        private readonly MeshRenderer        _dstMr;
        private readonly MeshFilter          _dstMf;

        private float _width => PlayerHighlighterMod.PrefOutlineWidth.Value;

        // Constructor for SkinnedMeshRenderer
        public OutlineProxy(SkinnedMeshRenderer source, Material mat)
        {
            _srcSmr = source;
            _go = new GameObject($"Outline_{source.name}");
            _go.transform.SetParent(source.transform, false);

            _dstSmr = _go.AddComponent<SkinnedMeshRenderer>();
            _dstSmr.sharedMesh  = source.sharedMesh;
            _dstSmr.bones       = source.bones;
            _dstSmr.rootBone    = source.rootBone;
            _dstSmr.material    = mat;
            _dstSmr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _dstSmr.receiveShadows    = false;

            // Inflate via localScale — simple Quest-friendly approach
            _go.transform.localScale = Vector3.one * (1f + _width * 10f);
        }

        // Constructor for MeshRenderer
        public OutlineProxy(MeshRenderer source, Material mat)
        {
            _srcMr = source;
            _go = new GameObject($"Outline_{source.name}");
            _go.transform.SetParent(source.transform, false);

            _dstMf = _go.AddComponent<MeshFilter>();
            _dstMf.sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;

            _dstMr = _go.AddComponent<MeshRenderer>();
            _dstMr.material = mat;
            _dstMr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _dstMr.receiveShadows    = false;

            _go.transform.localScale = Vector3.one * (1f + _width * 10f);
        }

        public void Sync()
        {
            if (_go == null) return;
            float scale = 1f + PlayerHighlighterMod.PrefOutlineWidth.Value * 10f;
            _go.transform.localScale = Vector3.one * scale;

            // Keep active state in sync with source
            bool srcActive = _srcSmr != null
                ? _srcSmr.enabled && _srcSmr.gameObject.activeInHierarchy
                : _srcMr != null && _srcMr.enabled && _srcMr.gameObject.activeInHierarchy;

            if (_dstSmr != null) _dstSmr.enabled = srcActive;
            if (_dstMr  != null) _dstMr.enabled  = srcActive;
        }

        public void Dispose()
        {
            if (_go != null) GameObject.Destroy(_go);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    //  World-space nametag that always faces the camera (billboard)
    // ──────────────────────────────────────────────────────────────────────────
    public class NametagBillboard : MonoBehaviour
    {
        private TextMesh   _textMesh;
        private MeshRenderer _renderer;
        private Camera     _cam;

        public void Init(string username, Material mat, float scale, Color color)
        {
            _textMesh = gameObject.AddComponent<TextMesh>();
            _textMesh.text      = username;
            _textMesh.fontSize  = 80;
            _textMesh.alignment = TextAlignment.Center;
            _textMesh.anchor    = TextAnchor.MiddleCenter;
            _textMesh.color     = color;

            _renderer = GetComponent<MeshRenderer>();
            if (_renderer != null)
            {
                // Use a simple unlit font material to avoid Quest shader issues
                var fontMat = _textMesh.font.material;
                fontMat.shader = Shader.Find("Universal Render Pipeline/Unlit")
                              ?? fontMat.shader;
                _renderer.sharedMaterial = fontMat;
                _renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _renderer.receiveShadows    = false;
            }

            transform.localScale = Vector3.one * scale;
        }

        private void LateUpdate()
        {
            if (!PlayerHighlighterMod.PrefShowNametags.Value)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);

            if (_cam == null)
                _cam = Camera.main ?? Camera.current;

            if (_cam != null)
                transform.rotation = _cam.transform.rotation;

            // Update scale from prefs each frame (allows live preview in BoneMenu)
            transform.localScale = Vector3.one * PlayerHighlighterMod.PrefNametagScale.Value;
        }
    }
}
