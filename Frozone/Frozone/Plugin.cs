using BepInEx;
using System;
using System.Runtime;
using UnityEngine;
using UnityEngine.XR;
using Utilla;
using GorillaLocomotion;
using GorillaTag;
using GorillaExtensions;

using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Oculus.Platform.Models;
using Unity.Mathematics;
using System.Diagnostics.Contracts;
using HarmonyLib;
using GorillaLocomotion.Swimming;

namespace Frozone
{
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool inRoom;
        public GameObject icePrefab;
        public List<GameObject> iceInstances = new List<GameObject>();

        public Vector3 leftHandP;
        public Quaternion leftHandR;
        public Vector3 rightHandP;
        public Quaternion rightHandR;
        public Quaternion Offset = Quaternion.Euler(-90f, 0f, 90f);
        public float leftTimer;
        public float rightTimer;
        public float coolDown = 1;

        private XRNode leftHandNode = XRNode.LeftHand;
        private XRNode rightHandNode = XRNode.RightHand;

        private bool isLeftPressed = false;
        private bool isRightPressed = false;
        public bool primary = false;

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
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }


        void Update()
        {
            if (inRoom == true)
            {
                InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.primaryButton, out primary);
                InputDevices.GetDeviceAtXRNode(leftHandNode).TryGetFeatureValue(CommonUsages.gripButton, out isLeftPressed);
                InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.gripButton, out isRightPressed);

                if (leftTimer <= 0)
                {
                    if (isLeftPressed)
                    {
                        leftTimer = coolDown;
                        Debug.Log("Left Pressed, Attempted spawn");

                        GameObject newIce = Instantiate(icePrefab, leftHandP, Quaternion.Euler(leftHandR.eulerAngles + Offset.eulerAngles));
                        iceInstances.Add(newIce);
                        newIce.SetActive(true);
                    }
                }
                else
                {
                    leftTimer -= Time.deltaTime;
                    Debug.Log(leftTimer);
                }

                if (rightTimer <= 0)
                {
                    if (isRightPressed)
                    {
                        rightTimer = coolDown;
                        Debug.Log("Right Pressed, Attempted spawn");

                        GameObject newIce = Instantiate(icePrefab, rightHandP, Quaternion.Euler(rightHandR.eulerAngles + Offset.eulerAngles));
                        iceInstances.Add(newIce);
                        newIce.SetActive(true);
                    }
                }
                else
                {
                    rightTimer -= Time.deltaTime;
                    Debug.Log(rightTimer);
                }
                if (primary)
                {
                    DeleteAllIce();

                }
            }

            leftHandP = Player.Instance.leftControllerTransform.position;
            rightHandP = Player.Instance.rightControllerTransform.position;
            leftHandR = Player.Instance.leftControllerTransform.rotation;
            rightHandR = Player.Instance.rightControllerTransform.rotation;
        }

        void DeleteAllIce()
        {
            Debug.Log("Destorying Ice");
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
