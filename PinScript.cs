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

        public List<(Transform t, Quaternion normal, Quaternion held)> transforms = new List<(Transform, Quaternion, Quaternion)>();

        private Grip grip;

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        public void Init(XElement xml, Grenade grenade)
        {
            this.grip = this.GetComponent<Grip>();
            this.grenade = grenade;

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
                        var q = Quaternion.Euler(GrenadesMod.ParseV3((string)el.Attribute("heldRotation")) ?? t.localEulerAngles);
                        transforms.Add((t, t.localRotation, q));
                    }
                }
            }
        }

        public void Reset()
        {
            foreach (var t in this.transforms)
            {
                t.t.gameObject.SetActive(true);
                t.t.localRotation = t.normal;
            }

            this.gameObject.SetActive(true);
        }

        private void OnPulled()
        {
            foreach (var t in this.transforms)
            {
                t.t.gameObject.SetActive(false);
            }

            this.grenade.OnPinPulled();
            this.gameObject.SetActive(false);
        }

        private ConfigurableJoint joint;
        
        void Update()
        {
            var hand = this.grip.GetHand();
            if (hand == null)
            {
                if (this.joint is object)
                {
                    this.joint = null;
                    foreach (var t in this.transforms)
                    {
                        t.t.localRotation = t.normal;
                    }
                }
            }
            else
            {
                foreach (var j in hand.GetComponents<ConfigurableJoint>())
                {
                    if (j.connectedBody.transform.root == this.transform.root)
                    {
                        joint = j;
                        foreach (var t in this.transforms)
                        {
                            t.t.localRotation = t.held;
                        }

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
