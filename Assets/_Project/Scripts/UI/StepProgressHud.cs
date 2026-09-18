using UnityEngine;
using UnityEngine.UI;
using VRTraining.Core.Events;
using VRTraining.Scenario.Data;

namespace VRTraining.UI
{
    /// <summary>
    /// Compact HUD listing step statuses for the active group.
    /// </summary>
    public class StepProgressHud : MonoBehaviour
    {
        [SerializeField] private Text hudText;

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
            if (!string.IsNullOrEmpty(evt.InfoMessage))
            {
                // Seed pending lines from info so HUD is useful before first resolve.
                var lines = evt.InfoMessage.Split('\n');
                for (var i = 0; i < lines.Length; i++)
                {
                    var line = lines[i].Trim();
                    if (line.Length > 0)
                        _statuses["hint_" + i] = StepStatus.Pending;
                }
            }

            Refresh(evt.InfoMessage);
        }

        private void OnStep(StepResolvedEvent evt)
        {
            _statuses[evt.StepId] = evt.Status;
            Refresh();
        }

        private void Refresh(string infoOverride = null)
        {
            if (hudText == null)
                return;

            var sb = new System.Text.StringBuilder();
            sb.AppendLine(_groupTitle);
            if (!string.IsNullOrEmpty(infoOverride))
            {
                sb.AppendLine(infoOverride);
            }
            else
            {
                foreach (var pair in _statuses)
                {
                    if (pair.Key.StartsWith("hint_"))
                        continue;
                    sb.AppendLine($"* {pair.Key}: {StatusLabel(pair.Value)}");
                }
            }

            UiFontBootstrap.EnsureCharacters(hudText, sb.ToString());
        }

        private static string StatusLabel(StepStatus status)
        {
            switch (status)
            {
                case StepStatus.Success: return "OK";
                case StepStatus.Failed: return "FAIL";
                case StepStatus.Skipped: return "SKIP";
                default: return "...";
            }
        }
    }
}
