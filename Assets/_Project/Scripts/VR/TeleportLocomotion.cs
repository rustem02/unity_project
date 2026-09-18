using UnityEngine;

namespace VRTraining.VR
{
    /// <summary>
    /// Simple point-and-teleport for VR/desktop: hold T and release to blink to aim point.
    /// </summary>
    public class TeleportLocomotion : MonoBehaviour
    {
        [SerializeField] private Transform playerRoot;
        [SerializeField] private Camera aimCamera;
        [SerializeField] private LayerMask groundMask = ~0;
        [SerializeField] private float maxDistance = 12f;
        [SerializeField] private KeyCode teleportKey = KeyCode.T;
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
            previewLine.startWidth = 0.02f;
            previewLine.endWidth = 0.02f;
            previewLine.material = new Material(Shader.Find("Sprites/Default") ?? Shader.Find("Unlit/Color"));
            previewLine.enabled = false;
        }

        private void Update()
        {
            if (aimCamera == null)
                return;

            if (Input.GetKey(teleportKey))
            {
                UpdateAim();
                previewLine.enabled = true;
                previewLine.startColor = previewLine.endColor = _hasTarget ? validColor : invalidColor;
                previewLine.SetPosition(0, aimCamera.transform.position);
                previewLine.SetPosition(1, _hasTarget ? _targetPoint : aimCamera.transform.position + aimCamera.transform.forward * maxDistance);
            }
            else if (Input.GetKeyUp(teleportKey))
            {
                if (_hasTarget)
                    playerRoot.position = _targetPoint + Vector3.up * 0.05f;
                previewLine.enabled = false;
                _hasTarget = false;
            }
            else
            {
                previewLine.enabled = false;
            }
        }

        private void UpdateAim()
        {
            var ray = new Ray(aimCamera.transform.position, aimCamera.transform.forward);
            if (Physics.Raycast(ray, out var hit, maxDistance, groundMask))
            {
                _hasTarget = true;
                _targetPoint = hit.point;
            }
            else
            {
                _hasTarget = false;
            }
        }
    }
}
