using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRTraining.Interaction
{
    /// <summary>
    /// Desktop mouse/keyboard interactions for click + grab.
    /// Uses camera→mouse ray; does not block on world-space HUD canvases.
    /// </summary>
    public class DesktopInteractionRay : MonoBehaviour
    {
        [SerializeField] private Camera rayCamera;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private KeyCode grabKey = KeyCode.E;
        [SerializeField] private bool debugLogs;

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
                TryClick();

            if (WasPressedThisFrame(grabKey))
                TryGrab();
        }

        private void TryClick()
        {
            if (!Physics.Raycast(GetRay(), out var hit, maxDistance, interactableMask, QueryTriggerInteraction.Ignore))
            {
                if (debugLogs)
                    Debug.Log("[DesktopRay] Click miss");
                return;
            }

            var clickable = hit.collider.GetComponentInParent<ClickableInteractable>();
            if (clickable != null)
            {
                if (debugLogs)
                    Debug.Log($"[DesktopRay] Click -> {clickable.TargetId}");
                clickable.NotifyClicked();
            }
        }

        private void TryGrab()
        {
            if (!Physics.Raycast(GetRay(), out var hit, maxDistance, interactableMask, QueryTriggerInteraction.Ignore))
            {
                if (debugLogs)
                    Debug.Log("[DesktopRay] Grab miss");
                return;
            }

            var grabbable = hit.collider.GetComponentInParent<GrabbableInteractable>();
            if (grabbable != null)
            {
                if (debugLogs)
                    Debug.Log($"[DesktopRay] Grab -> {grabbable.TargetId}");
                grabbable.NotifyGrabbed();
            }
        }

        private Ray GetRay()
        {
            return rayCamera.ScreenPointToRay(GetMousePosition());
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

        /// <summary>
        /// Only treat overlay/screen UI as blocking. World-space training canvases must not kill gameplay rays.
        /// </summary>
        private static bool IsBlockingScreenUi()
        {
            if (EventSystem.current == null)
                return false;

            if (!EventSystem.current.IsPointerOverGameObject())
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
