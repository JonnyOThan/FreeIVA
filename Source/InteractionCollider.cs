using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
    public class InteractionCollider : InternalModule
    {
        public string triggerTransform;
        public override void OnLoad(ConfigNode node)
        {
            if (node.HasValue("triggerTransform"))
            {
                triggerTransform = node.GetValue("triggerTransform");
            }
            else
            {
                Debug.LogError("[FreeIVA] InteractionCollider: No activateTransform found.");
                return;
            }
        }
        GameObject triggerObject;

        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight == false)
                return;

            Transform transform = internalProp.FindModelTransform(triggerTransform);
            if (transform != null)
            {
                triggerObject = transform.gameObject;
                if (triggerObject != null)
                {
                    ClickWatcher clickWatcher = triggerObject.GetComponent<ClickWatcher>();
                    if (clickWatcher == null)
                        clickWatcher = triggerObject.AddComponent<ClickWatcher>();

                    clickWatcher.AddMouseDownAction(() => OnMouseDown());
                    clickWatcher.AddMouseOverAction(() => OnMouseOver());
                    clickWatcher.AddMouseUpAction(() => OnMouseUp());
                }
            }
        }

        public void OnMouseDown()
        {
            ScreenMessages.PostScreenMessage("Interacted with collider " + triggerTransform + "!", 1f, ScreenMessageStyle.LOWER_CENTER);
            if (triggerObject != null)
            {
                KerbalIva.HoldItem(triggerObject.transform.parent);
            }
            
        }

        public void OnMouseOver()
        {
            ScreenMessages.PostScreenMessage("Hovered over collider " + triggerTransform + "!", 1f, ScreenMessageStyle.LOWER_CENTER);
        }

        public void OnMouseUp()
        {
            ScreenMessages.PostScreenMessage("Mouse up from collider " + triggerTransform + "!", 1f, ScreenMessageStyle.LOWER_CENTER);
            KerbalIva.DropHeldItem();
        }
    }
}
