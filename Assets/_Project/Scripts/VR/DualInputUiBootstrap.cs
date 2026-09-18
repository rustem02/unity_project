using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif

namespace VRTraining.VR
{
    /// <summary>
    /// Ensures UI works with mouse and (when present) Input System UI module.
    /// World-space canvases get GraphicRaycaster so buttons stay clickable.
    /// </summary>
    public class DualInputUiBootstrap : MonoBehaviour
    {
        [SerializeField] private EventSystem eventSystem;

        private void Awake()
        {
            if (eventSystem == null)
                eventSystem = FindAnyObjectByType<EventSystem>();

            if (eventSystem == null)
            {
                var go = new GameObject("EventSystem");
                eventSystem = go.AddComponent<EventSystem>();
            }

            EnsureMouseModule();
            EnsureWorldCanvases();
        }

        private void EnsureMouseModule()
        {
#if ENABLE_INPUT_SYSTEM
            // Prefer Input System UI module when the new Input System is active.
            var legacy = eventSystem.GetComponent<StandaloneInputModule>();
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                var inputModule = eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                inputModule.AssignDefaultActions();
            }

            if (legacy != null)
                legacy.enabled = false;
#else
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
#endif
        }

        private void EnsureWorldCanvases()
        {
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas.renderMode != RenderMode.WorldSpace)
                    continue;

                if (canvas.GetComponent<GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }
}
