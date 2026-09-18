using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VRTraining.Highlight;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRTraining.Interaction
{
    /// <summary>
    /// Desktop click / grab / world-UI press.
    /// Look-mode (RMB / locked cursor) aims from screen center so LMB can hit world buttons.
    /// Prefers currently highlighted targets to avoid grabbing distractors.
    /// </summary>
    public class DesktopInteractionRay : MonoBehaviour
    {
        [SerializeField] private Camera rayCamera;
        [SerializeField] private float maxDistance = 14f;
        [SerializeField] private float aimRadius = 0.07f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private KeyCode grabKey = KeyCode.E;

        private readonly RaycastHit[] _hits = new RaycastHit[32];
        private readonly List<RaycastResult> _uiHits = new List<RaycastResult>(16);

        private void Awake()
        {
            if (rayCamera == null)
                rayCamera = Camera.main;
        }

        private void Update()
        {
            if (rayCamera == null)
                rayCamera = Camera.main;
            if (rayCamera == null)
                return;

            if (WasLeftClick() && !IsBlockingScreenUi())
            {
                // UI first — otherwise players get stuck on PressUIButton steps in look-mode.
                if (!TryClickWorldUi())
                    TryPrimaryInteract();
            }

            if (WasPressedThisFrame(grabKey))
                TryGrab();
        }

        private void TryPrimaryInteract()
        {
            if (!TryPickInteractable(out var clickable, out var grabbable))
                return;

            if (clickable != null)
            {
                clickable.NotifyClicked();
                return;
            }

            grabbable?.NotifyGrabbed();
        }

        private void TryGrab()
        {
            if (!TryPickInteractable(out _, out var grabbable))
                return;
            grabbable?.NotifyGrabbed();
        }

        /// <summary>
        /// Click world-space scenario buttons via EventSystem using the same aim as gameplay.
        /// </summary>
        private bool TryClickWorldUi()
        {
            if (EventSystem.current == null)
                return false;

            _uiHits.Clear();
            var eventData = new PointerEventData(EventSystem.current)
            {
                position = GetAimScreenPosition()
            };
            EventSystem.current.RaycastAll(eventData, _uiHits);

            for (var i = 0; i < _uiHits.Count; i++)
            {
                var go = _uiHits[i].gameObject;
                var canvas = go.GetComponentInParent<Canvas>();
                if (canvas == null || canvas.renderMode != RenderMode.WorldSpace)
                    continue;

                var scenarioBtn = go.GetComponentInParent<ScenarioUIButton>();
                if (scenarioBtn != null)
                {
                    var btn = scenarioBtn.GetComponent<Button>();
                    if (btn != null && btn.interactable)
                    {
                        btn.onClick.Invoke();
                        return true;
                    }
                }

                var button = go.GetComponentInParent<Button>();
                if (button != null && button.interactable)
                {
                    button.onClick.Invoke();
                    return true;
                }
            }

            return false;
        }

        private bool TryPickInteractable(out ClickableInteractable clickable, out GrabbableInteractable grabbable)
        {
            clickable = null;
            grabbable = null;

            var ray = GetAimRay();
            var count = Physics.SphereCastNonAlloc(
                ray, aimRadius, _hits, maxDistance, interactableMask, QueryTriggerInteraction.Ignore);
            if (count <= 0)
                count = Physics.RaycastNonAlloc(
                    ray, _hits, maxDistance, interactableMask, QueryTriggerInteraction.Ignore);
            if (count <= 0)
                return false;

            ClickableInteractable bestClick = null;
            GrabbableInteractable bestGrab = null;
            var bestClickScore = float.MinValue;
            var bestGrabScore = float.MinValue;

            for (var i = 0; i < count; i++)
            {
                var col = _hits[i].collider;
                if (col == null)
                    continue;

                var distScore = -_hits[i].distance;

                var c = col.GetComponentInParent<ClickableInteractable>();
                if (c != null)
                {
                    var score = distScore + (IsHighlightedTarget(c.TargetId) ? 1000f : 0f);
                    if (score > bestClickScore)
                    {
                        bestClick = c;
                        bestClickScore = score;
                    }
                }

                var g = col.GetComponentInParent<GrabbableInteractable>();
                if (g != null)
                {
                    var score = distScore + (IsHighlightedTarget(g.TargetId) ? 1000f : 0f);
                    if (score > bestGrabScore)
                    {
                        bestGrab = g;
                        bestGrabScore = score;
                    }
                }
            }

            clickable = bestClick;
            grabbable = bestGrab;
            return clickable != null || grabbable != null;
        }

        private static bool IsHighlightedTarget(string targetId)
        {
            if (string.IsNullOrEmpty(targetId))
                return false;

            var highlighters = FindObjectsByType<OutlineHighlighter>();
            for (var i = 0; i < highlighters.Length; i++)
            {
                var h = highlighters[i];
                if (h != null && h.IsHighlighted &&
                    string.Equals(h.TargetId, targetId, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private Ray GetAimRay()
        {
            if (IsLookMode())
                return rayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return rayCamera.ScreenPointToRay(GetMousePosition());
        }

        private Vector2 GetAimScreenPosition()
        {
            if (IsLookMode())
                return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            return GetMousePosition();
        }

        private static bool IsLookMode()
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                return true;
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.rightButton.isPressed)
                return true;
#endif
            return Input.GetMouseButton(1);
        }

        private static Vector3 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
            return Input.mousePosition;
        }

        private static bool WasLeftClick()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                return true;
#endif
            return Input.GetMouseButtonDown(0);
        }

        private static bool WasPressedThisFrame(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                var mapped = ToInputSystemKey(key);
                if (mapped.HasValue && Keyboard.current[mapped.Value].wasPressedThisFrame)
                    return true;
            }
#endif
            return Input.GetKeyDown(key);
        }

#if ENABLE_INPUT_SYSTEM
        private static Key? ToInputSystemKey(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.E: return Key.E;
                case KeyCode.T: return Key.T;
                case KeyCode.Escape: return Key.Escape;
                default: return null;
            }
        }
#endif

        private static bool IsBlockingScreenUi()
        {
            if (EventSystem.current == null)
                return false;

            var results = new List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = GetMousePosition() };
            EventSystem.current.RaycastAll(eventData, results);
            for (var i = 0; i < results.Count; i++)
            {
                var canvas = results[i].gameObject.GetComponentInParent<Canvas>();
                // Only true overlay HUD blocks gameplay — never Crosshair (no raycast) / world UI.
                if (canvas != null &&
                    canvas.renderMode == RenderMode.ScreenSpaceOverlay &&
                    canvas.name != "CrosshairCanvas")
                    return true;
            }

            return false;
        }
    }
}
