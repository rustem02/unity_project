using System.Collections.Generic;
using VRTraining.Scenario.Data;

namespace VRTraining.Scenario.Runtime
{
    /// <summary>
    /// Runtime state for an ordered group of steps.
    /// </summary>
    public class GroupRuntimeState
    {
        public StepGroupDefinition Definition { get; }
        public List<StepRuntimeState> Steps { get; } = new List<StepRuntimeState>();
        public int CurrentStepIndex { get; private set; }
        public bool IsClosed { get; private set; }

        public GroupRuntimeState(StepGroupDefinition definition)
        {
            Definition = definition;
            for (var i = 0; i < definition.Steps.Count; i++)
                Steps.Add(new StepRuntimeState(definition.Steps[i]));
        }

        public StepRuntimeState CurrentStep =>
            CurrentStepIndex >= 0 && CurrentStepIndex < Steps.Count ? Steps[CurrentStepIndex] : null;

        public bool IsCompleted
        {
            get
            {
                if (IsClosed)
                    return true;

                for (var i = 0; i < Steps.Count; i++)
                {
                    if (!Steps[i].IsTerminal)
                        return false;
                }

                return true;
            }
        }

        public void AdvanceAfterCurrentResolved()
        {
            CurrentStepIndex++;
        }

        public void CloseWithSkipRemaining()
        {
            IsClosed = true;
            for (var i = 0; i < Steps.Count; i++)
            {
                if (Steps[i].Status == StepStatus.Pending)
                    Steps[i].SetStatus(StepStatus.Skipped);
            }
        }
    }
}
