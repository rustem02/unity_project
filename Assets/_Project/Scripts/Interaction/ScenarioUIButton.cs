using UnityEngine;
using UnityEngine.UI;
using VRTraining.Core.Interfaces;
using VRTraining.Scenario.Data;

namespace VRTraining.Interaction
{
    /// <summary>
    /// UI button that reports PressUIButton with a stable target id.
    /// Supports mouse, look-aim LMB, and XR UI rays. Highlights when expected.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class ScenarioUIButton : MonoBehaviour, IHighlightable
    {
        [SerializeField] private string targetId = "ui_button";
        [SerializeField] private Button button;
        [SerializeField] private Image targetImage;
        [SerializeField] private Color normalColor = new Color(0.2f, 0.45f, 0.75f, 1f);
        [SerializeField] private Color highlightColor = new Color(0.25f, 0.9f, 0.55f, 1f);

        public string TargetId => targetId;

        private void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (targetImage == null)
                targetImage = GetComponent<Image>();
            if (targetImage != null)
                normalColor = targetImage.color;
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

        public void SetHighlighted(bool highlighted)
        {
            if (targetImage == null)
                targetImage = GetComponent<Image>();
            if (targetImage != null)
                targetImage.color = highlighted ? highlightColor : normalColor;
        }
    }
}
