using UnityEngine;
using VRTraining.Core.Events;
using VRTraining.Interaction;

namespace VRTraining.Scenario.Runtime
{
    /// <summary>
    /// Resets one-shot interactables when a scenario restarts / group starts fresh.
    /// </summary>
    public class InteractableResetService : MonoBehaviour
    {
        private void OnEnable()
        {
            EventBus.Subscribe<GroupActivatedEvent>(OnGroupActivated);
            EventBus.Subscribe<ScenarioCompletedEvent>(_ => { /* keep state for results */ });
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GroupActivatedEvent>(OnGroupActivated);
        }

        /// <summary>Call from ResultsPanel restart path.</summary>
        public void ResetAllInteractables()
        {
            var grabs = FindObjectsByType<GrabbableInteractable>();
            for (var i = 0; i < grabs.Length; i++)
                grabs[i].ResetGrabFlag();

            var clicks = FindObjectsByType<ClickableInteractable>();
            for (var i = 0; i < clicks.Length; i++)
                clicks[i].ResetClick();
        }

        private void OnGroupActivated(GroupActivatedEvent evt)
        {
            if (evt.GroupIndex == 0)
                ResetAllInteractables();
        }
    }
}
