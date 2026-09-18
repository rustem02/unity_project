using UnityEngine;
using VRTraining.Core.Interfaces;
using VRTraining.Interaction;

namespace VRTraining.Highlight
{
    /// <summary>
    /// Lightweight outline/emission highlight (URP-friendly).
    /// Swap this component for a Highlight Plus adapter without touching scenario code.
    /// </summary>
    public class OutlineHighlighter : MonoBehaviour, IHighlightable
    {
        [SerializeField] private string targetId;
        [SerializeField] private Renderer[] renderers;
        [SerializeField] private Color highlightColor = new Color(0.15f, 0.85f, 1f, 1f);
        [SerializeField] private float emissionIntensity = 1.4f;
        [SerializeField] private bool createOutlineQuad = true;
        [SerializeField] private float outlineScale = 1.06f;

        private MaterialPropertyBlock _block;
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private GameObject _outlineRoot;
        private bool _highlighted;
        private Color[] _originalColors;

        public string TargetId => targetId;
        public bool IsHighlighted => _highlighted;

        private void Awake()
        {
            _block = new MaterialPropertyBlock();

            // UI buttons use CanvasRenderer, not MeshRenderer — skip mesh outline there.
            if (GetComponentInParent<Canvas>() != null)
            {
                createOutlineQuad = false;
                renderers = System.Array.Empty<Renderer>();
            }
            else if (renderers == null || renderers.Length == 0)
            {
                renderers = GetComponentsInChildren<Renderer>();
            }

            CacheOriginalColors();

            if (string.IsNullOrWhiteSpace(targetId))
            {
                // Prefer sibling interaction ids when present.
                var zone = GetComponent<InteractionZone>();
                if (zone != null) targetId = zone.TargetId;
                var grab = GetComponent<GrabbableInteractable>();
                if (grab != null) targetId = grab.TargetId;
                var click = GetComponent<ClickableInteractable>();
                if (click != null) targetId = click.TargetId;
                var ui = GetComponent<ScenarioUIButton>();
                if (ui != null) targetId = ui.TargetId;
            }

            if (createOutlineQuad)
                BuildOutlineProxy();
        }

        private void CacheOriginalColors()
        {
            _originalColors = new Color[renderers.Length];
            for (var i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;
                var mat = renderers[i].sharedMaterial;
                if (mat != null && mat.HasProperty(BaseColorId))
                    _originalColors[i] = mat.GetColor(BaseColorId);
                else if (mat != null && mat.HasProperty("_Color"))
                    _originalColors[i] = mat.GetColor("_Color");
                else
                    _originalColors[i] = Color.white;
            }
        }

        private void BuildOutlineProxy()
        {
            if (renderers.Length == 0 || renderers[0] == null)
                return;

            _outlineRoot = new GameObject("OutlineProxy");
            _outlineRoot.transform.SetParent(transform, false);
            _outlineRoot.transform.localScale = Vector3.one * outlineScale;

            var sourceFilter = renderers[0].GetComponent<MeshFilter>();
            if (sourceFilter == null)
            {
                Destroy(_outlineRoot);
                _outlineRoot = null;
                return;
            }

            var filter = _outlineRoot.AddComponent<MeshFilter>();
            filter.sharedMesh = sourceFilter.sharedMesh;
            var outlineRenderer = _outlineRoot.AddComponent<MeshRenderer>();

            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null)
                shader = Shader.Find("Unlit/Color");

            var mat = new Material(shader);
            if (mat.HasProperty(BaseColorId))
                mat.SetColor(BaseColorId, highlightColor);
            else
                mat.color = highlightColor;

            outlineRenderer.sharedMaterial = mat;
            outlineRenderer.enabled = false;
            _outlineRoot.SetActive(false);
        }

        public void SetHighlighted(bool highlighted)
        {
            _highlighted = highlighted;
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            for (var i = 0; i < renderers.Length; i++)
            {
                var r = renderers[i];
                if (r == null)
                    continue;

                r.GetPropertyBlock(_block);
                if (_highlighted)
                {
                    var emission = highlightColor * emissionIntensity;
                    _block.SetColor(EmissionColorId, emission);
                    if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId))
                        _block.SetColor(BaseColorId, Color.Lerp(_originalColors[i], highlightColor, 0.35f));
                }
                else
                {
                    _block.SetColor(EmissionColorId, Color.black);
                    if (r.sharedMaterial != null && r.sharedMaterial.HasProperty(BaseColorId))
                        _block.SetColor(BaseColorId, _originalColors[i]);
                }

                r.SetPropertyBlock(_block);

                // Enable emission keyword when possible.
                foreach (var mat in r.materials)
                {
                    if (mat.HasProperty(EmissionColorId))
                    {
                        if (_highlighted)
                            mat.EnableKeyword("_EMISSION");
                        else
                            mat.DisableKeyword("_EMISSION");
                    }
                }
            }

            if (_outlineRoot != null)
            {
                _outlineRoot.SetActive(_highlighted);
                var outlineRenderer = _outlineRoot.GetComponent<MeshRenderer>();
                if (outlineRenderer != null)
                    outlineRenderer.enabled = _highlighted;
            }
        }
    }
}
