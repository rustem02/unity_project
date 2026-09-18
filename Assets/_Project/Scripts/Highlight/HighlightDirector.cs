using System.Collections.Generic;
using UnityEngine;
using VRTraining.Core.Events;
using VRTraining.Core.Interfaces;

namespace VRTraining.Highlight
{
    /// <summary>
    /// Central highlight director: turns outlines on/off from scenario events.
    /// </summary>
    public class HighlightDirector : MonoBehaviour
    {
        private readonly Dictionary<string, List<IHighlightable>> _byId =
            new Dictionary<string, List<IHighlightable>>(System.StringComparer.OrdinalIgnoreCase);

        private void OnEnable()
        {
            EventBus.Subscribe<HighlightTargetsChangedEvent>(OnTargetsChanged);
            EventBus.Subscribe<ScenarioCompletedEvent>(OnScenarioCompleted);
            RebuildIndex();
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<HighlightTargetsChangedEvent>(OnTargetsChanged);
            EventBus.Unsubscribe<ScenarioCompletedEvent>(OnScenarioCompleted);
        }

        private void OnScenarioCompleted(ScenarioCompletedEvent _)
        {
            ClearAll();
        }

        public void RebuildIndex()
        {
            _byId.Clear();
            var behaviours = FindObjectsByType<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is not IHighlightable highlightable)
                    continue;

                if (string.IsNullOrWhiteSpace(highlightable.TargetId))
                    continue;

                if (!_byId.TryGetValue(highlightable.TargetId, out var list))
                {
                    list = new List<IHighlightable>();
                    _byId[highlightable.TargetId] = list;
                }

                list.Add(highlightable);
            }
        }

        private void OnTargetsChanged(HighlightTargetsChangedEvent evt)
        {
            ClearAll();
            if (evt.Targets == null)
                return;

            for (var i = 0; i < evt.Targets.Length; i++)
            {
                var id = evt.Targets[i].TargetId;
                if (!_byId.TryGetValue(id, out var list))
                    continue;

                for (var j = 0; j < list.Count; j++)
                    list[j].SetHighlighted(true);
            }
        }

        private void ClearAll()
        {
            foreach (var pair in _byId)
            {
                for (var i = 0; i < pair.Value.Count; i++)
                    pair.Value[i].SetHighlighted(false);
            }
        }
    }
}
