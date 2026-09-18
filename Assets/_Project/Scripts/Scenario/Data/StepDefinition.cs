using System;
using System.Collections.Generic;
using UnityEngine;

namespace VRTraining.Scenario.Data
{
    /// <summary>
    /// A single scenario step. May require one or several expected actions.
    /// </summary>
    [Serializable]
    public class StepDefinition
    {
        public string Id;
        [TextArea(2, 4)] public string Description;
        public List<ExpectedAction> ExpectedActions = new List<ExpectedAction>();

        public bool ContainsAction(ActionType actionType, string targetId)
        {
            for (var i = 0; i < ExpectedActions.Count; i++)
            {
                if (ExpectedActions[i].Matches(actionType, targetId))
                    return true;
            }

            return false;
        }
    }
}
