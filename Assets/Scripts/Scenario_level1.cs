using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scenario_level1 : MonoBehaviour {
    public Transform hmd;

    public Transform bedroom;
    public Transform bridge;
    public Transform observatory;
    public Transform exit;
    public Transform outside;

    public AudioSource audioGuide;
    public AudioClip a1_welcome;
    public AudioClip a2_well_done;
    public AudioClip a3_go_to_the_bridge;
    public AudioClip a5_observatory;
    public AudioClip a6_space_exit;
    public AudioClip a7_space_out;

    public int currentStage = 0;

    //public EquipSelector[] characterSelectors;
    //public EquipSelector defaultSelector;
    public ViveIKDemo selectedSuit;

    public Valve.VR.InteractionSystem.Hand left;
    public Valve.VR.InteractionSystem.Hand right;

    public DG.Tweening.DOTweenAnimation blinkQuad;

    public Animator fly_station_animator;

    public bool allowTp = false;

    public GameObject firstScene;

    // Use this for initialization
    void Start () {
        //SelectSuit(defaultSelector);
    }

    // Update is called once per frame
    void Update() {
        int triggers = 0;

        if (left.controller == null || right.controller == null)
        {
            return;
        }

        if (left.controller.GetPress(SteamVR_Controller.ButtonMask.Trigger))
            triggers++;
        if (right.controller.GetPress(SteamVR_Controller.ButtonMask.Trigger))
            triggers++;
            
        if (currentStage == 1) // equipSuite
        {
            if (triggers > 0 || Input.GetKeyUp(KeyCode.Alpha1))
            {
                EquipSuit();
            }
        }
        if (!allowTp)
            return;

        if (currentStage == 3) // teleport to the bridge
        {
            if (triggers == 2 || Input.GetKeyUp(KeyCode.Alpha1))
            {
                allowTp = false;
                StartCoroutine(teleport(bridge, null));
            }
        }
        else if (currentStage == 4) // teleport to the observatory
        {
            if (triggers == 2 || Input.GetKeyUp(KeyCode.Alpha1))
            {
                allowTp = false;
                StartCoroutine(teleport(observatory, a5_observatory));
                //fly_station_animator.StopPlayback();
                //fly_station_animator.StartPlayback();
                fly_station_animator.Play("station_orbit_02", -1, 0);
            }
        }
        else if (currentStage == 5) // teleport to exit
        {
            if (triggers == 2 || Input.GetKeyUp(KeyCode.Alpha1))
            {
                allowTp = false;
                StartCoroutine(teleport(exit, a6_space_exit));
            }
        }
        else if (currentStage == 6)
        {
            if (triggers == 2 || Input.GetKeyUp(KeyCode.Alpha1))
            {
                goOutside();
            }
        }
    }

    public void goOutside()
    {
        currentStage++;
        allowTp = false;
        Application.LoadLevelAdditive(1);
        transform.position = outside.position;
        transform.rotation = outside.rotation;
        firstScene.SetActive(false);

        audioGuide.clip = a7_space_out;
        audioGuide.Play();
    }

    IEnumerator teleport(Transform destination, AudioClip sound)
    {
        currentStage++;
        blinkQuad.DOPlayById("blink");
        yield return new WaitForSeconds(0.2f);
        transform.position = destination.position;
        transform.rotation = destination.rotation;

        audioGuide.clip = sound;
        audioGuide.Play();
        yield return new WaitForSeconds(2.0f);
        allowTp = true;
    }

        IEnumerator lookAround()
    {
        yield return new WaitForSeconds(10);
        audioGuide.clip = a3_go_to_the_bridge;
        audioGuide.Play();

        currentStage = 3;
        allowTp = true;
    }

    public void LogoClosed()
    {
        if (currentStage != 0)
            return;

        currentStage = 1;
        audioGuide.clip = a1_welcome;
        audioGuide.Play();
    }

    /*
    public void SelectSuit(EquipSelector selector)
    {
        for (int i=0; i<characterSelectors.Length; i++)
        {
            characterSelectors[i].activeLight.SetActive(false);
            characterSelectors[i].inActiveLight.SetActive(true);
        }

        selectedSuit = selector.equipIK;

        selector.inActiveLight.SetActive(false);
        selector.activeLight.SetActive(true);
    }
    */
    public void EquipSuit()
    {
        if (currentStage != 1)
            return;

        //left.transform.GetChild(0).gameObject.SetActive(false);
        //right.transform.GetChild(0).gameObject.SetActive(false);
        left.GetComponentInChildren<Valve.VR.InteractionSystem.SpawnRenderModel>().enabled = false;
        right.GetComponentInChildren<Valve.VR.InteractionSystem.SpawnRenderModel>().enabled = false;

        if (!selectedSuit.setupAll())
            return;

        audioGuide.clip = a2_well_done;
        audioGuide.Play();

        currentStage = 2;
        StartCoroutine(lookAround());
    }
}
