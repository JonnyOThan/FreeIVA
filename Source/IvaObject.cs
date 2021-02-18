using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
    public class IvaObject
    {
        public string Name { get; set; }

        public GameObject IvaGameObject = null;
        public Collider IvaGameObjectCollider;
        public Renderer IvaGameObjectRenderer;
        public Rigidbody IvaGameObjectRigidbody;
        private Vector3 _scale = Vector3.zero;
        public virtual Vector3 Scale
        {
            get
            {
                if (IvaGameObject != null)
                    return IvaGameObject.transform.localScale;
                return _scale;
            }
            set
            {
                if (IvaGameObject != null)
                    IvaGameObject.transform.localScale = value;
                _scale = value;
            }
        }
        private Vector3 _position = Vector3.zero;
        public virtual Vector3 LocalPosition
        {
            get
            {
                if (IvaGameObject != null)
                    return IvaGameObject.transform.localPosition;
                return _position;
            }
            set
            {
                if (IvaGameObject != null)
                    IvaGameObject.transform.localPosition = value;
                _position = value;
            }
        }

        private Quaternion _rotation = Quaternion.identity;
        public virtual Quaternion Rotation
        {
            get
            {
                if (IvaGameObject != null)
                {
                    IvaGameObject.transform.localRotation = _rotation;
                    return IvaGameObject.transform.localRotation;
                }
                return _rotation;
            }
            set
            {
                if (IvaGameObject != null)
                    IvaGameObject.transform.localRotation = value;
                _rotation = value;
            }
        }

        public virtual void Instantiate(Part p) { }
    }
}
