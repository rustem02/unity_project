using UnityEngine;

namespace VRTraining.Core
{
    /// <summary>
    /// Places a world-space canvas in front of the camera for readable VR HUD text.
    /// </summary>
    public class FollowCameraHud : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.12f, 0.75f);

        private void LateUpdate()
        {
            if (cameraTransform == null)
            {
                if (Camera.main != null)
                    cameraTransform = Camera.main.transform;
                else
                    return;
            }

            transform.position = cameraTransform.TransformPoint(localOffset);
            transform.rotation = Quaternion.LookRotation(transform.position - cameraTransform.position, Vector3.up);
        }
    }
}
