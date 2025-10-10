using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class playsteps : MonoBehaviour
{   PlayableDirector director;
    public List<Step> steps;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        director = GetComponent<PlayableDirector>();
    }
    [System.Serializable]
    public class Step
    {
        public string name;
        public float time;
        public bool hasplayed = false;
    }
    public void PlaystepIndex(int index)
    {
        Step step = steps[index];
        if (!step.hasplayed) {
        step.hasplayed = true;
            director.Stop();
            director.time = step.time;
            director.Play();
        }
    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
