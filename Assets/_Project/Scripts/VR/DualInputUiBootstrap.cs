using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace VRTraining.VR
{
    /// <summary>
    /// Mouse UI module + world-space GraphicRaycasters (+ XR TrackedDeviceGraphicRaycaster).
    /// Skips HUD / hint / label canvases so they never steal gameplay rays.
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
            var cam = Camera.main;
            var canvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (var i = 0; i < canvases.Length; i++)
            {
                var canvas = canvases[i];
                if (canvas.renderMode != RenderMode.WorldSpace)
                    continue;

                if (canvas.name is "HudCanvas" or "ControlsHint" or "CrosshairCanvas")
                    continue;
                if (canvas.name.StartsWith("Label_"))
                    continue;

                if (cam != null)
                    canvas.worldCamera = cam;

                if (canvas.GetComponent<GraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<GraphicRaycaster>();

                if (canvas.GetComponent<TrackedDeviceGraphicRaycaster>() == null)
                    canvas.gameObject.AddComponent<TrackedDeviceGraphicRaycaster>();
            }
        }
    }
}
