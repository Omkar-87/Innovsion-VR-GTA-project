using UnityEngine;
using UnityEngine.Events;
public class triggerZone : MonoBehaviour

{
    public string targetTag;
    public UnityEvent<GameObject> onenterEvent;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.tag == targetTag)
        {
            onenterEvent.Invoke(other.gameObject);
        }
    } 

    // Update is called once per frame
    void Update()
    {
        
    }
}
