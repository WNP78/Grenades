namespace WNP78.Grenades
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    using UnityEngine;

    using StressLevelZero.Interaction;

    [Serializable]
    public class PinScript : MonoBehaviour
    {
        public PinScript(IntPtr ptr) : base(ptr) { }

        public Grenade grenade;
        public float pullForceSqr;

        public List<Transform> transforms = new List<Transform>();

        private Rigidbody rigidbody;
        private Grip grip;

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        public void Init(XElement xml, Grenade grenade)
        {
            this.grip = this.GetComponent<Grip>();
            this.grenade = grenade;
            this.rigidbody = this.GetComponentInParent<Rigidbody>();

            this.pullForceSqr = (float?)xml.Attribute("pullForce") ?? 300f;
            this.pullForceSqr *= this.pullForceSqr;

            this.transforms.Clear();
            foreach (var el in xml.Elements("Transform"))
            {
                string path = (string)el.Attribute("path");
                if (path != null)
                {
                    var t = grenade.transform.Find(path);
                    if (t != null)
                    {
                        transforms.Add(t);
                    }
                }
            }
        }

        public void Reset()
        {
            foreach (var t in this.transforms)
            {
                t.gameObject.SetActive(true);
            }
        }

        private void OnPulled()
        {
            foreach (var t in this.transforms)
            {
                t.gameObject.SetActive(false);
            }

            this.grenade.OnPinPulled();
        }

        private ConfigurableJoint joint;
        
        void Update()
        {
            var hand = this.grip.GetHand();
            if (hand == null)
            {
                this.joint = null;
            }
            else
            {
                foreach (var j in hand.GetComponents<ConfigurableJoint>())
                {
                    if (j.connectedBody == this.rigidbody)
                    {
                        joint = j;
                        break;
                    }
                }
            }

            if (joint != null && joint.currentForce.sqrMagnitude > this.pullForceSqr)
            {
                hand.DetachJoint();
                this.OnPulled();
            }
        }
    }
}
