using UnityEngine;

public class AlignCapsuleColliderWithCamera : MonoBehaviour
{
    public CapsuleCollider capsuleCollider;
    public Transform centerEyeAnchor;
    public Transform ovrCameraRig;
    public Vector3 cameraOffset = new Vector3(0, 1.7f, 0); // Adjust based on head height

    void Start()
    {
        if (capsuleCollider == null)
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
        }

        if (centerEyeAnchor != null)
        {
            centerEyeAnchor.localPosition = cameraOffset;
        }
    }

    void Update()
    {
        if (capsuleCollider != null && centerEyeAnchor != null && ovrCameraRig != null)
        {
            // Calculate the local position of the CenterEyeAnchor relative to the OVR Camera Rig
            Vector3 localEyePosition = ovrCameraRig.InverseTransformPoint(centerEyeAnchor.position);

            // Align the center of the CapsuleCollider with the local position of the CenterEyeAnchor's X and Z
            Vector3 newCenter = capsuleCollider.center;
            newCenter.x = localEyePosition.x;
            newCenter.z = localEyePosition.z;

            capsuleCollider.center = newCenter;
        }
    }
}