using UnityEngine;
using VRTraining.Core.Interfaces;

namespace VRTraining.Highlight
{
    /// <summary>
    /// Drop-in adapter for Highlight Plus / Outline assets.
    /// Assign the target renderer and wire Highlight Plus Enable/Disable from SetHighlighted,
    /// or replace the body with HighlightEffect API calls after importing the asset.
    /// </summary>
    public class HighlightPlusAdapter : MonoBehaviour, IHighlightable
    {
        [SerializeField] private string targetId;
        [SerializeField] private OutlineHighlighter fallbackOutline;

        public string TargetId => targetId;

        private void Awake()
        {
            if (fallbackOutline == null)
                fallbackOutline = GetComponent<OutlineHighlighter>();
            if (string.IsNullOrWhiteSpace(targetId) && fallbackOutline != null)
                targetId = fallbackOutline.TargetId;
        }

        public void SetHighlighted(bool highlighted)
        {
            // Prefer Highlight Plus when present on this object (via SendMessage to avoid hard dependency).
            if (SendMessageOptionsSafe("SetHighlighted", highlighted))
                return;

            fallbackOutline?.SetHighlighted(highlighted);
        }

        private bool SendMessageOptionsSafe(string method, bool value)
        {
            var behaviours = GetComponents<MonoBehaviour>();
            for (var i = 0; i < behaviours.Length; i++)
            {
                var b = behaviours[i];
                if (b == null || b == this || b is OutlineHighlighter)
                    continue;

                var type = b.GetType();
                if (type.Name.IndexOf("Highlight", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                var methodInfo = type.GetMethod("SetHighlighted")
                                 ?? type.GetMethod("SetGlow")
                                 ?? type.GetMethod("SetOutline");
                if (methodInfo == null)
                    continue;

                methodInfo.Invoke(b, new object[] { value });
                return true;
            }

            return false;
        }
    }
}
