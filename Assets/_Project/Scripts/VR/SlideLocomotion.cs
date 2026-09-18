using UnityEngine;

namespace VRTraining.VR
{
    /// <summary>
    /// Continuous slide locomotion for desktop testing and as a VR fallback.
    /// XR continuous move can sit alongside this when headset is present.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class SlideLocomotion : MonoBehaviour
    {
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Transform headYawSource;
        [SerializeField] private float moveSpeed = 2.2f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [SerializeField] private float sprintMultiplier = 1.6f;
        [SerializeField] private bool enableMouseLook = true;
        [SerializeField] private float lookSensitivity = 2f;

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

            if (!Input.GetMouseButton(1) && !Cursor.lockState.Equals(CursorLockMode.Locked))
            {
                if (Input.GetKeyDown(KeyCode.Escape))
                    Cursor.lockState = CursorLockMode.None;
                return;
            }

            if (Input.GetMouseButtonDown(1))
                Cursor.lockState = CursorLockMode.Locked;

            var mx = Input.GetAxis("Mouse X") * lookSensitivity;
            var my = Input.GetAxis("Mouse Y") * lookSensitivity;
            transform.Rotate(0f, mx, 0f);
            _pitch = Mathf.Clamp(_pitch - my, -80f, 80f);
            headYawSource.localEulerAngles = new Vector3(_pitch, 0f, 0f);
        }

        private void HandleMove()
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 1f)
                input.Normalize();

            var yaw = headYawSource != null ? headYawSource.eulerAngles.y : transform.eulerAngles.y;
            var move =
                Quaternion.Euler(0f, yaw, 0f) * input *
                (moveSpeed * (Input.GetKey(sprintKey) ? sprintMultiplier : 1f));

            if (characterController.isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -1f;
            else
                _verticalVelocity += gravity * Time.deltaTime;

            move.y = _verticalVelocity;
            characterController.Move(move * Time.deltaTime);
        }
    }
}
