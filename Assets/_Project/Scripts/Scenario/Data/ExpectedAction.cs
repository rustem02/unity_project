using System;
using UnityEngine;

namespace VRTraining.Scenario.Data
{
    /// <summary>
    /// One expected player action inside a step (zone / grab / click / UI).
    /// </summary>
    [Serializable]
    public class ExpectedAction
    {
        [Tooltip("Type of interaction the player must perform.")]
        public ActionType ActionType;

        [Tooltip("Stable id of the scene target (zone, object, or UI button).")]
        public string TargetId;

        public bool Matches(ActionType actionType, string targetId)
        {
            return ActionType == actionType
                   && string.Equals(TargetId, targetId, StringComparison.OrdinalIgnoreCase);
        }
    }
}
