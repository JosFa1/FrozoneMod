using BepInEx;
using System;
using UnityEngine;
using UnityEngine.XR;
using Utilla;
using GorillaLocomotion;
using GorillaTag;
using GorillaExtensions;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using Oculus.Platform.Models;
using Unity.Mathematics;
using System.Diagnostics.Contracts;
using HarmonyLib;
using GorillaLocomotion.Swimming;
using Photon.Pun;

namespace Frozone
{
    public class IceObjectPool
    {
        private GameObject icePrefab;
        private List<GameObject> iceInstances = new List<GameObject>();

        public IceObjectPool(GameObject prefab)
        {
            icePrefab = prefab;
        }

        public GameObject GetIceInstance(Vector3 position, Quaternion rotation)
        {
            GameObject iceInstance = null;

            // Check if there is an inactive ice instance in the pool
            for (int i = 0; i < iceInstances.Count; i++)
            {
                if (!iceInstances[i].activeSelf)
                {
                    iceInstance = iceInstances[i];
                    iceInstance.transform.position = position;
                    iceInstance.transform.rotation = rotation;
                    iceInstance.SetActive(true);
                    break;
                }
            }

            // If there are no inactive instances, create a new one
            if (iceInstance == null)
            {
                iceInstance = UnityEngine.Object.Instantiate(icePrefab, position, rotation);
                iceInstances.Add(iceInstance);
            }

            return iceInstance;
        }

        public void ReturnIceInstance(GameObject iceInstance)
        {
            iceInstance.SetActive(false);
        }

        public void Clear()
        {
            foreach (GameObject iceInstance in iceInstances)
            {
                UnityEngine.Object.Destroy(iceInstance);
            }
            iceInstances.Clear();
        }
    }

    [Description("HauntedModMenu")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private bool inRoom;
        private bool enabled;
        private IceObjectPool iceObjectPool;
        private List<GameObject> iceInstances = new List<GameObject>();
        private Vector3 leftHandP;
        private Quaternion leftHandR;
        private Vector3 rightHandP;
        private Quaternion rightHandR;
        private double leftTimer;
        private double rightTimer;
        private double coolDown = 0.1;
        private double playerSpeedAverage;
        private double playerSpeedX;
        private double playerSpeedY;
        private double playerSpeedZ;
        private Vector3 playerSpeedV3;
        private GameObject Gorilla;
        private float speedCoolDown = 1;
        private XRNode leftHandNode = XRNode.LeftHand;
        private XRNode rightHandNode = XRNode.RightHand;
        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        public bool primaryR = false;
        public bool primaryL = false;
        public bool secondaryR = false;
        public bool admin = false;

        void Start()
        {
            GorillaTagger.OnPlayerSpawned(OnGameInitialized);
        }

        public AssetBundle LoadAssetBundle(string path)
        {
            Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(path);
            AssetBundle bundle = AssetBundle.LoadFromStream(stream);
            stream.Close();
            return bundle;
        }

        void OnEnable()
        {
            HarmonyPatches.ApplyHarmonyPatches();
            enabled = true;
        }

        void OnDisable()
        {
            enabled = false;
            DeleteAllIce();
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized()
        {
            NetworkSystem.Instance.OnReturnedToSinglePlayer += () => DeleteAllIce();
            try
            {
                Console.WriteLine("======================================================");
                var bundle = LoadAssetBundle("Frozone.Resources.frozonebundle");
                foreach (var name in bundle.GetAllAssetNames())
                {
                    Console.WriteLine(name);
                }
                GameObject icePrefab = bundle.LoadAsset<GameObject>("ice");
                icePrefab.SetActive(false);
                icePrefab.AddComponent<GorillaSurfaceOverride>();
                GorillaSurfaceOverride surfaceOverride = icePrefab.GetComponent<GorillaSurfaceOverride>();
                surfaceOverride.overrideIndex = 59;

                // Create the ice object pool
                iceObjectPool = new IceObjectPool(icePrefab);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            if (PhotonNetwork.LocalPlayer.NickName.ToUpper() == "JOSFA")
            {
                admin = true;
            }
        }

        void Update()
        {
            if (NetworkSystem.Instance.InRoom && NetworkSystem.Instance.GameModeString.Contains("MODDED_") && enabled)
            {
                //InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryR);
                if (admin == true)
                    primaryL = ControllerInputPoller.instance.leftControllerPrimaryButton;
                primaryR = ControllerInputPoller.instance.rightControllerPrimaryButton;
                primaryL = ControllerInputPoller.instance.leftControllerPrimaryButton;
                isLeftPressed = ControllerInputPoller.instance.leftGrab;
                isRightPressed = ControllerInputPoller.instance.rightGrab;
                if (leftTimer <= 0)
                {
                    if (isLeftPressed)
                    {
                        leftTimer = coolDown;
                        Debug.Log("Left Pressed, Attempted spawn");

                        // Use the object pool to get an ice instance
                        GameObject newIce = iceObjectPool.GetIceInstance(leftHandP, leftHandR);
                        iceInstances.Add(newIce);
                    }
                }
                else
                {
                    if (admin)
                    {
                        Debug.Log(leftTimer);
                    }
                    leftTimer -= (Time.deltaTime * playerSpeedAverage);
                }

                if (rightTimer <= 0)
                {
                    if (isRightPressed)
                    {
                        rightTimer = coolDown;
                        Debug.Log("Right Pressed, Attempted spawn");

                        // Use the object pool to get an ice instance
                        GameObject newIce = iceObjectPool.GetIceInstance(rightHandP, rightHandR);
                        iceInstances.Add(newIce);
                    }
                }
                else
                {
                    if (admin)
                    {
                        Debug.Log(rightTimer);
                    }
                    rightTimer -= (Time.deltaTime * playerSpeedAverage);
                }
                if (primaryR)
                {
                    DeleteAllIce();
                }
                /*if (secondaryR)
                {
                    Rigidbody targetRigidbody = GorillaLocomotion.Player.Instance.bodyCollider.attachedRigidbody;
                    Vector3 forwardVector = rightHandR * Vector3.forward;
                    forwardVector.Normalize();
                    float forceMagnitude = 5f;
                    targetRigidbody.AddForce(forwardVector * forceMagnitude);
                    Debug.Log("Speed boost, applying velocity in direction " + forwardVector);
                }
                else
                {
                    speedCoolDown -= Time.deltaTime;
                }*/
                if (primaryL && admin)
                {
                    Debug.Log(playerSpeedAverage);
                }
            }
            playerSpeedV3 = GorillaLocomotion.GTPlayer.Instance.GetComponent<Rigidbody>().velocity;
            leftHandP = GTPlayer.Instance.leftControllerTransform.position;
            rightHandP = GTPlayer.Instance.rightControllerTransform.position;
            leftHandR = GTPlayer.Instance.leftControllerTransform.rotation;
            rightHandR = GTPlayer.Instance.rightControllerTransform.rotation;
            playerSpeedX = playerSpeedV3.x;
            playerSpeedY = playerSpeedV3.y;
            playerSpeedZ = playerSpeedV3.z;
            playerSpeedAverage = (playerSpeedX * playerSpeedY * playerSpeedZ) / 3;
            if (playerSpeedAverage < 0)
            {
                playerSpeedAverage = playerSpeedAverage * -1;
            }
            if (playerSpeedAverage == 0)
            {
                playerSpeedAverage = 1;
            }
        }

        void DeleteAllIce()
        {
            if (admin)
            {
                Debug.Log("Destroying Ice");
            }
            foreach (GameObject iceInstance in iceInstances)
            {
                // Use the object pool to return all ice instances
                iceObjectPool.ReturnIceInstance(iceInstance);
            }
            iceInstances.Clear();
        }
    }
}
