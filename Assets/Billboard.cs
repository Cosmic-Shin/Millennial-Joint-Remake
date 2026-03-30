using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Billboard : MonoBehaviour
{
    Transform cam;
    public float referenceDistance = 10f; // Distance at which the object appears at its original size
    public float scaleOffset;
    public bool DisableY;
    public bool DoScale;

    private void Start()
    {
        cam = Camera.main.transform;
    }

    void LateUpdate()
    {
        if (DoScale)
        {
            float distance = Vector3.Distance(cam.transform.position, transform.position);
            float scaleFactor = distance / referenceDistance;
            float finalScaleValue = scaleFactor * scaleOffset;
            transform.localScale = new Vector3(finalScaleValue, finalScaleValue, finalScaleValue);
        }

        transform.LookAt(transform.position + cam.forward);
        if (DisableY)
        {
            Vector3 euler = transform.rotation.eulerAngles;
            euler.x = 0;
            euler.z = 0;
            transform.rotation = Quaternion.Euler(euler);
        }
    }
}
