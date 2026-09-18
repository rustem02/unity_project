using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRTraining.Interaction
{
    /// <summary>
    /// Desktop mouse/keyboard interactions for click + grab.
    /// When cursor is locked (look mode), aims from screen center (look direction).
    /// </summary>
    public class DesktopInteractionRay : MonoBehaviour
    {
        [SerializeField] private Camera rayCamera;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private LayerMask interactableMask = ~0;
        [SerializeField] private KeyCode grabKey = KeyCode.E;

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
                return;

            var clickable = hit.collider.GetComponentInParent<ClickableInteractable>();
            if (clickable != null)
            {
                clickable.NotifyClicked();
                return;
            }

            // Convenience: LMB also grabs when pointing at a grabbable.
            var grabbable = hit.collider.GetComponentInParent<GrabbableInteractable>();
            grabbable?.NotifyGrabbed();
        }

        private void TryGrab()
        {
            if (!Physics.Raycast(GetRay(), out var hit, maxDistance, interactableMask, QueryTriggerInteraction.Ignore))
                return;

            var grabbable = hit.collider.GetComponentInParent<GrabbableInteractable>();
            grabbable?.NotifyGrabbed();
        }

        private Ray GetRay()
        {
            // Locked cursor → FPS crosshair (look direction). Free cursor → mouse pointer.
            if (Cursor.lockState == CursorLockMode.Locked)
                return rayCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

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
