using System;
using System.Collections.Generic;

namespace VRTraining.Scenario.Data
{
    [Serializable]
    public class StepResult
    {
        public string GroupTitle;
        public string StepId;
        public string Description;
        public StepStatus Status;
    }

    [Serializable]
    public class ScenarioResult
    {
        public string ScenarioName;
        public List<StepResult> Steps = new List<StepResult>();

        public int SuccessCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Steps.Count; i++)
                {
                    if (Steps[i].Status == StepStatus.Success)
                        count++;
                }

                return count;
            }
        }

        public int FailedCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Steps.Count; i++)
                {
                    if (Steps[i].Status == StepStatus.Failed)
                        count++;
                }

                return count;
            }
        }

        public int SkippedCount
        {
            get
            {
                var count = 0;
                for (var i = 0; i < Steps.Count; i++)
                {
                    if (Steps[i].Status == StepStatus.Skipped)
                        count++;
                }

                return count;
            }
        }
    }
}
