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

        public void Init(XElement xml, Grenade grenade)
        {
            this.grenade = grenade;

            this.pullForceSqr = (float?)xml.Attribute("pullForce") ?? 300f;
            this.pullForceSqr *= this.pullForceSqr;

            this.transforms.Clear();
            foreach (var el in xml.Elements("Transform"))
            {
                var t = grenade.transform.Find((string)el.Attribute("path") ?? "PinTransformDefault");
                if (t != null)
                {
                    transforms.Add(t);
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

        private void OnHandAttachedUpdate(Hand hand)
        {
            if (hand.joint.currentForce.sqrMagnitude > this.pullForceSqr)
            {
                hand.DetachJoint();
                this.OnPulled();
            }
        }
    }
}
