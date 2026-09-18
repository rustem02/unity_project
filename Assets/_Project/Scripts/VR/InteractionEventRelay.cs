using UnityEngine;
using VRTraining.Interaction;

namespace VRTraining.VR
{
    /// <summary>
    /// UnityEvent-friendly relay for XR Interaction Toolkit (or any other input).
    /// Wire XRGrabInteractable.selectEntered → NotifyGrab, activate → NotifyClick.
    /// Keeps scenario/interaction code free of XRI assembly references.
    /// </summary>
    public class InteractionEventRelay : MonoBehaviour
    {
        [SerializeField] private GrabbableInteractable grabbable;
        [SerializeField] private ClickableInteractable clickable;

        private void Awake()
        {
            if (grabbable == null)
                grabbable = GetComponent<GrabbableInteractable>();
            if (clickable == null)
                clickable = GetComponent<ClickableInteractable>();
        }

        public void NotifyGrab()
        {
            grabbable?.NotifyGrabbed();
        }

        public void NotifyClick()
        {
            clickable?.NotifyClicked();
        }
    }
}
