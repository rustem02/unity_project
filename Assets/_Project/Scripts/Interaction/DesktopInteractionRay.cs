using UnityEngine;
using UnityEngine.EventSystems;
using VRTraining.Interaction;

namespace VRTraining.Interaction
{
    /// <summary>
    /// Desktop / mouse ray helper: left-click clickables, E to grab looked-at objects.
    /// Complements XR interactors so the scene is testable without a headset.
    /// </summary>
    public class DesktopInteractionRay : MonoBehaviour
    {
        [SerializeField] private Camera rayCamera;
        [SerializeField] private float maxDistance = 8f;
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
                return;

            // Skip world clicks when pointer is over UI.
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (Input.GetMouseButtonDown(0))
                TryClick();

            if (Input.GetKeyDown(grabKey))
                TryGrab();
        }

        private void TryClick()
        {
            if (!Physics.Raycast(GetRay(), out var hit, maxDistance, interactableMask))
                return;

            var clickable = hit.collider.GetComponentInParent<ClickableInteractable>();
            clickable?.NotifyClicked();
        }

        private void TryGrab()
        {
            if (!Physics.Raycast(GetRay(), out var hit, maxDistance, interactableMask))
                return;

            var grabbable = hit.collider.GetComponentInParent<GrabbableInteractable>();
            grabbable?.NotifyGrabbed();
        }

        private Ray GetRay()
        {
            return rayCamera.ScreenPointToRay(Input.mousePosition);
        }
    }
}
