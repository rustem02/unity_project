using UnityEngine;
using VRTraining.Core.Interfaces;
using VRTraining.Scenario.Data;

namespace VRTraining.Interaction
{
    /// <summary>
    /// Object that reacts to pointer / ray click (XR interactor or mouse raycast).
    /// </summary>
    public class ClickableInteractable : MonoBehaviour, IHighlightable
    {
        [SerializeField] private string targetId = "clickable";
        [SerializeField] private bool oneShot = true;

        private bool _used;

        public string TargetId => targetId;

        public void NotifyClicked()
        {
            if (_used && oneShot)
                return;

            _used = true;
            ActionPublisher.Publish(ActionType.Click, targetId);
        }

        public void ResetClick()
        {
            _used = false;
        }

        public void SetHighlighted(bool highlighted) { }
    }
}
