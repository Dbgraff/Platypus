using UnityEngine;

public class FixedRobotCamera : MonoBehaviour
{
    [SerializeField] private Transform robotBody;
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 0f, 0.88f);

    private void Start()
    {
        if (robotBody == null)
            robotBody = transform.parent;

        transform.localPosition = localOffset;
        transform.localRotation = Quaternion.identity;
    }

    private void LateUpdate()
    {
        if (robotBody == null) return;

        transform.position = robotBody.position + robotBody.TransformDirection(localOffset);
        transform.rotation = robotBody.rotation;
    }
}