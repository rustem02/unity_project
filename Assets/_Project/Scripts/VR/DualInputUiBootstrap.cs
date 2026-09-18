using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VRTraining.VR
{
    /// <summary>
    /// Ensures UI works with both mouse (StandaloneInputModule) and XR ray (TrackedDeviceGraphicRaycaster).
    /// Adds missing pieces at runtime so scenes stay resilient.
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
            if (eventSystem.GetComponent<StandaloneInputModule>() == null)
                eventSystem.gameObject.AddComponent<StandaloneInputModule>();
        }

        private void EnsureWorldCanvases()
        {
            var canvases = FindObjectsByType<Canvas>();
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
