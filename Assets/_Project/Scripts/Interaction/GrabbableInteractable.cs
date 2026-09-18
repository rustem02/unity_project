using UnityEngine;
using VRTraining.Core.Interfaces;
using VRTraining.Scenario.Data;

namespace VRTraining.Interaction
{
    /// <summary>
    /// Grab interactable. Works with XR grab events and a desktop fallback (E / click).
    /// </summary>
    public class GrabbableInteractable : MonoBehaviour, IHighlightable
    {
        [SerializeField] private string targetId = "object";
        [SerializeField] private bool publishOnSelectEntered = true;
        [SerializeField] private Rigidbody body;

        private bool _published;

        public string TargetId => targetId;

        private void Awake()
        {
            if (body == null)
                body = GetComponent<Rigidbody>();
        }

        /// <summary>Called by XR select or desktop grab proxy.</summary>
        public void NotifyGrabbed()
        {
            if (_published && publishOnSelectEntered)
                return;

            _published = true;
            ActionPublisher.Publish(ActionType.Grab, targetId);
        }

        public void ResetGrabFlag()
        {
            _published = false;
        }

        public void SetHighlighted(bool highlighted) { }
    }
}
