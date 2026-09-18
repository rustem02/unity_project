using UnityEngine;
using VRTraining.Core.Events;
using VRTraining.Scenario.Data;

namespace VRTraining.Interaction
{
    /// <summary>
    /// Shared helper that publishes player actions to the EventBus.
    /// </summary>
    public static class ActionPublisher
    {
        public static void Publish(ActionType actionType, string targetId)
        {
            if (string.IsNullOrWhiteSpace(targetId))
            {
                Debug.LogWarning("[ActionPublisher] Empty targetId ignored.");
                return;
            }

            EventBus.Publish(new PlayerActionEvent(actionType, targetId.Trim()));
        }
    }
}
