﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;
using System.Reflection;
using EditorGizmos;

/* TODO
 * Collisions
    * Find part collider - DONE
    * Find which part we are currently inside - DONE
    * Find out if we are intersecting the part - DONE
    * Pushback on collision - DONE
    * Collide with terrain or go on EVA when leaving the vessel
 * Acceleration
    * Gravity - Works in some cases
	* Vessel motion
 * Interaction/GUI
    * Get out of/into seat
        * Remove character model when moving - DONE
        * Identify seat locations - DONE
        * Perform crew transfer on selecting a new seat - DONE
        * Allow returning to the original seat - DONE
        * Add sound effect
    * Open/close hatch
        * Depth mask shader - DONE
        * Find hatch locations/shapes - DONE
        * Add interaction prompts for hatches - DONE
        * Add sound effect - DONE
        * Animation?
        * Don't allow reaching through structural colliders
    * Ladders / handles
    * Disable regular flight controls and reuse their keys - DONE
    * Prevent window focus areas dumping the player back in the seat with no way to leave again
    * Support switching the active kerbal (V key) properly
 * Camera without gimbal lock - DONE
 * Camera with gravity gimbal lock
 * EVA
    * Attachment over the top of external airlock hatches
    * Transition to EVA
 * Pause - DONE
 * Persist hatch states
 * Test in Editor (VAB/SPH)
 * Test with RPM transparent cockpits
 * Add internal windows (e.g. hatches)
 * Probe Control Room support
    * Change "Buckle" text to "Sit" or similar
    * Allow gravity override
 * Allow enabling / disabling individual kerbals / helmets
 * Mesh switcher: Add doorways when external nodes have approprate parts attached.
 */

/* IVA design tools
 * Toggle helmet
 */

/* Future projects
 * Cutaway internals view - Done in stock
 * Placeable IVA components
 * KerbCam movie tools
 */

/* Quick list
 * 
 * 2019:
 *   HideWhenOpen not working.
 *   Multiple parts -> colliders only show for the first part.
 *   PropHatch spawns in root part, not current part - Fixed
 *   Colliders drifting apart over time???
 *   World space colliders pushes vessels apart and destroys them.
 * 
 * ModuleFreeIva's OnStart currently only operates on the active vessel. Other craft switched to will have uninitialised hatches.
 * Fix issues with loading, restarting etc.
 * Finish colliders for the rest of the parts.
 * Fix render queue to view through mutliple parts and windows.
 * Fix startup states for hatches, hide colliders.
 * Set up visible filler colliders.
 * Split cfg files out to individual parts.
 * Move cfg entries to the Internal rather than the Part.
 * 
 */

// Assistance received from: egg
namespace FreeIva
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class FreeIva : MonoBehaviour
    {
        //public delegate void EventHandler();
        //public static event EventHandler OnIvaPartChanged;
        public static EventData<Part> OnIvaPartChanged = new EventData<Part>("OnIvaPartChanged");

        //private GameObject sphere;
        //private GameObject capsule;
        //private ScreenMessage screenMessage;
        public static Part InitialPart;
        public static Part CurrentPart;
        public static GameObject SelectedObject = null;
        private static ModuleFreeIva _currentModuleFreeIva = null;
        public static ModuleFreeIva CurrentModuleFreeIva
        {
            get
            {
                if (_currentModuleFreeIva == null || _currentModuleFreeIva.part != CurrentPart)
                {
                    _currentModuleFreeIva = CurrentPart.GetModule<ModuleFreeIva>();
                    return _currentModuleFreeIva;
                }
                return _currentModuleFreeIva;
            }
        }

        public void Start()
        {
            CurrentPart = FlightGlobals.ActiveVessel.rootPart;
            GuiUtils.DrawGui = true;

            //Utils.line = Utils.CreateLine(Color.white, Color.red, 0.5f, 0.5f);
            /*forwardLine = CreateLine(Color.white, Color.red, 0.05f, 0.05f);
            upLine = CreateLine(Color.white, Color.green, 0.05f, 0.05f);
            rightLine = CreateLine(Color.white, Color.blue, 0.05f, 0.05f);*/

            Paused = false;
            GameEvents.onGamePause.Add(OnPause);
            GameEvents.onGameUnpause.Add(OnUnPause);

            /*screenMessage = new ScreenMessage(string.Empty, 3f, ScreenMessageStyle.UPPER_CENTER);
            //screenMessage.message = "Starting Free IVA...";
            ScreenMessages.PostScreenMessage(screenMessage, false);*/

            // Per-frame screen message, as used during EVA:
            //ScreenMessages.PostScreenMessage("[" + GameSettings.EVA_Board.name + "]: Board", 0.1f, ScreenMessageStyle.LOWER_CENTER);
            Settings.LoadSettings();
            OnIvaPartChanged.Add(IvaPartChanged);
            SetRenderQueues(FlightGlobals.ActiveVessel.rootPart);
            SetCollisionLayers();


            /*xLine = Utils.CreateLine(Color.white, Color.red, 0.1f, 0.1f);
            yLine = Utils.CreateLine(Color.white, Color.green, 0.1f, 0.1f);
            zLine = Utils.CreateLine(Color.white, Color.blue, 0.1f, 0.1f);*/
        }

        /*static LineRenderer xLine;
        static LineRenderer yLine;
        static LineRenderer zLine;*/


        private GameObject hatchMask = null;
        private void DumpModels()
        {
            hatchMask = GameDatabase.Instance.GetModel("FreeIva/TestSphere");
            if (hatchMask != null)
            {
                //Debug.Log("#MeshFilter: " + hatchMask.GetComponent<MeshFilter>());
                //Debug.Log("#MeshRenderer: " + hatchMask.GetComponent<MeshRenderer>());

                //Debug.Log("#Creating hatchMask");
                GameObject hatchInstance = GameObject.Instantiate(hatchMask, FlightGlobals.ActiveVessel.rootPart.transform.position, Quaternion.identity) as GameObject;
                //Debug.Log("# instance null? " + (hatchInstance == null));
                hatchInstance.transform.parent = FlightCamera.fetch.transform;
                hatchInstance.transform.position = FlightCamera.fetch.transform.forward;
            }
        }

        public static bool Paused = false;
        public void OnPause()
        {
            Paused = true;
        }

        public void OnUnPause()
        {
            Paused = false;
        }

        public void OnDestroy()
        {
            GameEvents.onGamePause.Remove(OnPause);
            GameEvents.onGameUnpause.Remove(OnUnPause);
            OnIvaPartChanged.Remove(IvaPartChanged);
            InputLockManager.RemoveControlLock("FreeIVA");
        }

        public void FixedUpdate()
        {
            UpdateCurrentPart();
        }

        public static int DepthMaskQueue = 999;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="activePart">The part that the IVA player is currently inside.</param>
        public static void SetRenderQueues(Part activePart)
        {
            List<Part> visibleParts = new List<Part>();
            GetVisibleParts(CurrentPart, ref visibleParts);
            for (int i = 0; i < FlightGlobals.ActiveVessel.Parts.Count; i++)
            {
                Part p = FlightGlobals.ActiveVessel.Parts[i];
                bool partVisible = visibleParts.Contains(p);

                if (p.internalModel != null)
                {
                    Renderer[] renderers = p.internalModel.GetComponentsInChildren<Renderer>();
                    foreach (var r in renderers)
                    {
                        foreach (var m in r.materials)
                        {
                            // Geometry is rendered at 2000. The depth mask will be rendered over it at 1999.
                            // Render the next visible area (behind the depth mask) before it, over the top of it, at 1998.
                            if (m.shader.name.Contains("DepthMask") || r.name == "HatchDoor" || r.name == "hatchCombing" || r.name == "mk2CrewCabinExtHatchCut")
                            {
                                m.renderQueue = DepthMaskQueue;
                            }
                            else
                            {
                                if (p == activePart || !partVisible)
                                    m.renderQueue = 2000; // Hide the part the player is inside, and parts with closed hatches.
                                else
                                    m.renderQueue = DepthMaskQueue - 1; //1998;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Gets a list of parts that have
        /// </summary>
        /// <returns></returns>
        private static List<Part> GetVisibleParts(Part part, ref List<Part> visibleParts)
        {
            ModuleFreeIva iva = part.GetModule<ModuleFreeIva>();
            if (iva != null)
            {
                if (!visibleParts.Contains(part))
                    visibleParts.Add(part);

                for (int i = 0; i < iva.Hatches.Count; i++)
                {
                    Hatch h = iva.Hatches[i];

                    if (h.IsOpen && h.ConnectedHatch != null && h.ConnectedHatch.IsOpen &&
                        h.ConnectedHatch.Part != null && !visibleParts.Contains(h.ConnectedHatch.Part))
                        visibleParts.AddRange(GetVisibleParts(h.ConnectedHatch.Part, ref visibleParts));
                }
            }

            return visibleParts;
        }

        private void SetCollisionLayers()
        {
            for (int i = 0; i < 32; i++)
            {
                /*if (i == 20)
                {
                    Physics.IgnoreLayerCollision(20, i, false);
                    continue;
                }
                Physics.IgnoreLayerCollision(20, i, true);*/
                //Physics.IgnoreLayerCollision((int)Layers.Kerbals, i, true);
                //Physics.IgnoreLayerCollision(i, (int)Layers.Kerbals, true);
                //Physics.IgnoreLayerCollision((int)Layers.InternalSpace, i, true);
                //Physics.IgnoreLayerCollision(i, (int)Layers.InternalSpace, true);
                Physics.IgnoreLayerCollision((int)Layers.InternalSpace, i, true);
                Physics.IgnoreLayerCollision(i, (int)Layers.InternalSpace, true);
            }
            Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.Kerbals, false);
            Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.InternalSpace, false);
            Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.Kerbals, false);

            Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.LocalScenery, false);
            Physics.IgnoreLayerCollision((int)Layers.LocalScenery, (int)Layers.Kerbals, false);
            //Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.Kerbals, false);
            /*Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.InternalSpace, false);
            Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.PhysicalObjects, false);
            Physics.IgnoreLayerCollision((int)Layers.PhysicalObjects, (int)Layers.InternalSpace, false);
            Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.TerrainColliders, false);
            Physics.IgnoreLayerCollision((int)Layers.TerrainColliders, (int)Layers.InternalSpace, false);
            Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.TerrainColliders, false);
            Physics.IgnoreLayerCollision((int)Layers.TerrainColliders, (int)Layers.InternalSpace, false);*/
            //Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.Default, false);
            //Physics.IgnoreLayerCollision((int)Layers.Default, (int)Layers.InternalSpace, false);
            //Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.LocalScenery, false);
            //Physics.IgnoreLayerCollision((int)Layers.LocalScenery, (int)Layers.InternalSpace, false);

            //Physics.IgnoreLayerCollision((int)Layers.InternalSpace, (int)Layers.InternalSpace, true);
            //Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.Kerbals, true);
        }

        //private void DisableCollisionLayers()
        //{
        //    for (int i = 0; i < 32; i++)
        //    {
        //        Physics.IgnoreLayerCollision((int)Layers.Kerbals, i, true);
        //        Physics.IgnoreLayerCollision(i, (int)Layers.Kerbals, true);
        //        Physics.IgnoreLayerCollision((int)Layers.InternalSpace, i, true);
        //        Physics.IgnoreLayerCollision(i, (int)Layers.InternalSpace, true);
        //    }
        //    Physics.IgnoreLayerCollision((int)Layers.Kerbals, (int)Layers.InternalSpace, true);
        //}

        Vector3 _previousCameraPosition = Vector3.zero;
        public void UpdateCurrentPart()
        {
            if (InternalCamera.Instance == null)
            {
                Debug.LogError("InternalCamera was null");
                Debug.Log("Searching for camera: " + InternalCamera.FindObjectOfType<Camera>());
                return;
            }

            if (_previousCameraPosition == InternalCamera.Instance.transform.position)
                return;
            //Debug.Log("###########################");
            _previousCameraPosition = InternalCamera.Instance.transform.position;
            Vector3 camPos = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
            Part lastPart = CurrentPart;

            // Part colliders are larger than the parts themselves and overlap.
            // Find which of the containing parts we're nearest to.
            List<Part> possibleParts = new List<Part>();

            if (PartBoundsCamera(CurrentPart))
            {
                //Debug.Log("# Adding previous currentpart.");
                possibleParts.Add(CurrentPart);
            }
            // Check all attached parts.
            if (CurrentPart.parent != null && PartBoundsCamera(CurrentPart.parent))
            {
                //Debug.Log("# Adding parent " + CurrentPart.parent);
                possibleParts.Add(CurrentPart.parent);
            }
            foreach (Part c in CurrentPart.children)
            {
                if (PartBoundsCamera(c))
                {
                    //Debug.Log("# Adding child " + c);
                    possibleParts.Add(c);
                }
            }
            if (possibleParts.Count == 0)
            {
                //Debug.Log("# Zero connected parts found, checking everything.");
                foreach (Part p in FlightGlobals.ActiveVessel.parts)
                {
                    if (PartBoundsCamera(p))
                    {
                        //Debug.Log("# Adding vessel part " + p);
                        possibleParts.Add(p);
                    }
                }
            }

            if (possibleParts.Count == 0)
            {
                //Debug.Log("# No potential parts found");
                return;
            }

            if (possibleParts.Count == 1)
            {
                //Debug.Log("# Only one part found: " + possibleParts[0]);
                CurrentPart = possibleParts[0];
                if (CurrentPart != lastPart)
                    OnIvaPartChanged.Fire(CurrentPart);
                /*else
                    Debug.Log("# Same part as before: " + CurrentPart + " at " + CurrentPart.transform.position);*/
                return;
            }

            float minDistance = float.MaxValue;
            Part closestPart = null;
            //Debug.Log("# Checking " + possibleParts.Count + " possibilities.");
            foreach (Part pp in possibleParts)
            {
                // Raycast from the camera to the centre of the collider.
                // TODO: Figure out how to deal with multi-collider parts.
                Vector3 c = pp.collider.bounds.center;
                Vector3 direction = c - camPos;
                Ray ray = new Ray(camPos, direction);
                RaycastHit hitInfo;
                if (!pp.collider.Raycast(ray, out hitInfo, direction.magnitude))
                {
                    //Debug.Log("# Raycast missed part from inside: " + pp);
                    // Ray didn't hit the collider => we are inside the collider.
                    float dist = Vector3.Distance(pp.collider.bounds.center, camPos);
                    if (dist < minDistance)
                    {
                        closestPart = pp;
                        minDistance = dist;
                    }
                    /*else
                        Debug.Log("# Part was further away: " + minDistance + " vs part's " + dist);*/
                }
                /*else
                    Debug.Log("# Raycast hit part from outside: " + pp);*/
            }
            if (closestPart != null)
            {
                //Debug.Log("# New closest part found: " + closestPart);
                CurrentPart = closestPart;
                if (CurrentPart != lastPart)
                    OnIvaPartChanged.Fire(CurrentPart);
                /*else
                    Debug.Log("# Same part as before: " + CurrentPart + " at " + CurrentPart.transform.position);*/
            }
            /*else
                Debug.Log("# No closest part found.");*/
            // Keep the last part we were inside as the current part: We could be transitioning between hatches.
            // TODO: Idendify/store when we are outside all parts (EVA from IVA?).
        }

        public static bool PartBoundsCamera(Part p)
        {
            return GameObjectBoundsCamera(p.gameObject);
        }

        private static bool GameObjectBoundsCamera(GameObject go)
        {
            // The transform containing the mesh can be buried several levels deep.
            int childCount = go.transform.childCount;
            for (int i = 0; i < childCount; i++)
            {
                Transform child = go.transform.GetChild(i);
                if (child.name != "main camera pivot" && child.GetComponent<Part>() == null)
                {
                    GameObject goc = child.gameObject;
                    MeshFilter[] meshc = goc.GetComponents<MeshFilter>();
                    for (int m = 0; m < meshc.Length; m++)
                    {
                        Bounds meshBounds = meshc[m].mesh.bounds;
                        if (meshBounds != null)
                        {
                            Vector3 camPos = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.position);
                            // Bounds are relative to the transform position, not the world.
                            camPos -= goc.transform.position;

                            if (meshBounds.Contains(camPos))
                                return true;
                        }
                    }
                    bool foundGrandChild = GameObjectBoundsCamera(goc);
                    if (foundGrandChild)
                        return true;
                }
            }
            return false;
        }

        public static void UpdateCurrentPart(Part newCurrentPart)
        {
            if (FreeIva.CurrentPart != newCurrentPart)
            {
                CurrentPart = newCurrentPart;
                OnIvaPartChanged.Fire(CurrentPart);
            }
        }

        public void IvaPartChanged(Part newPart)
        {
            SetRenderQueues(newPart);
        }

        //static bool _initialised = false;
        public static void EnableInternals()
        {
            try
            {
                //if (_initialised) return;

                foreach (Part p in FlightGlobals.ActiveVessel.parts)
                {
                    if (p.internalModel == null)
                    {
                        p.CreateInternalModel();
                    }
                    if (p.internalModel != null)
                    {
                        p.internalModel.SetVisible(true);
                        PropHatch.AddPropHatches(p.internalModel);
                        //PrintInternals(p);
                    }
                }

                // TODO
                InternalCollider.HideAllColliders();
                // TODO
                Hatch.InitialiseAllHatchesClosed();

                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                // Colliders, hatches and hatch props are all unhidden by this.
                //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!


                //_initialised = true;
            }
            catch (Exception ex)
            {
                Debug.LogError("[FreeIVA] Error enabling internals: " + ex.Message + ", " + ex.StackTrace);
            }
        }

        /*public static float DoCollisions()
        {
            Part p = FlightGlobals.ActiveVessel.rootPart;
            if (p != null && p.collider != null)
            {
                /*
                Vector3 closestPoint = p.collider.ClosestPointOnBounds(InternalSpace.InternalToWorld(InternalCamera.Instance.transform.localPosition));
                float distance = Vector3.Distance(InternalSpace.InternalToWorld(InternalCamera.Instance.transform.localPosition), closestPoint);
                Debug.Log("Distance: " + distance + ", closest point: " + closestPoint);

                Vector3 closestPointExt = p.collider.ClosestPointOnBounds(InternalSpace.InternalToWorld(InternalCamera.Instance.transform.localPosition));
                float distanceExt = Vector3.Distance(InternalCamera.Instance.transform.localPosition, closestPoint);
                Debug.Log("DistanceExt: " + distanceExt + ", closest pointExt: " + closestPointExt);
                * /

                Vector3 camPosWorld = InternalSpace.InternalToWorld(InternalCamera.Instance.transform.forward) * 10;

                //line.SetPosition(0, InternalSpace.InternalToWorld(InternalCamera.Instance.transform.localPosition));
                //line.SetPosition(1, FlightGlobals.ActiveVessel.rootPart.transform.localPosition);
                RaycastHit hit;

                if (Physics.Linecast(InternalSpace.InternalToWorld(InternalCamera.Instance.transform.localPosition), camPosWorld, out hit)) // Progressively move the point forward, stopping everytime we see a new plane in the way.
                {
                    float distance = Vector3.Distance(InternalCamera.Instance.transform.localPosition, hit.point);
                    //Debug.Log("Distance: " + distance + ", hit point: " + hit.point);
                    return distance;
                }
                /*else
                {
                    Debug.Log("No hit");
                }* /

                //sphere.transform.position = InternalSpace.WorldToInternal(closestPoint);
            }
            return 0;
        }*/


        public static void PositionIvaObject(IvaObject o)
        {
            GUILayout.BeginHorizontal();
            if (o.IvaGameObject != null)
                o.IvaGameObjectRigidbody = o.IvaGameObject.GetComponent<Rigidbody>();

            GUILayout.Label("Position X");
            float xPos = float.Parse(GUILayout.TextField(o.LocalPosition.x.ToString()));
            GUILayout.Label("Position Y");
            float yPos = float.Parse(GUILayout.TextField(o.LocalPosition.y.ToString()));
            GUILayout.Label("Position Z");
            float zPos = float.Parse(GUILayout.TextField(o.LocalPosition.z.ToString()));
            if (xPos != o.LocalPosition.x || yPos != o.LocalPosition.y || zPos != o.LocalPosition.z)
            {
                //currentJoint = c.IvaGameObject.GetComponent<FixedJoint>();
                //if (currentJoint != null) Destroy(currentJoint);
                o.LocalPosition = new Vector3(xPos, yPos, zPos);
                //currentJoint = c.IvaGameObject.AddComponent<FixedJoint>();
                //currentJoint.connectedBody = CurrentPart.collider.rigidbody;
                if (o.IvaGameObjectRigidbody != null)
                {
                    o.IvaGameObjectRigidbody.velocity = Vector3.zero;
                    o.IvaGameObjectRigidbody.angularVelocity = Vector3.zero;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Scale X");
            float xSc = float.Parse(GUILayout.TextField(o.Scale.x.ToString()));
            GUILayout.Label("Scale Y");
            float ySc = float.Parse(GUILayout.TextField(o.Scale.y.ToString()));
            GUILayout.Label("Scale Z");
            float zSc = float.Parse(GUILayout.TextField(o.Scale.z.ToString()));
            if (xSc != o.Scale.x || ySc != o.Scale.y || zSc != o.Scale.z)
            {
                //currentJoint = c.IvaGameObject.GetComponent<FixedJoint>();
                //if (currentJoint != null) Destroy(currentJoint);
                o.Scale = new Vector3(xSc, ySc, zSc);
                //currentJoint = c.IvaGameObject.AddComponent<FixedJoint>();
                //currentJoint.connectedBody = CurrentPart.collider.rigidbody;
                if (o.IvaGameObjectRigidbody != null)
                {
                    o.IvaGameObjectRigidbody.velocity = Vector3.zero;
                    o.IvaGameObjectRigidbody.angularVelocity = Vector3.zero;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Rotation X");
            float xRot = float.Parse(GUILayout.TextField(o.Rotation.eulerAngles.x.ToString()));
            GUILayout.Label("Rotation Y");
            float yRot = float.Parse(GUILayout.TextField(o.Rotation.eulerAngles.y.ToString()));
            GUILayout.Label("Rotation Z");
            float zRot = float.Parse(GUILayout.TextField(o.Rotation.eulerAngles.z.ToString()));
            if (xRot != o.Rotation.eulerAngles.x || yRot != o.Rotation.eulerAngles.y || zRot != o.Rotation.eulerAngles.z)
            {
                //currentJoint = c.IvaGameObject.GetComponent<FixedJoint>();
                //if (currentJoint != null) Destroy(currentJoint);
                o.Rotation = Quaternion.Euler(xRot, yRot, zRot);
                //currentJoint = c.IvaGameObject.AddComponent<FixedJoint>();
                //currentJoint.connectedBody = CurrentPart.collider.rigidbody;
                if (o.IvaGameObjectRigidbody != null)
                {
                    o.IvaGameObjectRigidbody.velocity = Vector3.zero;
                    o.IvaGameObjectRigidbody.angularVelocity = Vector3.zero;
                }
            }
            GUILayout.EndHorizontal();


            /*Vector3 tmpPos = InternalSpace.InternalToWorld(o.LocalPosition);
            xLine.SetPosition(0, Vector3.zero);
            xLine.SetPosition(1, new Vector3(tmpPos.x, 0, 0));

            yLine.SetPosition(0, new Vector3(tmpPos.x, 0, 0));
            yLine.SetPosition(1, new Vector3(tmpPos.x, -tmpPos.y, 0));

            zLine.SetPosition(0, new Vector3(tmpPos.x, -tmpPos.y, 0));
            zLine.SetPosition(1, new Vector3(tmpPos.x, -tmpPos.y, -tmpPos.z));*/

            /*Vector3 invPos = -o.LocalPosition;
            GUILayout.Label("Position forward");
            float forward = float.Parse(GUILayout.TextField(o.LocalPosition.x.ToString()));
            GUILayout.Label("Position right");
            float right = float.Parse(GUILayout.TextField(o.LocalPosition.x.ToString()));
            GUILayout.Label("Position up");
            float up = float.Parse(GUILayout.TextField(o.LocalPosition.x.ToString()));*/
        }

        public void TestGizmos()
        {

            GizmoOffset gizmoOffset = new GizmoOffset();
            /*GizmoOffsetHandle.Setup(gizmoOffset, Callback<GizmoOffsetHandle,
                Vector3> onHandleDragStart,
            Callback<GizmoOffsetHandle,Vector3, float> onHandleDrag,
            Callback<GizmoOffsetHandle, Vector3, float> onHandleDragEnd,
            Camera referenceCamera);*/
            //Gizmos
        }
    } // End of class FreeIva

    /*[KSPAddon(KSPAddon.Startup.MainMenu, false)]
    class AutoStartup : UnityEngine.MonoBehaviour
    {
        public static bool first = true;
        public void Start()
        {
            //only do it on the first entry to the menu
            if (first)
            {
                first = false;
                HighLogic.SaveFolder = "test";
                var game = GamePersistence.LoadGame("persistent", HighLogic.SaveFolder, true, false);
                if (game != null && game.flightState != null && game.compatible)
                    FlightDriver.StartAndFocusVessel(game, game.flightState.activeVesselIdx);
                CheatOptions.InfinitePropellant = true;
                CheatOptions.InfiniteElectricity = true;
            }
        }
    }*/
}
