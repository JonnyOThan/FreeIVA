using System;
using System.Collections.Generic;
using UnityEngine;

namespace FreeIva
{
    public class PropHatch : Hatch
    {

        // TODO: Remove all non-prop hatches.

        public override Vector3 WorldPosition
        {
            get
            {
                if (ClosedProp != null)
                    return ClosedProp.transform.position;
                return Vector3.zero;
            }
        }

        public InternalProp ClosedProp;
        private MeshRenderer _closedRenderer = null;
        public MeshRenderer ClosedRenderer
        {
            get
            {
                if (_closedRenderer == null && ClosedProp != null)
                    _closedRenderer = ClosedProp.GetComponentInChildren<MeshRenderer>();
                return _closedRenderer;
            }
        }

        public InternalProp OpenProp;
        private MeshRenderer _openRenderer = null;
        public MeshRenderer OpenRenderer {
            get
            {
                if (_openRenderer == null && OpenProp != null)
                    _openRenderer = OpenProp.GetComponentInChildren<MeshRenderer>();
                return _openRenderer;
            }
        }

        public override bool IsOpen
        {
            get
            {
                if (ClosedRenderer != null)
                    return !ClosedRenderer.enabled;
                else
                    return false;
            }
        }

        public string ClosedPropName { get; set; }
        public string OpenPropName { get; set; }
        public int PropIndex { get; set; }

        public override void Instantiate(Part p)
        {
            Part = p;
            Debug.Log("# Instantiating prop hatch for part " + p);
            if (IvaGameObject != null)
            {
                Debug.LogError("[FreeIVA] Hatch has already been instantiated.");
                return;
            }

            GetProp();
            IvaGameObject = OpenProp.gameObject;
            IvaGameObject.layer = (int)Layers.InternalSpace;
            PropHatches.Add(this);

            SetupAudio();
        }

        private void GetProp()
        {
            InternalProp openHatch = PartLoader.GetInternalProp(this.OpenPropName);
            if (openHatch == null)
            {
                Debug.LogError("[FreeIVA] Unable to load open prop hatch \"" + this.OpenPropName + "\" in part " + this.Part.name);
            }
            else
            {
                openHatch.propID = FreeIva.CurrentPart.internalModel.props.Count;
                openHatch.internalModel = this.Part.internalModel;
                openHatch.transform.parent = this.Part.internalModel.transform;
                openHatch.hasModel = true;
                this.Part.internalModel.props.Add(openHatch);
                openHatch.transform.rotation = Quaternion.Euler(0, 0, 0); // TODO
                openHatch.transform.position = new Vector3(0, 0, 0.91f); // TODO
                MeshRenderer mrO = openHatch.GetComponentInChildren<MeshRenderer>();
                mrO.enabled = false;
                OpenProp = openHatch;
            }

            //InternalProp closedProp = this.Part.internalModel.props[PropIndex];
            InternalProp closedHatch = PartLoader.GetInternalProp(this.ClosedPropName);
            if (closedHatch == null)
            {
                Debug.LogError("[FreeIVA] Unable to load closed prop hatch \"" + this.ClosedPropName + "\" in part " + this.Part.name);
            }
            else
            {
                closedHatch.propID = FreeIva.CurrentPart.internalModel.props.Count;
                closedHatch.internalModel = this.Part.internalModel;
                closedHatch.transform.parent = this.Part.internalModel.transform;
                closedHatch.hasModel = true;
                this.Part.internalModel.props.Add(closedHatch);
                closedHatch.transform.rotation = Quaternion.Euler(0, 0, 0); // TODO
                closedHatch.transform.position = new Vector3(0, 0, 0.91f); // TODO
                MeshRenderer mrC = closedHatch.GetComponentInChildren<MeshRenderer>();
                mrC.enabled = true;
                ClosedProp = closedHatch;
            }
        }

        public override void Open(bool open)
        {
            if (ClosedRenderer != null)
                ClosedRenderer.enabled = !open;
            if (OpenRenderer != null)
                OpenRenderer.enabled = open;
            MeshRenderer[] meshRenderers = Part.internalModel.GetComponentsInChildren<MeshRenderer>();
            foreach (var hideProp in HideWhenOpen)
            {
                foreach (MeshRenderer mr in meshRenderers)
                {
                    if (mr.name.Equals(hideProp.Value) && mr.transform != null)
                    {
                        float error = Vector3.Distance(mr.transform.position, hideProp.Key);
                        if (error < 0.15)
                        {
                            Debug.Log("# Toggling " + mr.name);
                            mr.enabled = !open;
                            break;
                        }
                    }
                }
            }
            FreeIva.SetRenderQueues(FreeIva.CurrentPart);

            if (Collider != null)
                Collider.Enable(!open);

            if (open)
            {
                if (HatchOpenSound != null && HatchOpenSound.audio != null)
                    HatchOpenSound.audio.Play();
            }
            else
            {
                if (HatchCloseSound != null && HatchCloseSound.audio != null)
                    HatchCloseSound.audio.Play();
            }
        }

        public static List<PropHatch> PropHatches = new List<PropHatch>();

        // Can be safely called multiple times for the same part.
        public static void AddPropHatches(InternalModel internalModel)
        {



            // TODO!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            return;




            if (internalModel == null)
            {
                Debug.LogWarning("Unable to create prop hatches: internal model was null");
                return;
            }

            int propCount = internalModel.props.Count; // This list will be added to in the loop below.
            for (int i = 0; i < propCount; i++)
            {
                InternalProp prop = internalModel.props[i];
                if (prop.name == "Hatch_Plane" && !HatchInitialised(prop)) // TODO: Generalise this.
                {
                    InternalProp openHatch = PartLoader.GetInternalProp("Hatch_Plane_Frame");
                    openHatch.propID = FreeIva.CurrentPart.internalModel.props.Count;
                    openHatch.internalModel = FreeIva.CurrentPart.internalModel;
                    //openHatch.get_transform().set_parent(base.get_transform()); TODO: Set parent
                    openHatch.hasModel = true;
                    FreeIva.CurrentPart.internalModel.props.Add(openHatch);
                    openHatch.transform.rotation = prop.transform.rotation;
                    openHatch.transform.position = prop.transform.position;

                    MeshRenderer mr = openHatch.GetComponentInChildren<MeshRenderer>();
                    mr.enabled = false;

                    PropHatch propHatch = new PropHatch();
                    propHatch.ClosedProp = prop;
                    propHatch.OpenProp = openHatch;
                    PropHatches.Add(propHatch);
                }
            }
        }

        private static bool HatchInitialised(InternalProp prop)
        {
            foreach (PropHatch p in PropHatches)
            {
                if (p.ClosedProp.Equals(prop))
                    return true;
            }
            return false;
        }

        public static PropHatch LoadPropHatchFromCfg(ConfigNode node)
        {
            // TODO


            Vector3 position = Vector3.zero;
            Vector3 scale = Vector3.one;
            if (!node.HasValue("propIndex"))
            {
                Debug.LogWarning("[FreeIVA] Prop hatch propIndex not found: Skipping hatch.");
                return null;
            }
            PropHatch propHatch = new PropHatch();
            propHatch.PropIndex = int.Parse(node.GetValue("propIndex"));

            if (node.HasValue("closedPropName"))
                propHatch.ClosedPropName = node.GetValue("closedPropName");

            if (node.HasValue("openPropName"))
                propHatch.OpenPropName = node.GetValue("openPropName");

            if (node.HasValue("attachNodeId"))
                propHatch.AttachNodeId = node.GetValue("attachNodeId");

            if (node.HasValue("position"))
            {
                string posString = node.GetValue("position");
                string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid prop hatch position definition \"" + posString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    propHatch.LocalPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }

            if (node.HasValue("scale"))
            {
                string scaleString = node.GetValue("scale");
                string[] s = scaleString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid prop hatch scale definition \"" + scaleString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    propHatch.Scale = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }

            if (node.HasValue("rotation"))
            {
                string rotationString = node.GetValue("rotation");
                string[] s = rotationString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid prop hatch rotation definition \"" + rotationString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    propHatch.Rotation = Quaternion.Euler(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }

            if (node.HasNode("HideWhenOpen"))
            {
                ConfigNode[] hideNodes = node.GetNodes("HideWhenOpen");
                foreach (var hideNode in hideNodes)
                {
                    if (!hideNode.HasValue("name"))
                    {
                        Debug.LogWarning("[FreeIVA] HideWhenOpen name not found.");
                        continue;
                    }
                    string propName = hideNode.GetValue("name");

                    if (hideNode.HasValue("position"))
                    {
                        string posString = hideNode.GetValue("position");
                        string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                        if (p.Length != 3)
                        {
                            Debug.LogWarning("[FreeIVA] Invalid HideWhenOpen position definition \"" + posString + "\": Must be in the form x, y, z.");
                            continue;
                        }
                        else
                        {
                            Vector3 propPos = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
                            propHatch.HideWhenOpen.Add(new KeyValuePair<Vector3, string>(propPos, propName));
                        }
                    }

                }
            }

            if (node.HasNode("InternalCollider"))
            {
                ConfigNode hatchColliderNode = node.GetNode("InternalCollider");
                if (hatchColliderNode != null)
                    propHatch.Collider = InternalCollider.LoadFromCfg(hatchColliderNode);
            }
            return propHatch;
        }
    }
}
