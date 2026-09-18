using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace VRTraining.VR
{
    /// <summary>
    /// Continuous slide locomotion for desktop / VR fallback.
    /// Look: hold RMB (or locked cursor). Move: WASD / arrows.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SlideLocomotion : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform headYawSource;
        [SerializeField] private float moveSpeed = 2.6f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private bool enableMouseLook = true;
        [SerializeField] private float lookSensitivity = 2.2f;

        private float _verticalVelocity;
        private float _pitch;

        private void Awake()
        {
            if (characterController == null)
                characterController = GetComponent<CharacterController>();
            if (headYawSource == null)
            {
                var cam = GetComponentInChildren<Camera>();
                if (cam != null)
                    headYawSource = cam.transform;
            }
        }

        private void Update()
        {
            HandleLook();
            HandleMove();
        }

        private void HandleLook()
        {
            if (!enableMouseLook || headYawSource == null)
                return;

            // Always look while cursor is locked; RMB also toggles lock. Escape unlocks.
            // Avoid requiring RMB to be held after lock — that felt like "ПКМ broken" mid-scenario.
            if (WasPressedThisFrame(KeyCode.Escape))
                Cursor.lockState = CursorLockMode.None;

            if (WasMouseButtonPressed(1))
            {
                Cursor.lockState = Cursor.lockState == CursorLockMode.Locked
                    ? CursorLockMode.None
                    : CursorLockMode.Locked;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            var delta = GetMouseDelta() * lookSensitivity;
            transform.Rotate(0f, delta.x, 0f);
            _pitch = Mathf.Clamp(_pitch - delta.y, -80f, 80f);
            headYawSource.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var input = GetMoveAxes();
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var yaw = headYawSource != null ? headYawSource.eulerAngles.y : transform.eulerAngles.y;
            var sprint = IsKeyHeld(sprintKey) ? sprintMultiplier : 1f;
            var move = Quaternion.Euler(0f, yaw, 0f) * input * (moveSpeed * sprint);

            if (characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -1f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            move.y = _verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }

        private static Vector3 GetMoveAxes()
        {
            float x = 0f, z = 0f;
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null)
            {
                if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) x -= 1f;
                if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) x += 1f;
                if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) z -= 1f;
                if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) z += 1f;
                return new Vector3(x, 0f, z);
            }
#endif
            return new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
        }

        private static Vector2 GetMouseDelta()
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
                return Mouse.current.delta.ReadValue() * 0.1f;
#endif
            return new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
        }

        private static bool IsMouseButtonHeld(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (button == 1) return Mouse.current.rightButton.isPressed;
                if (button == 0) return Mouse.current.leftButton.isPressed;
            }
#endif
            return Input.GetMouseButton(button);
        }

        private static bool WasMouseButtonPressed(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current != null)
            {
                if (button == 1) return Mouse.current.rightButton.wasPressedThisFrame;
                if (button == 0) return Mouse.current.leftButton.wasPressedThisFrame;
            }
#endif
            return Input.GetMouseButtonDown(button);
        }

        private static bool IsKeyHeld(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && key == KeyCode.LeftShift)
                return Keyboard.current.leftShiftKey.isPressed;
#endif
            return Input.GetKey(key);
        }

        private static bool WasPressedThisFrame(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current != null && key == KeyCode.Escape)
                return Keyboard.current.escapeKey.wasPressedThisFrame;
#endif
            return Input.GetKeyDown(key);
        }
    }
}
