﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
    public class InternalCollider : IvaObject
    {
        public PrimitiveType ColliderType = PrimitiveType.Cube;

        private Rigidbody _rigidbody = null;
        private Rigidbody Rigidbody
        {
            get
            {
                if (_rigidbody == null)
                {
                    _rigidbody = IvaGameObject.GetComponent<Rigidbody>();
                    return _rigidbody;
                }
                return _rigidbody;
            }
        }

        public bool Visible
        {
            get
            {
                Renderer r = IvaGameObject.GetComponentCached<Renderer>(ref IvaGameObjectRenderer);
                if (r != null)
                {
                    return r.enabled;
                }
                else
                {
                    Debug.LogWarning("[FreeIVA] Tried get visibility of null renderer for " + IvaGameObject);
                }
                return false;
            }
            set
            {
                if (!AlwaysVisible)
                {
                    if (IvaGameObject != null)
                    {
                        Renderer r = IvaGameObject.GetComponentCached<Renderer>(ref IvaGameObjectRenderer);
                        if (r != null)
                        {
                            r.enabled = value;
                        }
                        else
                        {
                            Debug.LogWarning("[FreeIVA] Tried to change visibility of null renderer for " + IvaGameObject);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("[FreeIVA] Tried to change visibility of null IvaGameObject");
                    }
                }
            }
        }
        public bool AlwaysVisible = false;

        //public override void OnLoad(ConfigNode node)
        //{
        //    LoadFromCfg(node);
        //    Instantiate(this.part);
        //}

        public override void Instantiate(Part p)
        {
            Instantiate(p, ColliderType);
        }

        public void Instantiate(Part p, PrimitiveType colliderType)
        {
            Debug.Log("# Creating internal collider " + Name + " for part " + p);
            if (IvaGameObject != null)
            {
                Debug.LogError("[FreeIVA] InternalCollider " + Name + " has already been instantiated.");
                return;
            }
      
            // These values will be cleared on creating the object.
            Vector3 scale = Scale;
            Vector3 localPosition = LocalPosition;
            Quaternion rotation = Rotation;

            IvaGameObject = GameObject.CreatePrimitive(colliderType);
            IvaGameObject.layer = (int)Layers.InternalSpace;
            IvaGameObject.GetComponentCached<Collider>(ref IvaGameObjectCollider).enabled = true;
            //IvaGameObject.collider.isTrigger = true;
            if (p.internalModel == null)
                p.internalModel = new InternalModel();
            IvaGameObject.transform.parent = p.internalModel.transform;
            IvaGameObject.layer = (int)Layers.InternalSpace;
            IvaGameObject.transform.localScale = scale;
            IvaGameObject.transform.localPosition = localPosition;
            IvaGameObject.transform.localRotation = rotation;
            IvaGameObject.name = Name;
            PhysicMaterial physMat = IvaGameObjectCollider.material;
            physMat.bounciness = 0;
            //IvaGameObject.AddComponent<IvaCollisionPrinter>();
            //FixedJoint joint = IvaGameObject.AddComponent<FixedJoint>();
            //joint.connectedBody = p.collider.rigidbody;
            if (IvaGameObject.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = IvaGameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;

                rb.constraints = RigidbodyConstraints.FreezeAll;
            }

            IvaGameObject.GetComponentCached<Renderer>(ref IvaGameObjectRenderer);
            if (AlwaysVisible)
            {
                // Change the colour from the default white.
                IvaGameObjectRenderer.material.color = Color.grey;
            }
            else
                IvaGameObjectRenderer.enabled = false;
        }

        public void Enable(bool enabled)
        {
            if (IvaGameObject != null)
            {
                IvaGameObjectCollider = IvaGameObject.GetComponent<Collider>(); // Why get this every time?
            }
            if (IvaGameObjectCollider != null)
                IvaGameObjectCollider.enabled = enabled;
        }

        public static void HideAllColliders()
        {
            foreach (Part p in FlightGlobals.ActiveVessel.Parts)
            {
                ModuleFreeIva mfi = p.GetModule<ModuleFreeIva>();
                if (mfi != null)
                {
                    for (int i = 0; i < mfi.InternalColliders.Count; i++)
                    {
                        if (!mfi.InternalColliders[i].AlwaysVisible)
                            mfi.InternalColliders[i].Visible = false;
                    }
                }
            }
        }

        public static InternalCollider LoadFromCfg(ConfigNode node)
        {
            Vector3 position = Vector3.zero;
            if (!node.HasValue("type"))
            {
                Debug.LogWarning("[FreeIVA] InternalCollider type not found: Skipping collider.");
                return null;
            }
            InternalCollider internalCollider = new InternalCollider();
            internalCollider.ColliderType = (PrimitiveType)Enum.Parse(typeof(PrimitiveType), node.GetValue("type"));

            if (node.HasValue("name"))
            {
                internalCollider.Name = node.GetValue("name");
            }

            if (node.HasValue("alwaysVisible"))
            {
                internalCollider.AlwaysVisible = bool.Parse(node.GetValue("alwaysVisible"));
            }

            if (node.HasValue("position"))
            {
                string posString = node.GetValue("position");
                string[] p = posString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (p.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid collider position definition \"" + posString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    internalCollider.LocalPosition = new Vector3(float.Parse(p[0]), float.Parse(p[1]), float.Parse(p[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Collider position not found: Skipping collider.");
                return null;
            }

            if (node.HasValue("scale"))
            {
                string scaleString = node.GetValue("scale");
                string[] s = scaleString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length != 3)
                {
                    Debug.LogWarning("[FreeIVA] Invalid collider scale definition \"" + scaleString + "\": Must be in the form x, y, z.");
                    return null;
                }
                else
                    internalCollider.Scale = new Vector3(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Collider scale not found: Skipping collider.");
                return null;
            }

            if (node.HasValue("rotation"))
            {
                string rotationString = node.GetValue("rotation");
                string[] s = rotationString.Split(Utils.CfgSplitChars, StringSplitOptions.RemoveEmptyEntries);
                if (s.Length == 3)
                {
                    internalCollider.Rotation = Quaternion.Euler(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]));
                }
                else if (s.Length == 4)
                {
                    internalCollider.Rotation = new Quaternion(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2]), float.Parse(s[3]));
                }
                else
                {
                    Debug.LogWarning("[FreeIVA] Invalid collider rotation definition \"" + rotationString + "\": Must be in the form x, y, z or w, x, y, z.");
                    return null;
                }
            }
            else
            {
                Debug.LogWarning("[FreeIVA] Collider rotation not found: Using defaults.");
                internalCollider.Rotation = Quaternion.Euler(0, 0, 0);
            }
            return internalCollider;
        }
    }
}
