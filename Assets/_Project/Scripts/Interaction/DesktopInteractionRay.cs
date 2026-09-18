using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRTraining.Interaction
{
    /// <summary>
    /// Desktop mouse/keyboard click + grab.
    /// Prefers interactables over furniture; uses a small sphere cast for forgiveness.
    /// </summary>
    public class DesktopInteractionRay : MonoBehaviour
    {
        [SerializeField] private Camera rayCamera;
        [SerializeField] private float maxDistance = 14f;
        [SerializeField] private float aimRadius = 0.12f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private KeyCode grabKey = KeyCode.E;
        [SerializeField] private bool showDebugRay;

        private readonly RaycastHit[] _hits = new RaycastHit[24];

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
                TryPrimaryInteract();

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
        /// Sphere-cast along aim ray and prefer Clickable/Grabbable over tables/walls.
        /// </summary>
        private bool TryPickInteractable(out ClickableInteractable clickable, out GrabbableInteractable grabbable)
        {
            clickable = null;
            grabbable = null;

            var ray = GetAimRay();
            var count = Physics.SphereCastNonAlloc(
                ray, aimRadius, _hits, maxDistance, interactableMask, QueryTriggerInteraction.Ignore);
            if (count <= 0)
            {
                // Fallback thin ray for precision.
                count = Physics.RaycastNonAlloc(
                    ray, _hits, maxDistance, interactableMask, QueryTriggerInteraction.Ignore);
            }

            if (count <= 0)
                return false;

            // Sort by distance (SphereCastNonAlloc is unsorted).
            for (var i = 0; i < count - 1; i++)
            {
                for (var j = i + 1; j < count; j++)
                {
                    if (_hits[j].distance < _hits[i].distance)
                    {
                        var tmp = _hits[i];
                        _hits[i] = _hits[j];
                        _hits[j] = tmp;
                    }
                }
            }

            ClickableInteractable bestClick = null;
            GrabbableInteractable bestGrab = null;
            var bestClickDist = float.MaxValue;
            var bestGrabDist = float.MaxValue;

            for (var i = 0; i < count; i++)
            {
                var col = _hits[i].collider;
                if (col == null)
                    continue;

                var c = col.GetComponentInParent<ClickableInteractable>();
                if (c != null && _hits[i].distance < bestClickDist)
                {
                    bestClick = c;
                    bestClickDist = _hits[i].distance;
                }

                var g = col.GetComponentInParent<GrabbableInteractable>();
                if (g != null && _hits[i].distance < bestGrabDist)
                {
                    bestGrab = g;
                    bestGrabDist = _hits[i].distance;
                }
            }

            clickable = bestClick;
            grabbable = bestGrab;

            if (showDebugRay)
            {
                Debug.DrawRay(ray.origin, ray.direction * maxDistance, Color.cyan, 0.35f);
                if (clickable != null)
                    Debug.Log($"[DesktopRay] click -> {clickable.TargetId}");
                else if (grabbable != null)
                    Debug.Log($"[DesktopRay] grab -> {grabbable.TargetId}");
            }

            return clickable != null || grabbable != null;
        }

        private Ray GetAimRay()
        {
            // RMB look / locked cursor → aim by look direction (FPS crosshair).
            // Free cursor → aim by mouse pointer.
            if (IsLookMode())
                return rayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

            return rayCamera.ScreenPointToRay(GetMousePosition());
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

            var results = new System.Collections.Generic.List<RaycastResult>();
            var eventData = new PointerEventData(EventSystem.current) { position = GetMousePosition() };
            EventSystem.current.RaycastAll(eventData, results);
            for (var i = 0; i < results.Count; i++)
            {
                var canvas = results[i].gameObject.GetComponentInParent<Canvas>();
                if (canvas != null && canvas.renderMode != RenderMode.WorldSpace)
                    return true;
            }

            return false;
        }
    }
}
