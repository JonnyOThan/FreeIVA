using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
    public class WorldCollisionTracker : MonoBehaviour
    {
        private Rigidbody _rbToApplyCollisionsTo;
        public void Initialise(Rigidbody rbToApplyCollisionsTo)
        {
            Debug.Log("# Starting WorldCollisionTracker");
            _rbToApplyCollisionsTo = rbToApplyCollisionsTo;
        }

        public void OnCollisionEnter(Collision collision)
        {
            Part collidingPart = collision.gameObject.GetComponent<Part>();
            if (collidingPart != null && FreeIva.CurrentPart == collidingPart)
            {
                return;
            }
            ScreenMessages.PostScreenMessage("World OnCollisionEnter " + name + " with " + collision.gameObject + " with force " + collision.impulse,
                1f, ScreenMessageStyle.LOWER_CENTER);
            _rbToApplyCollisionsTo.AddForce(collision.impulse, ForceMode.Impulse);
        }

        public void OnCollisionStay(Collision collision)
        {
            Part collidingPart = collision.gameObject.GetComponent<Part>();
            if (collidingPart != null && FreeIva.CurrentPart == collidingPart)
            {
                return;
            }
            ScreenMessages.PostScreenMessage("World OnCollisionStay " + name + " with " + collision.gameObject + " with force " + collision.impulse,
                1f, ScreenMessageStyle.LOWER_CENTER);
            _rbToApplyCollisionsTo.AddForce(collision.impulse, ForceMode.Impulse);
        }
    }
}
