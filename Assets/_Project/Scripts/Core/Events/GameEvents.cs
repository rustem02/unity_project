using VRTraining.Scenario.Data;

namespace VRTraining.Core.Events
{
    /// <summary>
    /// Fired by interaction components when the player performs a concrete action.
    /// </summary>
    public struct PlayerActionEvent
    {
        public ActionType ActionType;
        public string TargetId;
        public bool IsValidSignal;

        public PlayerActionEvent(ActionType actionType, string targetId, bool isValidSignal = true)
        {
            ActionType = actionType;
            TargetId = targetId;
            IsValidSignal = isValidSignal;
        }
    }

    public struct GroupActivatedEvent
    {
        public int GroupIndex;
        public string GroupTitle;
        public string InfoMessage;
    }

    public struct StepResolvedEvent
    {
        public int GroupIndex;
        public int StepIndex;
        public string StepId;
        public StepStatus Status;
    }

    public struct SequenceViolationEvent
    {
        public int GroupIndex;
        public string AttemptedTargetId;
    }

    public struct ScenarioCompletedEvent
    {
        public ScenarioResult Result;
    }

    public struct FeedbackRequestEvent
    {
        public FeedbackKind Kind;
    }

    public enum FeedbackKind
    {
        Success,
        Failure,
        SequenceViolation,
        GroupStart
    }
}
