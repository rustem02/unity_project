using TMPro;
using UnityEngine;
using VRTraining.Core.Events;
using VRTraining.Scenario.Data;

namespace VRTraining.UI
{
    /// <summary>
    /// Compact HUD listing step statuses for the active group.
    /// </summary>
    public class StepProgressHud : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI hudText;

        private string _groupTitle = string.Empty;
        private readonly System.Collections.Generic.Dictionary<string, StepStatus> _statuses =
            new System.Collections.Generic.Dictionary<string, StepStatus>();

        private void OnEnable()
        {
            EventBus.Subscribe<GroupActivatedEvent>(OnGroup);
            EventBus.Subscribe<StepResolvedEvent>(OnStep);
            EventBus.Subscribe<ScenarioCompletedEvent>(_ =>
            {
                if (hudText != null) hudText.text = string.Empty;
            });
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GroupActivatedEvent>(OnGroup);
            EventBus.Unsubscribe<StepResolvedEvent>(OnStep);
        }

        private void OnGroup(GroupActivatedEvent evt)
        {
            _groupTitle = evt.GroupTitle;
            _statuses.Clear();
            Refresh();
        }

        private void OnStep(StepResolvedEvent evt)
        {
            _statuses[evt.StepId] = evt.Status;
            Refresh();
        }

        private void Refresh()
        {
            if (hudText == null)
                return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(_groupTitle);
            foreach (var pair in _statuses)
                sb.AppendLine($"• {pair.Key}: {pair.Value}");
            hudText.text = sb.ToString();
        }
    }
}
