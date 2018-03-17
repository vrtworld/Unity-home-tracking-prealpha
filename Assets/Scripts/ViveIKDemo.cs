using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// A tracker is 'bound' to one foot, then its pose(position and rotation) can be 
/// used to calculate the ankle position used in IK.
/// Similarly, the torso pose is calculated by a bound tracker or controller, then 
/// the derived arm positions and thigh positions are used in IK.
/// 
/// </summary>
public class ViveIKDemo : MonoBehaviour {
    public Text logText;
    public GameObject ankleMarkerLeft;
    public GameObject ankleMarkerRight;
    public List<GameObject> deviceMarkers;
    public GameObject leftHandOffsetObject;
    public GameObject rightHandOffsetObject;
    public GameObject markerHead;
    public Transform eyeTransform;

    Dictionary<int, Transform> deviceMarkerDict = new Dictionary<int, Transform>();
    public bool kinematicReady = false;
    Queue<object> logQueue = new Queue<object>(); // logs in logQueue can be seen in VR
    int maxLogCount = 8;
    public float initHeight = 1.75f;
    IKDemoModelState initModelState = null;

    Dictionary<TrackerRole, int> trackers = new Dictionary<TrackerRole, int>();
    List<OffsetTracking> offsetTrackedList = new List<OffsetTracking>();
	public Transform leftHandTarget = null;
	public Transform rightHandTarget = null;
    AnimationBlendTest animationBlender;
    List<ThreePointIK> ikList = new List<ThreePointIK>();// to fully control the execution order of the IK solvers

    public GameObject helperTorso;
    public GameObject helperLeftHand;
    public GameObject helperRightHand;
    public GameObject helperLeftFoot;
    public GameObject helperRightFoot;

    public bool enablePoseValidator = true;

    public int stage = 0;
    // Use this for initialization
    void Start () {
        transform.parent = Camera.main.transform.parent;
        transform.localPosition = Vector3.zero;

        foreach (var item in deviceMarkers)
        {
            deviceMarkerDict[(int)item.GetComponent<SteamVR_TrackedObject>().index] = item.transform;
        }
        //InitWithCustomHeight();

        animationBlender = GetComponent<AnimationBlendTest>();
        RecordInitModelState();
    }
	
    void RecordInitModelState()
    {
        initModelState = new IKDemoModelState();
        initModelState.eyePos = eyeTransform.position - transform.position;
        initModelState.ankleMarkerLeftPos = ankleMarkerLeft.transform.position - transform.position;
        initModelState.ankleMarkerRightPos = ankleMarkerRight.transform.position - transform.position;
        initModelState.markerHeadPos = markerHead.transform.position - transform.position;
        initModelState.modelScale = transform.localScale;
    }

    public bool setupAll()
    {
        // stage 0
        AutoAdjustHeight();

        // stage 1
        if (AssignTrackers())
        {
            MyLog("Entering stage2");
        }
        else
        {
            MyLogError("Not enough tracked devices found");
            return false;
        }

        if (!checkValidPosition())
            return false;

        // stage 2
        StartOffsetTracking();
        StartIK();
        MyLog("Entering stage3");

        kinematicReady = true;
        return true;
    }

    public bool checkValidPosition()
    {
        Pose leftHandPose = new Pose();
        Pose rightHandPose = new Pose();
        Pose leftFootPose = new Pose();
        Pose rightFootPose = new Pose();
        Pose torsoPose = new Pose();

        if (trackers.Count == 5)
        {
            leftHandPose = GetPose(trackers[TrackerRole.HandLeft]);
            rightHandPose = GetPose(trackers[TrackerRole.HandRight]);
            leftFootPose = GetPose(trackers[TrackerRole.FootLeft]);
            rightFootPose = GetPose(trackers[TrackerRole.FootRight]);
            torsoPose = GetPose(trackers[TrackerRole.Torso]);
        } else
        {
            rightHandPose = GetPose(trackers[TrackerRole.HandRight]);
            torsoPose = GetPose(trackers[TrackerRole.Torso]);
        }

        {
            helperTorso.SetActive(true);
            helperLeftHand.SetActive(true);
            helperRightHand.SetActive(true);
            helperLeftFoot.SetActive(true);
            helperRightFoot.SetActive(true);

            helperTorso.transform.localPosition = torsoPose.pos;
            helperLeftHand.transform.localPosition = leftHandPose.pos;
            helperRightHand.transform.localPosition = rightHandPose.pos;
            helperLeftFoot.transform.localPosition = leftFootPose.pos;
            helperRightFoot.transform.localPosition = rightFootPose.pos;

            helperTorso.GetComponent<MeshRenderer>().material.color = Color.green;
            helperLeftHand.GetComponent<MeshRenderer>().material.color = Color.green;
            helperRightHand.GetComponent<MeshRenderer>().material.color = Color.green;
            helperLeftFoot.GetComponent<MeshRenderer>().material.color = Color.green;
            helperRightFoot.GetComponent<MeshRenderer>().material.color = Color.green;
        }
        

        Debug.Log("check valid pose :: ");
        Debug.Log(leftHandPose.pos + " <- " + torsoPose.pos + " -> " + rightHandPose.pos);
        Debug.Log(leftFootPose.pos + " <- " + torsoPose.pos + " -> " + rightFootPose.pos);

        float dRightHand = Mathf.Abs(rightHandPose.pos.x - torsoPose.pos.x);
        float dLeftHand = Mathf.Abs(leftHandPose.pos.x - torsoPose.pos.x);
        float dRightFoot = Mathf.Abs(rightFootPose.pos.x - torsoPose.pos.x);
        float dLeftFoot = Mathf.Abs(leftFootPose.pos.x - torsoPose.pos.x);

        bool result = true;

        if (Mathf.Abs(torsoPose.pos.x) > 0.15f || Mathf.Abs(torsoPose.pos.z) > 0.35f)
        {
            Debug.Log("Invalid Torso pose");
            result &= false;
            helperTorso.GetComponent<MeshRenderer>().material.color = Color.red;
        }

        if (dRightHand < 0.4f)
        {
            Debug.Log("ivalid right hand pose");
            result &= false;
            helperRightHand.GetComponent<MeshRenderer>().material.color = Color.red;
        }

        if (dLeftHand < 0.4f)
        {
            Debug.Log("ivalid left hand pose");
            result &= false;
            helperLeftHand.GetComponent<MeshRenderer>().material.color = Color.red;
        }

        if (dRightFoot < 0.1f)
        {
            Debug.Log("ivalid right foot pose");
            result &= false;
            helperRightFoot.GetComponent<MeshRenderer>().material.color = Color.red;
        }

        if (dLeftFoot < 0.1f)
        {
            Debug.Log("ivalid left foot pose");
            result &= false;
            helperLeftFoot.GetComponent<MeshRenderer>().material.color = Color.red;
        }

        if (result)
        {
            helperTorso.SetActive(false);
            helperLeftHand.SetActive(false);
            helperRightHand.SetActive(false);
            helperLeftFoot.SetActive(false);
            helperRightFoot.SetActive(false);
        }

        if (enablePoseValidator)
            return result;
        else
            return false;
    }

	// Update is called once per frame
	void Update () {
        if (kinematicReady)
        {
            UpdateIK();
            UpdateOffsetTracking();
        } else
        {
            AutoAdjustHeight();
        }
    }

    void AutoAdjustHeight()
    {

        float actualEyeHeight = (Camera.main.transform.position - transform.position).y;

        actualEyeHeight = Mathf.Clamp(actualEyeHeight, 0.7f, 2.5f);

        float eyeHeightToBodyHeadRatio = initModelState.eyePos.y / initHeight;
        float estimatedHeight = actualEyeHeight / eyeHeightToBodyHeadRatio;

        AdjustToHeight(estimatedHeight);
    }

    void InitWithCustomHeight()
    {
        float customHeight;
        if (DumbConfigFile.ReadFloat(out customHeight))
        {
            MyLog("Adjusting to height = " + customHeight);
            AdjustToHeight(customHeight);
        }
        else
        {
            Debug.Log("no height config file found, using default");
        }
    }

    void AdjustToHeight(float customHeight)
    {
        float ratio = customHeight / initHeight;
        ankleMarkerLeft.transform.localPosition = initModelState.ankleMarkerLeftPos*ratio;
        ankleMarkerRight.transform.localPosition = initModelState.ankleMarkerRightPos*ratio;
        markerHead.transform.localPosition = initModelState.markerHeadPos*ratio;
        transform.localScale = initModelState.modelScale*ratio;
    }


    void StartIK()
    {

        int headIndex = -1;
        

        var tpIkComps = GetComponents<ThreePointIK>();

        foreach (var item in tpIkComps)
        {
            item.manualUpdateIK = true;
            item.enabled = true;

            ikList.Add(item);
        }


        headIndex = ikList.FindIndex(item => item.bendNormalStrategy == ThreePointIK.BendNormalStrategy.head);
        if (headIndex >= 0)
            Swap(ikList, 0, headIndex);
    }


    void UpdateIK()
	{

        foreach (var item in ikList)
        {
            item.UpdateIK();
        }
	}

    Pose GetPose(int index)
    {
        var trans = deviceMarkerDict[index];
        Pose pose = new Pose { pos = (trans.position - transform.position), rot = trans.rotation };
        return pose;
    }

    void StartOffsetTracking()
    {
        List<TrackerRole> keys = new List<TrackerRole> { TrackerRole.Torso, TrackerRole.FootLeft, TrackerRole.FootRight };
        List<Transform> values = new List<Transform> { transform, ankleMarkerLeft.transform, ankleMarkerRight.transform };
        
        foreach (var item in trackers)
        {
            int index = keys.IndexOf(item.Key);
            if (index >= 0)
            {
                var trackedInfo = new OffsetTracking();
                trackedInfo.deviceIndex = item.Value;
                trackedInfo.trackerRole = keys[index];
                trackedInfo.targetTrans = values[index];
                trackedInfo.deviceMarkerDict = deviceMarkerDict;
                trackedInfo.StartTracking();
                offsetTrackedList.Add(trackedInfo);
            }
        }

        markerHead.transform.parent = Camera.main.transform;
        
    }

    void UpdateOffsetTracking()
    {
        foreach (var item in offsetTrackedList)
            item.UpdateOffsetTracking();
    
        if (animationBlender != null && trackers.ContainsKey(TrackerRole.HandLeft))
        {
            int leftHandIndex = trackers[TrackerRole.HandLeft];
            float triggerValue = SteamVR_Controller.Input(leftHandIndex).GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
            animationBlender.lerpValue = triggerValue;
        }

        if (trackers.ContainsKey(TrackerRole.HandRight))
        {
            int leftHandIndex = trackers[TrackerRole.HandRight];
            float triggerValue = SteamVR_Controller.Input(leftHandIndex).GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger).x;
            UpdatePistolEffect(triggerValue);
        }
    }

    void UpdatePistolEffect(float triggerValue)
    {

    }

    void Swap<T>(List<T> list, int indexA, int indexB)
    {
        T temp = list[indexA];
        list[indexA] = list[indexB];
        list[indexB] = temp;
    }

    public bool AssignTrackers()
    {
        trackers.Clear();

        List<KeyValuePair<int, Vector3>> devices = new List<KeyValuePair<int, Vector3>>();
        for (int i = (int)SteamVR_TrackedObject.EIndex.Device1; i < (int)SteamVR_TrackedObject.EIndex.Device15; i++)
        {
            var device = SteamVR_Controller.Input(i);
            if (device.hasTracking && device.connected && device.valid)
            {
                var deviceClass = Valve.VR.OpenVR.System.GetTrackedDeviceClass((uint)i);
                if (deviceClass == Valve.VR.ETrackedDeviceClass.Controller || deviceClass == Valve.VR.ETrackedDeviceClass.GenericTracker)
                {
                    devices.Add(new KeyValuePair<int, Vector3>(i, GetPose(i).pos));
                    Debug.Log((SteamVR_TrackedObject.EIndex)i + ", type = " + deviceClass);
                }
                else
                {
                    MyLog("Device"+i+" is a basestation, type = " + deviceClass);
                }
            }
        }

        MyLog("device count = " + devices.Count);

        devices.Sort((a, b) => a.Value.y.CompareTo(b.Value.y));
               
        if (devices.Count == 5)
        {
            if (devices[0].Value.x < 0f)
                Swap(devices, 0, 1);
            if (devices[3].Value.x < 0f)
                Swap(devices, 3, 4);

            trackers[TrackerRole.FootRight] = devices[0].Key;
            trackers[TrackerRole.FootLeft] = devices[1].Key;
            trackers[TrackerRole.Torso] = devices[2].Key;
            trackers[TrackerRole.HandRight] = devices[3].Key;
            trackers[TrackerRole.HandLeft] = devices[4].Key;

			rightHandTarget = deviceMarkerDict[devices[3].Key];
			leftHandTarget = deviceMarkerDict [devices [4].Key];
        }
        else if (devices.Count == 4)
        {
            if (devices[0].Value.x < 0f)
                Swap(devices, 0, 1);

            trackers[TrackerRole.FootRight] = devices[0].Key;
            trackers[TrackerRole.FootLeft] = devices[1].Key;
            trackers[TrackerRole.Torso] = devices[2].Key;

			if (devices [3].Value.x < 0f) {
				trackers [TrackerRole.HandLeft] = devices [3].Key;
				leftHandTarget = deviceMarkerDict[devices[3].Key];
			} else {
				trackers [TrackerRole.HandRight] = devices [3].Key;
				rightHandTarget = deviceMarkerDict[devices[3].Key];
			}
        }
        else if (devices.Count == 3)
        {
            trackers[devices[0].Value.x < 0f? TrackerRole.FootLeft : TrackerRole.FootRight] = devices[0].Key;
			trackers[TrackerRole.Torso] = devices[1].Key;
			if (devices [2].Value.x < 0f) {
				trackers [TrackerRole.HandLeft] = devices [2].Key;
				leftHandTarget = deviceMarkerDict[devices[2].Key];
			} else {
				trackers [TrackerRole.HandRight] = devices [2].Key;
				rightHandTarget = deviceMarkerDict[devices[2].Key];
			}
        }
        else if (devices.Count == 2)
        {
			trackers[TrackerRole.Torso] = devices[0].Key;
			if (devices [1].Value.x < 0f) {
				trackers [TrackerRole.HandLeft] = devices [1].Key;
				leftHandTarget = deviceMarkerDict[devices[1].Key];
			} else {
				trackers [TrackerRole.HandRight] = devices [1].Key;
				rightHandTarget = deviceMarkerDict[devices[1].Key];
			}
        }
        else
        {
            return false;
        }

        if (leftHandTarget != null && leftHandOffsetObject != null)
        {
            AssignChildAndKeepLocalTrans(ref leftHandTarget, leftHandOffsetObject.transform);
        }
        if (rightHandTarget != null && rightHandOffsetObject != null)
        {
            AssignChildAndKeepLocalTrans(ref rightHandTarget, rightHandOffsetObject.transform);
        }

        string strTrackers = "";
        foreach (var item in trackers.Keys)
            strTrackers += item + ",";
        MyLog("bound body parts: " + strTrackers);

        return true;
    }

    void AssignChildAndKeepLocalTrans(ref Transform parent, Transform child)
    {
        Vector3 localPos = child.localPosition;
        Vector3 localScale = child.localScale;
        Quaternion localRot = child.localRotation;
        child.parent = parent;
        child.localPosition = localPos;
        child.localScale = localScale;
        child.localRotation = localRot;
        parent = child;
    }

    void MyLog(object msg)
    {
        Debug.Log(msg);

        if (logText != null)
        {
            logQueue.Enqueue(msg);
            if (logQueue.Count > maxLogCount)
                logQueue.Dequeue();
            DisplayLogQueue();
        }
    }

    void MyLogError(object msg)
    {
        Debug.LogError(msg);

        if (logText != null)
        {
            logQueue.Enqueue("ERROR: " + msg);
            if (logQueue.Count > maxLogCount)
                logQueue.Dequeue();
            DisplayLogQueue();
        }
    }

    void DisplayLogQueue()
    {
        string str = "";
        foreach (var item in logQueue)
        {
            str += item + "\n";
        }
        logText.text = str;
    }
}
