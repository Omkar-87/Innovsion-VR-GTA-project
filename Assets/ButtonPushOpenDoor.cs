using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class ButtonPushOpenDoor : MonoBehaviour
{
    public Animator animator;
    public string boolName = "open";
    private void Start()
    {
        GetComponent<XRSimpleInteractable>().selectEntered.AddListener(x=> toggleDoorOpen());

    }
    public void toggleDoorOpen()
    {
        bool isOpen = animator.GetBool(boolName);
        animator.SetBool(boolName, !isOpen);
    }
}
