using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace VRTraining.Scenario.Data
{
    /// <summary>
    /// Ordered block of related steps (e.g. "Document check").
    /// </summary>
    [Serializable]
    public class StepGroupDefinition
    {
        public string Title;
        [TextArea(2, 5)] public string InfoMessage;
        public List<StepDefinition> Steps = new List<StepDefinition>();

        public string BuildOrderHint()
        {
            if (!string.IsNullOrWhiteSpace(InfoMessage))
                return InfoMessage;

            var builder = new StringBuilder();
            builder.AppendLine($"Группа: {Title}");
            for (var i = 0; i < Steps.Count; i++)
            {
                builder.AppendLine($"{i + 1}. {Steps[i].Description}");
            }

            return builder.ToString().TrimEnd();
        }
    }
}
