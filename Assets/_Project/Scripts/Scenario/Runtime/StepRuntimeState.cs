using System.Collections.Generic;
using VRTraining.Scenario.Data;

namespace VRTraining.Scenario.Runtime
{
    /// <summary>
    /// Mutable runtime state for a single step.
    /// </summary>
    public class StepRuntimeState
    {
        public StepDefinition Definition { get; }
        public StepStatus Status { get; private set; } = StepStatus.Pending;
        public HashSet<int> CompletedActionIndices { get; } = new HashSet<int>();

        public StepRuntimeState(StepDefinition definition)
        {
            Definition = definition;
        }

        public bool IsTerminal => Status != StepStatus.Pending;

        public bool TryMarkActionCompleted(ActionType actionType, string targetId)
        {
            for (var i = 0; i < Definition.ExpectedActions.Count; i++)
            {
                if (!Definition.ExpectedActions[i].Matches(actionType, targetId))
                    continue;

                CompletedActionIndices.Add(i);
                return true;
            }

            return false;
        }

        public bool AreAllActionsCompleted()
        {
            return CompletedActionIndices.Count >= Definition.ExpectedActions.Count;
        }

        public void SetStatus(StepStatus status)
        {
            Status = status;
        }
    }
}
