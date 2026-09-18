using System.Collections.Generic;
using UnityEngine;
using VRTraining.Core.Events;
using VRTraining.Scenario.Data;

namespace VRTraining.Scenario.Runtime
{
    /// <summary>
    /// Orchestrates groups and steps. Listens to <see cref="PlayerActionEvent"/> only —
    /// no hard references to interaction MonoBehaviours (DIP / event-driven).
    /// </summary>
    public class ScenarioController : MonoBehaviour
    {
        [SerializeField] private ScenarioDefinition scenario;
        [SerializeField] private bool autoStart = true;

        private readonly List<GroupRuntimeState> _groups = new List<GroupRuntimeState>();
        private readonly StepActionEvaluator _evaluator = new StepActionEvaluator();
        private readonly ScenarioResult _result = new ScenarioResult();

        private int _groupIndex;
        private bool _isRunning;

        public ScenarioDefinition Scenario => scenario;
        public GroupRuntimeState ActiveGroup =>
            _groupIndex >= 0 && _groupIndex < _groups.Count ? _groups[_groupIndex] : null;

        public bool IsRunning => _isRunning;

        private void Awake()
        {
            // Builder sometimes fails to serialize SO refs into a brand-new scene YAML.
            // Resources fallback keeps Play Mode / builds working.
            if (scenario == null)
            {
                scenario = Resources.Load<ScenarioDefinition>("TrainingScenario");
                if (scenario == null)
                    Debug.LogWarning("[ScenarioController] No scenario assigned and Resources/TrainingScenario missing.");
            }
        }

        private void OnEnable()
        {
            EventBus.Subscribe<PlayerActionEvent>(OnPlayerAction);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<PlayerActionEvent>(OnPlayerAction);
        }

        private void Start()
        {
            if (autoStart)
                BeginScenario();
        }

        public void BeginScenario()
        {
            if (scenario == null)
            {
                Debug.LogError("[ScenarioController] ScenarioDefinition is not assigned.");
                return;
            }

            _groups.Clear();
            _result.Steps.Clear();
            _result.ScenarioName = scenario.ScenarioName;
            _groupIndex = 0;
            _isRunning = true;

            for (var i = 0; i < scenario.Groups.Count; i++)
                _groups.Add(new GroupRuntimeState(scenario.Groups[i]));

            ActivateCurrentGroup();
        }

        public void RestartScenario()
        {
            BeginScenario();
        }

        private void OnPlayerAction(PlayerActionEvent action)
        {
            if (!_isRunning || ActiveGroup == null)
                return;

            var evaluation = _evaluator.Evaluate(ActiveGroup, action.ActionType, action.TargetId);
            switch (evaluation.Kind)
            {
                case StepActionEvaluator.EvaluationKind.CurrentActionProgress:
                    // Partial multi-action step — wait for remaining actions.
                    break;

                case StepActionEvaluator.EvaluationKind.CurrentStepSuccess:
                    ResolveCurrentStep(StepStatus.Success);
                    break;

                case StepActionEvaluator.EvaluationKind.CurrentStepFailure:
                    ResolveCurrentStep(StepStatus.Failed);
                    break;

                case StepActionEvaluator.EvaluationKind.SequenceViolation:
                    HandleSequenceViolation(action.TargetId);
                    break;
            }
        }

        private void ResolveCurrentStep(StepStatus status)
        {
            var group = ActiveGroup;
            var step = group.CurrentStep;
            if (step == null || step.IsTerminal)
                return;

            step.SetStatus(status);
            PublishStepResolved(group, group.CurrentStepIndex, step);

            EventBus.Publish(new FeedbackRequestEvent
            {
                Kind = status == StepStatus.Success ? FeedbackKind.Success : FeedbackKind.Failure
            });

            group.AdvanceAfterCurrentResolved();

            if (group.IsCompleted)
                AdvanceToNextGroup();
            else
                RefreshHighlightsForActiveStep();
        }

        private void HandleSequenceViolation(string attemptedTargetId)
        {
            var group = ActiveGroup;
            EventBus.Publish(new SequenceViolationEvent
            {
                GroupIndex = _groupIndex,
                AttemptedTargetId = attemptedTargetId
            });

            EventBus.Publish(new FeedbackRequestEvent { Kind = FeedbackKind.SequenceViolation });

            // Preserve statuses of already finished steps; skip the rest.
            group.CloseWithSkipRemaining();
            for (var i = 0; i < group.Steps.Count; i++)
            {
                // Publish only steps that just became skipped / already terminal for UI sync.
                PublishStepResolved(group, i, group.Steps[i]);
            }

            AdvanceToNextGroup();
        }

        private void AdvanceToNextGroup()
        {
            _groupIndex++;
            if (_groupIndex >= _groups.Count)
            {
                FinishScenario();
                return;
            }

            ActivateCurrentGroup();
        }

        private void ActivateCurrentGroup()
        {
            var group = ActiveGroup;
            if (group == null)
            {
                FinishScenario();
                return;
            }

            EventBus.Publish(new GroupActivatedEvent
            {
                GroupIndex = _groupIndex,
                GroupTitle = group.Definition.Title,
                InfoMessage = group.Definition.BuildOrderHint()
            });

            EventBus.Publish(new FeedbackRequestEvent { Kind = FeedbackKind.GroupStart });
            RefreshHighlightsForActiveStep();
        }

        private void FinishScenario()
        {
            _isRunning = false;
            BuildFinalResult();
            EventBus.Publish(new ScenarioCompletedEvent { Result = _result });
        }

        private void BuildFinalResult()
        {
            _result.Steps.Clear();
            for (var g = 0; g < _groups.Count; g++)
            {
                var group = _groups[g];
                for (var s = 0; s < group.Steps.Count; s++)
                {
                    var step = group.Steps[s];
                    _result.Steps.Add(new StepResult
                    {
                        GroupTitle = group.Definition.Title,
                        StepId = step.Definition.Id,
                        Description = step.Definition.Description,
                        Status = step.Status == StepStatus.Pending ? StepStatus.Skipped : step.Status
                    });
                }
            }
        }

        private void PublishStepResolved(GroupRuntimeState group, int stepIndex, StepRuntimeState step)
        {
            EventBus.Publish(new StepResolvedEvent
            {
                GroupIndex = _groupIndex,
                StepIndex = stepIndex,
                StepId = step.Definition.Id,
                Status = step.Status
            });
        }

        private void RefreshHighlightsForActiveStep()
        {
            EventBus.Publish(new HighlightTargetsChangedEvent
            {
                GroupIndex = _groupIndex,
                StepIndex = ActiveGroup?.CurrentStepIndex ?? -1,
                Targets = CollectActiveTargets()
            });
        }

        private HighlightTargetInfo[] CollectActiveTargets()
        {
            var step = ActiveGroup?.CurrentStep;
            if (step == null || step.IsTerminal)
                return System.Array.Empty<HighlightTargetInfo>();

            var list = new List<HighlightTargetInfo>(step.Definition.ExpectedActions.Count);
            for (var i = 0; i < step.Definition.ExpectedActions.Count; i++)
            {
                if (step.CompletedActionIndices.Contains(i))
                    continue;

                var expected = step.Definition.ExpectedActions[i];
                list.Add(new HighlightTargetInfo
                {
                    TargetId = expected.TargetId,
                    ActionType = expected.ActionType
                });
            }

            return list.ToArray();
        }
    }
}
