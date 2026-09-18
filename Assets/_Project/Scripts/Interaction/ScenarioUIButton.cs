using UnityEngine;
using UnityEngine.UI;
using VRTraining.Core.Interfaces;
using VRTraining.Scenario.Data;

namespace VRTraining.Interaction
{
    /// <summary>
    /// UI button that reports PressUIButton with a stable target id.
    /// Works with mouse and XR UI ray interactors.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ScenarioUIButton : MonoBehaviour, IHighlightable
    {
        [SerializeField] private string targetId = "ui_button";
        [SerializeField] private Button button;

        public string TargetId => targetId;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
        }

        private void OnEnable()
        {
            button.onClick.AddListener(OnClicked);
        }

        private void OnDisable()
        {
            button.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            ActionPublisher.Publish(ActionType.PressUIButton, targetId);
        }

        public void SetHighlighted(bool highlighted) { }
    }
}
