using System;
using VRTraining.Core.Events;
using VRTraining.Scenario.Data;

namespace VRTraining.Scenario.Runtime
{
    /// <summary>
    /// Pure evaluation rules for player actions against the active group.
    /// Separated from MonoBehaviour for testability and SOLID SRP.
    /// </summary>
    public class StepActionEvaluator
    {
        public enum EvaluationKind
        {
            Ignored,
            CurrentActionProgress,
            CurrentStepSuccess,
            CurrentStepFailure,
            SequenceViolation
        }

        public readonly struct EvaluationResult
        {
            public EvaluationKind Kind { get; }
            public int MatchedFutureStepIndex { get; }

            public EvaluationResult(EvaluationKind kind, int matchedFutureStepIndex = -1)
            {
                Kind = kind;
                MatchedFutureStepIndex = matchedFutureStepIndex;
            }
        }

        public EvaluationResult Evaluate(GroupRuntimeState group, ActionType actionType, string targetId)
        {
            if (group == null || group.IsClosed)
                return new EvaluationResult(EvaluationKind.Ignored);

            var current = group.CurrentStep;
            if (current == null || current.IsTerminal)
                return new EvaluationResult(EvaluationKind.Ignored);

            // Doing a later step's action out of order → sequence violation.
            for (var i = group.CurrentStepIndex + 1; i < group.Steps.Count; i++)
            {
                if (group.Steps[i].Definition.ContainsAction(actionType, targetId))
                    return new EvaluationResult(EvaluationKind.SequenceViolation, i);
            }

            if (current.TryMarkActionCompleted(actionType, targetId))
            {
                return current.AreAllActionsCompleted()
                    ? new EvaluationResult(EvaluationKind.CurrentStepSuccess)
                    : new EvaluationResult(EvaluationKind.CurrentActionProgress);
            }

            // Wrong target for the same action family as the current step → fail step.
            if (IsConflictingAction(current, actionType))
                return new EvaluationResult(EvaluationKind.CurrentStepFailure);

            return new EvaluationResult(EvaluationKind.Ignored);
        }

        private static bool IsConflictingAction(StepRuntimeState current, ActionType actionType)
        {
            for (var i = 0; i < current.Definition.ExpectedActions.Count; i++)
            {
                if (current.Definition.ExpectedActions[i].ActionType == actionType)
                    return true;
            }

            return false;
        }
    }
}
