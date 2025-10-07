using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class DisableGrabbingHandModel : MonoBehaviour
{
    public GameObject leftHandModel;
    public GameObject rightHandModel;
    void Start()
    {
        XRGrabInteractable xRGrabInteractable = GetComponent<XRGrabInteractable>();
        xRGrabInteractable.selectEntered.AddListener(HidegrabbingHand);
        xRGrabInteractable.selectExited.AddListener (ShowgrabbingHand);
    }
    public void HidegrabbingHand(SelectEnterEventArgs args)
    {
        if(args.interactableObject.transform.tag == "Left Hand")
        {
            leftHandModel.SetActive(false);
        }
        else if(args.interactableObject.transform.tag == "Right Hand")
        {
            rightHandModel.SetActive(false);
        }
    }
    public void ShowgrabbingHand(SelectExitEventArgs args)
    {
        if (args.interactableObject.transform.tag == "Left Hand")   
        {
            leftHandModel.SetActive(true);
        }
        else if (args.interactableObject.transform.tag == "Right Hand")
        {
            rightHandModel.SetActive(true);
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
