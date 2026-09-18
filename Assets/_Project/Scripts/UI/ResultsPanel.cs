using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRTraining.Core.Events;
using VRTraining.Scenario.Data;
using VRTraining.Scenario.Runtime;

namespace VRTraining.UI
{
    /// <summary>
    /// End-of-scenario results: step list + Restart / Return to Lobby.
    /// </summary>
    public class ResultsPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI summaryText;
        [SerializeField] private TextMeshProUGUI detailsText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button lobbyButton;
        [SerializeField] private ScenarioController scenarioController;
        [SerializeField] private string lobbySceneName = "Lobby";

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            SetVisible(false);
        }

        private void OnEnable()
        {
            EventBus.Subscribe<ScenarioCompletedEvent>(OnScenarioCompleted);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestart);
            if (lobbyButton != null)
                lobbyButton.onClick.AddListener(OnLobby);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<ScenarioCompletedEvent>(OnScenarioCompleted);
            if (restartButton != null)
                restartButton.onClick.RemoveListener(OnRestart);
            if (lobbyButton != null)
                lobbyButton.onClick.RemoveListener(OnLobby);
        }

        private void OnScenarioCompleted(ScenarioCompletedEvent evt)
        {
            Render(evt.Result);
            SetVisible(true);
        }

        private void Render(ScenarioResult result)
        {
            if (summaryText != null)
            {
                summaryText.text =
                    $"Итог: ✓ {result.SuccessCount}  ✗ {result.FailedCount}  → {result.SkippedCount}";
            }

            if (detailsText == null)
                return;

            var sb = new StringBuilder();
            string lastGroup = null;
            for (var i = 0; i < result.Steps.Count; i++)
            {
                var step = result.Steps[i];
                if (step.GroupTitle != lastGroup)
                {
                    lastGroup = step.GroupTitle;
                    sb.AppendLine();
                    sb.AppendLine($"[{lastGroup}]");
                }

                sb.AppendLine($"  {StatusGlyph(step.Status)}  {step.Description}");
            }

            detailsText.text = sb.ToString().TrimStart();
        }

        private static string StatusGlyph(StepStatus status)
        {
            switch (status)
            {
                case StepStatus.Success: return "✓ успех";
                case StepStatus.Failed: return "✗ ошибка";
                case StepStatus.Skipped: return "→ пропущен";
                default: return "? ожидание";
            }
        }

        private void OnRestart()
        {
            SetVisible(false);
            if (scenarioController != null)
            {
                scenarioController.RestartScenario();
                return;
            }

            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        private void OnLobby()
        {
            SceneManager.LoadScene(lobbySceneName);
        }

        private void SetVisible(bool visible)
        {
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }
    }
}
