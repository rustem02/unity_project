using UnityEngine;
using VRTraining.Core.Interfaces;
using VRTraining.Scenario.Data;
using VRTraining.VR;

namespace VRTraining.Interaction
{
    /// <summary>
    /// Point of interest: entering the trigger reports ReachZone.
    /// Wrong zones still publish — evaluator decides failure vs ignore.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class InteractionZone : MonoBehaviour, IHighlightable
    {
        [SerializeField] private string targetId = "zone";
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private bool triggerOncePerVisit = true;

        private bool _occupied;

        public string TargetId => targetId;

        private void Reset()
        {
            var col = GetComponent<Collider>();
            col.isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!IsPlayer(other) || (_occupied && triggerOncePerVisit))
                return;

            _occupied = true;
            ActionPublisher.Publish(ActionType.ReachZone, targetId);
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsPlayer(other))
                _occupied = false;
        }

        private bool IsPlayer(Collider other)
        {
            return other.CompareTag(playerTag) || other.GetComponentInParent<PlayerMarker>() != null;
        }

        public void SetHighlighted(bool highlighted)
        {
            // Visual handled by OutlineHighlighter on the same object / child.
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 0.7f, 1f, 0.35f);
            var col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawCube(box.center, box.size);
            }
            else
            {
                Gizmos.DrawSphere(transform.position, 0.5f);
            }
        }
#endif
    }
}
