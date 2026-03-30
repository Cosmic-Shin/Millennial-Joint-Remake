using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform Cam;
    public float LerpTime;
    public Transform[] FollowGroup;
    public float DistanceOffset;

    private void Update()
    {
        Vector3 pos = Vector3.zero;
        float dis = 0;

        foreach (Transform t in FollowGroup)
        {
            pos += t.position;
            float d = Vector3.Distance(transform.position, t.position);
            if (d > dis) dis = d;
        }

        pos /= FollowGroup.Length;

        float lerpTime = LerpTime * Time.deltaTime;
        transform.position = Vector3.Lerp(transform.position, pos, lerpTime);
        Cam.localPosition = Vector3.Lerp(Cam.localPosition, new(0, 0, -dis - DistanceOffset), lerpTime / 2);
    }
}
