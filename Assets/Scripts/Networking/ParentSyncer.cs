using UnityEngine;

public class ParentSyncer : MonoBehaviour
{
    private Transform parentTransform;

    void Start()
    {
        parentTransform = transform.parent;
    }

    void LateUpdate()
    {
        if (parentTransform != null)
        {
            parentTransform.position = transform.position;
            parentTransform.rotation = transform.rotation;

            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
    }
}
