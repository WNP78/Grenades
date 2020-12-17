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

        public AudioSource audio;

        public Quaternion closed;
        public Vector3 axis;
        public float open;
        public float released;

        public float[] fingers;
        public float threshold;
        public float rotateSpeed;

        Animator animationClip;

        public bool Locked { [UnhollowerBaseLib.Attributes.HideFromIl2Cpp] get; [UnhollowerBaseLib.Attributes.HideFromIl2Cpp] set; } = true;

        private float angle = 0f;

        private bool hasReleased = false;

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        public void Init(XElement xml, Grenade grenade)
        {
            this.grenade = grenade;
            this.closed = this.transform.localRotation;

            this.animationClip = grenade.transform.Find((string)xml.Attribute("animationClip") ?? "HandleAnimation")?.GetComponent<Animator>();
            this.axis = GrenadesMod.ParseV3((string)xml.Attribute("axis") ?? "1,0,0") ?? Vector3.right;
            this.open = (float?)xml.Attribute("open") ?? 30f;
            this.released = (float?)xml.Attribute("released") ?? 150f;
            this.threshold = (float?)xml.Attribute("threshold") ?? 27f;
            this.rotateSpeed = (float?)xml.Attribute("degreesPerSecond") ?? 100f;
            this.grip = grenade.transform.Find((string)xml.Attribute("grip") ?? "HandleGrip")?.GetComponent<Grip>();
            var fingers = (string)xml.Attribute("fingers");
            this.audio = grenade.transform.Find((string)xml.Attribute("audio") ?? "HandleSound")?.GetComponent<AudioSource>();
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
            this.angle = 0f;
            this.hasReleased = false;
            this.transform.localRotation = this.closed;
            this.audio?.Stop();
        }

        void Update()
        {
            if (!this.Locked)
            {
                if (!this.hasReleased && this.angle > this.threshold)
                {
                    this.hasReleased = true;
                    grenade.OnHandleReleased();
                    this.audio?.Play();
                }
                
                if (this.hasReleased)
                {
                    if (this.angle < this.released)
                    {
                        this.angle = Mathf.MoveTowards(this.angle, this.released, this.rotateSpeed * Time.deltaTime);
                        this.transform.localRotation = this.closed * Quaternion.AngleAxis(this.angle, this.axis);
                    }
                }
                else
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

                    var heldAngle = (1 - heldState) * this.open;

                    if (heldAngle < this.angle)
                    {
                        this.angle = heldAngle;
                    }
                    else
                    {
                        this.angle = Mathf.MoveTowards(this.angle, heldAngle, this.rotateSpeed * Time.deltaTime);
                    }

                    this.transform.localRotation = this.closed * Quaternion.AngleAxis(this.angle, this.axis);
                    if (this.animationClip != null)
                    {
                        this.animationClip.playbackTime = this.angle / this.released;
                    }
                }
            }
        }
    }
}
