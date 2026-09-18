namespace VRTraining.Core.Interfaces
{
    /// <summary>
    /// Objects that can be visually emphasized for the active scenario step.
    /// Implement with Outline / Highlight Plus adapters without changing scenario code.
    /// </summary>
    public interface IHighlightable
    {
        string TargetId { get; }
        void SetHighlighted(bool highlighted);
    }
}
