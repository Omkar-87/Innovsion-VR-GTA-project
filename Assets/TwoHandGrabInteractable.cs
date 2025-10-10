// TwoHandGrabInteractable.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

[AddComponentMenu("XR/Two Hand Grab Interactable")]
public class TwoHandGrabInteractable : XRGrabInteractable
{
    XRBaseInteractor primaryInteractor;
    XRBaseInteractor secondaryInteractor;

    [Tooltip("How fast the rifle rotates to align between the two hands")]
    public float rotationLerpSpeed = 25f;

    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);

        var interactor = args.interactorObject as XRBaseInteractor;
        if (primaryInteractor == null)
            primaryInteractor = interactor;
        else if (secondaryInteractor == null && interactor != primaryInteractor)
            secondaryInteractor = interactor;
    }

    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);

        var interactor = args.interactorObject as XRBaseInteractor;
        if (interactor == secondaryInteractor)
        {
            secondaryInteractor = null;
        }
        else if (interactor == primaryInteractor)
        {
            // If primary releases but a secondary is still there, promote it to primary
            primaryInteractor = secondaryInteractor;
            secondaryInteractor = null;
        }
    }

    public override void ProcessInteractable(XRInteractionUpdateOrder.UpdatePhase updatePhase)
    {
        base.ProcessInteractable(updatePhase);

        // Only modify rotation while both hands are attached, and during dynamic updates
        if (primaryInteractor != null && secondaryInteractor != null &&
            updatePhase == XRInteractionUpdateOrder.UpdatePhase.Dynamic)
        {
            Vector3 primaryPos = primaryInteractor.transform.position;
            Vector3 secondaryPos = secondaryInteractor.transform.position;
            Vector3 dir = secondaryPos - primaryPos;

            if (dir.sqrMagnitude > 0.0001f)
            {
                // compute a target rotation that points the rifle from primary -> secondary
                Quaternion targetRot = Quaternion.LookRotation(dir, primaryInteractor.transform.up);

                // Smoothly blend current rotation to target rotation for nicer feel
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationLerpSpeed * Time.deltaTime);
            }
        }
    }
}
