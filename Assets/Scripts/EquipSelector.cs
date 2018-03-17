using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EquipSelector : MonoBehaviour {
    public Scenario_level1 scenario_level1;
    public ViveIKDemo equipIK;
    public GameObject activeLight;
    public GameObject inActiveLight;

    void OnTriggerEnter(Collider other)
    {
        //scenario_level1.SelectSuit(this);
    }
}
