using System.Collections.Generic;
using UnityEngine;

namespace VRTraining.Scenario.Data
{
    /// <summary>
    /// Authoring asset for a full training scenario (groups → steps → actions).
    /// Data-driven: change content without touching runtime code.
    /// </summary>
    [CreateAssetMenu(fileName = "ScenarioDefinition", menuName = "VR Training/Scenario Definition")]
    public class ScenarioDefinition : ScriptableObject
    {
        public string ScenarioName = "Training";
        public List<StepGroupDefinition> Groups = new List<StepGroupDefinition>();
    }
}
