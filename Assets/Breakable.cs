using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

public class Breakable : MonoBehaviour
{
    public List<GameObject> breakabelPices;
    public float timedtobreak = 2;
    private float timer = 0;
    public UnityEvent Onbreak;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        foreach (var item in breakabelPices)
        {
            item.SetActive(false);
        }
    }
    public void Break()
    {
        timer += Time.deltaTime;
        if (timer > timedtobreak)
        {
            foreach (var item in breakabelPices)
            {
                item.SetActive(true);
                item.transform.parent = null;
            }
            Onbreak.Invoke();
            gameObject.SetActive(false);

        }
    }
}
