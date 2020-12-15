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
        public PinInteractionMode mode;

        public List<(Transform t, Quaternion normal, Quaternion held)> transforms = new List<(Transform, Quaternion, Quaternion)>();

        private Grip grip;

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        public void Init(XElement xml, Grenade grenade)
        {
            this.grip = this.GetComponent<Grip>();
            this.grenade = grenade;

            if (!Enum.TryParse((string)xml.Attribute("mode") ?? "ForceThreshold", out this.mode))
            {
                this.mode = PinInteractionMode.ForceThreshold;
            }

            if (this.mode == PinInteractionMode.PullDevice)
            {
                var pullDevice = this.GetComponentInParent<PullDevice>();
                pullDevice.OnHandlePull.AddListener((Action)this.OnHandlePulled);
            }
            else if (this.mode == PinInteractionMode.ForceThreshold)
            {
                this.pullForceSqr = (float?)xml.Attribute("pullForce") ?? 300f;
                this.pullForceSqr *= this.pullForceSqr;
            }

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

        private void OnGrabbed()
        {
            foreach (var t in this.transforms)
            {
                if (t.t != null)
                {
                    t.t.localRotation = t.held;
                }
            }
        }

        private void OnUngrabbed()
        {
            foreach (var t in this.transforms)
            {
                t.t.localRotation = t.normal;
            }
        }

        private void OnHandlePulled()
        {
            this.grip.GetHand()?.DetachJoint();
            this.OnPulled();
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
                    this.OnUngrabbed();
                }
            }
            else
            {
                foreach (var j in hand.GetComponents<ConfigurableJoint>())
                {
                    if (j.connectedBody?.transform?.root == this.transform.root)
                    {
                        joint = j;
                        this.OnGrabbed();
                        break;
                    }
                }
            }

            if (mode == PinInteractionMode.ForceThreshold && joint != null && joint.currentForce.sqrMagnitude > this.pullForceSqr)
            {
                hand?.DetachJoint();
                this.OnPulled();
            }
        }

        public enum PinInteractionMode
        {
            ForceThreshold,
            PullDevice
        }
    }
}
