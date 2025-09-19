using UnityEngine;

public class FollowUs : MonoBehaviour
{
    [SerializeField] private Transform centerEyeAnchor;
    [SerializeField] private Transform target;

    public float distanza = 1.5f;
    public float followSpeed = 5f;
    public float rotationSpeed = 5f;

    void Update()
    {
        if (centerEyeAnchor == null) return;

        // Target position in front of the user's gaze
        Vector3 targetPos = centerEyeAnchor.position + centerEyeAnchor.forward * distanza;

        // Interpolazione fluida della posizione
        target.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // Interpolazione fluida della rotazione verso lo sguardo
        Quaternion targetRot = Quaternion.LookRotation(centerEyeAnchor.forward);
        target.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
    }
}