using UnityEngine;

public class trashZon : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Start()
    {
        GetComponent<triggerZone>().onenterEvent.AddListener(InsideTrash);
    }

    // Update is called once per frame
   public void InsideTrash(GameObject go)
    {
        go.SetActive(false); 
    }
}
