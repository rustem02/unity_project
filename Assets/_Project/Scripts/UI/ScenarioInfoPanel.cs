using UnityEngine;
using UnityEngine.UI;
using VRTraining.Core.Events;

namespace VRTraining.UI
{
    /// <summary>
    /// Shows group intro / expected action order. Large fonts for VR readability.
    /// </summary>
    public class ScenarioInfoPanel : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Text titleText;
        [SerializeField] private Text bodyText;
        [SerializeField] private float visibleSeconds = 8f;
        [SerializeField] private float fadeSeconds = 0.4f;

        private float _hideAt;
        private bool _visible;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            HideImmediate();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GroupActivatedEvent>(OnGroupActivated);
            EventBus.Subscribe<ScenarioCompletedEvent>(_ => HideImmediate());
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<GroupActivatedEvent>(OnGroupActivated);
        }

        private void Update()
        {
            if (!_visible)
                return;

            if (Time.time >= _hideAt)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 0f, Time.deltaTime / fadeSeconds);
                if (canvasGroup.alpha <= 0.01f)
                    HideImmediate();
            }
            else if (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha = Mathf.MoveTowards(canvasGroup.alpha, 1f, Time.deltaTime / fadeSeconds);
            }
        }

        private void OnGroupActivated(GroupActivatedEvent evt)
        {
            if (titleText != null)
                UiFontBootstrap.EnsureCharacters(titleText, evt.GroupTitle);

            if (bodyText != null)
                UiFontBootstrap.EnsureCharacters(bodyText, evt.InfoMessage);

            _visible = true;
            _hideAt = Time.time + visibleSeconds;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        private void HideImmediate()
        {
            _visible = false;
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                canvasGroup.interactable = false;
            }
        }
    }
}
