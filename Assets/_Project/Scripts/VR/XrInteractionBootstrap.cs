using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using VRTraining.Interaction;

namespace VRTraining.VR
{
    /// <summary>
    /// Lightweight XR readiness: when an XR display is running, spawn a head-anchored
    /// ray interactor that forwards select to Clickable/Grabbable (mouse path unchanged).
    /// </summary>
    public class XrInteractionBootstrap : MonoBehaviour
    {
        [SerializeField] private bool spawnWhenXrDetected = true;
        [SerializeField] private float rayLength = 8f;

        private bool _spawned;

        private void Start()
        {
            if (!spawnWhenXrDetected || _spawned)
                return;

            if (!IsXrLikelyActive())
                return;

            SpawnControllerRays();
            _spawned = true;
        }

        private static bool IsXrLikelyActive()
        {
            var displays = new System.Collections.Generic.List<UnityEngine.XR.XRDisplaySubsystem>();
            SubsystemManager.GetSubsystems(displays);
            for (var i = 0; i < displays.Count; i++)
            {
                if (displays[i] != null && displays[i].running)
                    return true;
            }

            return false;
        }

        private void SpawnControllerRays()
        {
            var cam = Camera.main;
            if (cam == null)
                return;

            var rayGo = new GameObject("XR_FallbackRay");
            rayGo.transform.SetParent(cam.transform, false);
            rayGo.transform.localPosition = new Vector3(0.15f, -0.05f, 0.1f);

            var interactor = rayGo.AddComponent<XRRayInteractor>();
            interactor.maxRaycastDistance = rayLength;
            rayGo.AddComponent<XrRayInteractableBridge>();
            Debug.Log("[XR] Fallback ray interactor spawned (headset detected).");
        }
    }

    /// <summary>
    /// Forwards XRRayInteractor select to existing desktop interactable APIs.
    /// </summary>
    public class XrRayInteractableBridge : MonoBehaviour
    {
        [SerializeField] private XRRayInteractor rayInteractor;
        [SerializeField] private float activateCooldown = 0.25f;

        private float _nextAllowed;

        private void Awake()
        {
            if (rayInteractor == null)
                rayInteractor = GetComponent<XRRayInteractor>();
        }

        private void Update()
        {
            if (rayInteractor == null || Time.time < _nextAllowed)
                return;

            if (!rayInteractor.isSelectActive)
                return;

            if (!rayInteractor.TryGetCurrent3DRaycastHit(out var hit))
                return;

            _nextAllowed = Time.time + activateCooldown;

            var click = hit.collider.GetComponentInParent<ClickableInteractable>();
            if (click != null)
            {
                click.NotifyClicked();
                return;
            }

            var grab = hit.collider.GetComponentInParent<GrabbableInteractable>();
            if (grab != null)
            {
                grab.NotifyGrabbed();
                return;
            }

            var relay = hit.collider.GetComponentInParent<InteractionEventRelay>();
            if (relay != null)
            {
                relay.NotifyClick();
                relay.NotifyGrab();
            }
        }
    }
}
