using VRTraining.Scenario.Data;

namespace VRTraining.Core.Events
{
    public struct HighlightTargetInfo
    {
        public string TargetId;
        public ActionType ActionType;
    }

    /// <summary>
    /// Broadcast when the set of currently expected / highlighted targets changes.
    /// </summary>
    public struct HighlightTargetsChangedEvent
    {
        public int GroupIndex;
        public int StepIndex;
        public HighlightTargetInfo[] Targets;
    }
}
