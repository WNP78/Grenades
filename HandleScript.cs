namespace WNP78.Grenades
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    using UnityEngine;

    using StressLevelZero.Interaction;

    [Serializable]
    public class HandleScript : MonoBehaviour
    {
        public HandleScript(IntPtr ptr) : base(ptr) { }

        public Grenade grenade;

        public Grip grip;

        public Quaternion closed;
        public Quaternion open;
        public Quaternion released;

        public float[] fingers;
        public float threshold;
        public float rotateSpeed;

        public bool Locked { [UnhollowerBaseLib.Attributes.HideFromIl2Cpp] get; [UnhollowerBaseLib.Attributes.HideFromIl2Cpp] set; } = true;

        private float handleState = 1f;

        private bool hasReleased = false;

        private bool rotationComplete = false;

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        public void Init(XElement xml, Grenade grenade)
        {
            this.grenade = grenade;
            this.closed = this.transform.localRotation;
            this.open = grenade.transform.Find((string)xml.Attribute("open") ?? "HandleOpen")?.localRotation ?? this.closed;
            this.released = grenade.transform.Find((string)xml.Attribute("released") ?? "HandleReleased")?.localRotation ?? this.closed;
            this.threshold = (float?)xml.Attribute("threshold") ?? 0.1f;
            this.rotateSpeed = (float?)xml.Attribute("degreesPerSecond") ?? 100f;
            this.grip = grenade.transform.Find((string)xml.Attribute("grip") ?? "HandleGrip")?.GetComponent<Grip>();
            var fingers = (string)xml.Attribute("fingers");
            if (fingers != null)
            {
                var strings = fingers.Split(',');
                this.fingers = new float[strings.Length];
                for (int i = 0; i < strings.Length; i++)
                {
                    if (!float.TryParse(strings[i], out this.fingers[i]))
                    {
                        this.fingers[i] = 0f;
                    }
                }
            }
        }

        public void Reset()
        {
            this.Locked = true;
            this.handleState = 1f;
            this.hasReleased = false;
            this.rotationComplete = false;
            this.transform.localRotation = this.closed;
        }

        void Update()
        {
            if (!this.Locked)
            {
                float heldState = 0f;

                var hand = this.grip?.GetHand();
                if (hand != null)
                {
                    var fingerStates = hand.fingerCurl.axis;
                    heldState = 0f;
                    for (int i = 0; i < fingerStates.Length; i++)
                    {
                        heldState = Mathf.Max(heldState, fingerStates[i] * this.fingers[i]);
                    }
                }

                if (!this.hasReleased && this.handleState < this.threshold)
                {
                    this.hasReleased = true;
                    grenade.OnHandleReleased();
                }
                
                if (this.hasReleased)
                {
                    if (!this.rotationComplete)
                    {
                        this.transform.localRotation = Quaternion.RotateTowards(this.transform.localRotation, this.released, this.rotateSpeed * Time.deltaTime);
                        if (Mathf.Approximately(Quaternion.Angle(this.transform.localRotation, this.released), 0f))
                        {
                            this.rotationComplete = true;
                        }
                    }
                }
                else
                {
                    if (heldState < this.handleState)
                    {
                        this.handleState = heldState;
                    }
                    else
                    {
                        this.handleState = Mathf.MoveTowards(this.handleState, heldState, this.rotateSpeed / Quaternion.Angle(this.closed, this.open));
                    }

                    this.transform.localRotation = Quaternion.Lerp(this.open, this.closed, this.handleState);
                }
            }
        }
    }
}
