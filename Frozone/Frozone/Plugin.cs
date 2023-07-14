using BepInEx;
using System;
using System.Runtime;
using UnityEngine;
using UnityEngine.XR;
using Utilla;
using GorillaLocomotion;

using System.IO;
using System.Text;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using Oculus.Platform.Models;
using Unity.Mathematics;
using System.Diagnostics.Contracts;

namespace Frozone
{
    /// <summary>
    /// This is your mod's main class.
    /// </summary>

    /* This attribute tells Utilla to look for [ModdedGameJoin] and [ModdedGameLeave] */
    [ModdedGamemode]
    [BepInDependency("org.legoandmars.gorillatag.utilla", "1.5.0")]
    [BepInPlugin(PluginInfo.GUID, PluginInfo.Name, PluginInfo.Version)]
    public class Plugin : BaseUnityPlugin
    {
        bool inRoom;
        public static GameObject icePrefab;
        public GameObject ice;

        public Vector3 leftHandP;
        public Quaternion leftHandR;
        public Vector3 rightHandP;
        public Quaternion rightHandR;
        public float leftTimer;
        public float rightTimer;
        public float coolDown = 1;

        private XRNode leftHandNode = XRNode.LeftHand;
        private XRNode rightHandNode = XRNode.RightHand;

        private bool leftOn = false;
        private bool rightOn = false;

        private bool isLeftPressed = false;
        private bool isRightPressed = false;

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
            /* Undo mod setup here */
            /* This provides support for toggling mods with ComputerInterface, please implement it :) */
            /* Code here runs whenever your mod is disabled (including if it disabled on startup)*/

            HarmonyPatches.RemoveHarmonyPatches();
        }

        void OnGameInitialized(object sender, EventArgs e)
        {
            /* Code here runs after the game initializes (i.e. GorillaLocomotion.Player.Instance != null) */
            try
            {
                Console.WriteLine("On Game Initialized ===========================");
                var bundle = LoadAssetBundle("Frozone.Resources.frozoneassets");
                foreach (var name in bundle.GetAllAssetNames())
                {
                    Console.WriteLine(name);
                }
                icePrefab = bundle.LoadAsset<GameObject>("Ice");
                icePrefab.SetActive(false);
                ice = Instantiate(icePrefab);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            try
            {
                Stream manifestResourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream($"Frozone.Resources.waterTexA.png");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message+"XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXx");
            }
        }

        void Update()
        {
            if (inRoom == true)
            {
                InputDevices.GetDeviceAtXRNode(leftHandNode).TryGetFeatureValue(CommonUsages.gripButton, out isLeftPressed);
                InputDevices.GetDeviceAtXRNode(rightHandNode).TryGetFeatureValue(CommonUsages.gripButton, out isRightPressed);

                if (leftTimer <= 0)
                {
                    if (isLeftPressed)
                    {
                        if (ice == null)
                        {
                            Debug.Log("LP = " + leftHandP);
                            Debug.Log("LR = " + leftHandR);
                            Debug.Log(ice);
                        }
                        else
                        {
                            leftTimer = coolDown;
                            Debug.Log("Left Pressed, Attempted spawn");
                            ice.transform.position = leftHandP;
                            ice.transform.rotation = leftHandR;
                            ice.SetActive(true);
                        }
                    }
                }
                else
                {
                    if (leftTimer != 0)
                    {
                        leftTimer -= Time.deltaTime;
                        Debug.Log(leftTimer);
                    }
                }

                if (rightTimer <= 0)
                {
                    if (isRightPressed)
                    {
                        if (ice == null)
                        {
                            Debug.Log("RP = " + rightHandP);
                            Debug.Log("RR = " + rightHandR);
                            Debug.Log(ice);
                        }
                        rightTimer = coolDown;
                        Debug.Log("Right Pressed, Attempted spawn");
                        ice.transform.position = rightHandP;
                        ice.transform.rotation = rightHandR;
                        ice.SetActive(true);
                    }
                }
                else
                {
                    rightTimer -= Time.deltaTime;
                    Debug.Log(rightTimer);
                }
            }
            leftHandP = Player.Instance.leftControllerTransform.position;
            rightHandP = Player.Instance.rightControllerTransform.position;
            leftHandR = Player.Instance.leftControllerTransform.rotation;
            rightHandR = Player.Instance.rightControllerTransform.rotation;
        }

        /* This attribute tells Utilla to call this method when a modded room is joined */
        [ModdedGamemodeJoin]
        public void OnJoin(string gamemode)
        {
            /* Activate your mod here */
            /* This code will run regardless of if the mod is enabled*/

            Debug.Log("Joined Modded Lobby");
            inRoom = true;
        }

        /* This attribute tells Utilla to call this method when a modded room is left */
        [ModdedGamemodeLeave]
        public void OnLeave(string gamemode)
        {
            /* Deactivate your mod here */
            /* This code will run regardless of if the mod is enabled*/

            Debug.Log("Left Modded Lobby");
            inRoom = false;
        }
    }
}
