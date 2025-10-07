using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class meteorPistol : MonoBehaviour
{
    public ParticleSystem particles;
    public LayerMask layermask;
    public Transform shootSource;
    public float Distance = 10f;
    private bool raycastActive = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        XRGrabInteractable xRGrabInteractable = GetComponent<XRGrabInteractable>();
        xRGrabInteractable.activated.AddListener(x => StartShoot());
        xRGrabInteractable.deactivated.AddListener(x => StopShoot());

    }
    public void StartShoot()
    {
        particles.Play();
        raycastActive = true;
    }
    public void StopShoot()
    {
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        raycastActive = false;
    }
    // Update is called once per frame
    void Update()
    {
        if (raycastActive)
        {
            RaycastCheck();
        }
    }
    void RaycastCheck()
    {
        RaycastHit hit;
        bool hasHit = Physics.Raycast(shootSource.position, shootSource.forward, out hit, Distance, layermask);
        if (hasHit)
        {
            hit.transform.gameObject.SendMessage("Break", SendMessageOptions.DontRequireReceiver);
        }
    }
}
