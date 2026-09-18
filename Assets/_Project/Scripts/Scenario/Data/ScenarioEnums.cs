namespace VRTraining.Scenario.Data
{
    /// <summary>
    /// Supported player actions defined by the specification.
    /// </summary>
    public enum ActionType
    {
        ReachZone = 0,
        Grab = 1,
        Click = 2,
        PressUIButton = 3
    }

    public enum StepStatus
    {
        Pending = 0,
        Success = 1,
        Failed = 2,
        Skipped = 3
    }
}
