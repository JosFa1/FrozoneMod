using BepInEx;
using System;
using UnityEngine;
using UnityEngine.XR;
using Utilla;
using GorillaLocomotion;
using GorillaTag;
using GorillaExtensions;
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
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        private bool inRoom;
        public GameObject icePrefab;
        public List<GameObject> iceInstances = new List<GameObject>();
        private Vector3 leftHandP;
        private Quaternion leftHandR;
        private Vector3 rightHandP;
        private Quaternion rightHandR;
        private Quaternion Offset = Quaternion.Euler(90f, 180f, 0f);
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
            Utilla.Events.GameInitialized += OnGameInitialized;
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
        }

        void OnDisable()
        {
            DeleteAllIce();
            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            try
            {
                Console.WriteLine("======================================================");
                var bundle = LoadAssetBundle("Frozone.Resources.frozonebundle");
                foreach (var name in bundle.GetAllAssetNames())
                {
                    Console.WriteLine(name);
                }
                icePrefab = bundle.LoadAsset<GameObject>("ice");
                icePrefab.SetActive(false);
                icePrefab.AddComponent<GorillaSurfaceOverride>();
                GorillaSurfaceOverride surfaceOverride = icePrefab.GetComponent<GorillaSurfaceOverride>();
                surfaceOverride.overrideIndex = 59;
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
            if (inRoom)
            {
                //InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.secondaryButton, out secondaryR);
                if (admin == true)
                {
                    InputDevices.GetDeviceAtXRNode(leftHandNode).TryGetFeatureValue(CommonUsages.primaryButton, out primaryL);
                }
                InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.primaryButton, out primaryR);
                InputDevices.GetDeviceAtXRNode(leftHandNode).TryGetFeatureValue(CommonUsages.gripButton, out isLeftPressed);
                InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.gripButton, out isRightPressed);

                if (leftTimer <= 0)
                {
                    if (isLeftPressed)
                    {
                        leftTimer = coolDown;
                        Debug.Log("Left Pressed, Attempted spawn");
                        GameObject newIce = Instantiate(icePrefab, leftHandP, leftHandR);
                        iceInstances.Add(newIce);
                        newIce.SetActive(true);
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
                        GameObject newIce = Instantiate(icePrefab, rightHandP, rightHandR);
                        iceInstances.Add(newIce);
                        newIce.SetActive(true);
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
            playerSpeedV3 = GorillaLocomotion.Player.Instance.GetComponent<Rigidbody>().velocity;
            leftHandP = Player.Instance.leftControllerTransform.position;
            rightHandP = Player.Instance.rightControllerTransform.position;
            leftHandR = Player.Instance.leftControllerTransform.rotation;
            rightHandR = Player.Instance.rightControllerTransform.rotation;
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
                Destroy(iceInstance);
            }
            iceInstances.Clear();
        }

        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            Debug.Log("Joined Modded Lobby");
            inRoom = true;
        }

        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            Debug.Log("Left Modded Lobby");
            inRoom = false;
            DeleteAllIce();
        }
    }
}
