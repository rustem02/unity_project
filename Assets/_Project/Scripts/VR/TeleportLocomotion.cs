using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRTraining.VR
{
    /// <summary>
    /// Point-and-teleport: hold T, aim with look/mouse, release to blink.
    /// Works with CharacterController (temporarily disables it while moving the root).
    /// </summary>
    public class TeleportLocomotion : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float maxDistance = 14f;
        [SerializeField] private KeyCode teleportKey = KeyCode.T;
        [SerializeField] private bool useMouseAim = true;
        [SerializeField] private LineRenderer previewLine;
        [SerializeField] private Color validColor = new Color(0.2f, 0.9f, 0.5f, 0.8f);
        [SerializeField] private Color invalidColor = new Color(0.9f, 0.2f, 0.2f, 0.8f);

        private bool _hasTarget;
        private Vector3 _targetPoint;

        private void Awake()
        {
            if (playerRoot == null)
                playerRoot = transform;
            if (aimCamera == null)
                aimCamera = Camera.main;
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            EnsureLine();
        }

        private void EnsureLine()
        {
            if (previewLine != null)
                return;

            var go = new GameObject("TeleportPreview");
            go.transform.SetParent(transform, false);
            previewLine = go.AddComponent<LineRenderer>();
            previewLine.positionCount = 2;
            previewLine.startWidth = 0.03f;
            previewLine.endWidth = 0.03f;
            var shader = Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Sprites/Default")
                         ?? Shader.Find("Unlit/Color");
            previewLine.material = new Material(shader);
            previewLine.enabled = false;
        }

        private void Update()
        {
            if (aimCamera == null)
                aimCamera = Camera.main;
            if (aimCamera == null)
                return;

            var held = IsKeyHeld(teleportKey);
            var released = WasKeyReleased(teleportKey);

            if (held)
            {
                UpdateAim();
                previewLine.enabled = true;
                previewLine.startColor = previewLine.endColor = _hasTarget ? validColor : invalidColor;
                if (previewLine.material != null && previewLine.material.HasProperty("_BaseColor"))
                    previewLine.material.SetColor("_BaseColor", _hasTarget ? validColor : invalidColor);
                previewLine.SetPosition(0, aimCamera.transform.position);
                previewLine.SetPosition(1,
                    _hasTarget
                        ? _targetPoint
                        : aimCamera.transform.position + GetAimDirection() * maxDistance);
            }
            else if (released)
            {
                if (_hasTarget)
                    TeleportTo(_targetPoint);
                previewLine.enabled = false;
                _hasTarget = false;
            }
            else
            {
                previewLine.enabled = false;
            }
        }

        private void TeleportTo(Vector3 point)
        {
            var destination = point + Vector3.up * 0.08f;
            if (characterController != null)
            {
                characterController.enabled = false;
                playerRoot.position = destination;
                characterController.enabled = true;
            }
            else
            {
                playerRoot.position = destination;
            }
        }

        private void UpdateAim()
        {
            var ray = new Ray(aimCamera.transform.position, GetAimDirection());
            if (Physics.Raycast(ray, out var hit, maxDistance, groundMask, QueryTriggerInteraction.Ignore))
            {
                // Prefer roughly horizontal surfaces (floor / tables), reject steep walls.
                if (Vector3.Dot(hit.normal, Vector3.up) > 0.35f)
                {
                    _hasTarget = true;
                    _targetPoint = hit.point;
                    return;
                }
            }

            _hasTarget = false;
        }

        private Vector3 GetAimDirection()
        {
            if (useMouseAim)
            {
                var mouseRay = aimCamera.ScreenPointToRay(GetMousePosition());
                return mouseRay.direction;
            }

            return aimCamera.transform.forward;
        }

        private static Vector2 GetMousePosition()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.position.ReadValue();
#endif
            return Input.mousePosition;
        }

        private static bool IsKeyHeld(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && key == KeyCode.T)
                return Keyboard.current.tKey.isPressed;
#endif
            return Input.GetKey(key);
        }

        private static bool WasKeyReleased(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && key == KeyCode.T)
                return Keyboard.current.tKey.wasReleasedThisFrame;
#endif
            return Input.GetKeyUp(key);
        }
    }
}
