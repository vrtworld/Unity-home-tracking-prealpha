using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GoOutside : MonoBehaviour
{
    public Scenario_level1 level1;
    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    void OnTriggerEnter(Collider other)
    {
	//	yield return WaitForSecondsRealtime (5);
	//	level1.goOutside();
		StartCoroutine("WaitAndLoad");
    }

	IEnumerator WaitAndLoad(){
		yield return new WaitForSeconds (5);
		level1.goOutside();
	}
}
