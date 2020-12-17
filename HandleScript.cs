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

        public static Dictionary<string, Func<IHandleElement>> Elements = new Dictionary<string, Func<IHandleElement>>
        {
            { "Rotate", () => new RotateHandle() },
            { "Animator", () => new AnimateHandle() },
        };

        public Grenade grenade;

        public Grip grip;

        public AudioSource audio;

        public float open;
        public float released;

        public float[] fingers;
        public float threshold;
        public float rotateSpeed;

        List<IHandleElement> elements;

        public bool Locked { [UnhollowerBaseLib.Attributes.HideFromIl2Cpp] get; [UnhollowerBaseLib.Attributes.HideFromIl2Cpp] set; } = true;

        private float angle = 0f;

        private bool hasReleased = false;

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        public void Init(XElement xml, Grenade grenade)
        {
            this.grenade = grenade;

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

            if (xml.Attribute("axis") != null) // legacy handling
            {
                var legacy = new RotateHandle();
                legacy.Init(xml, this);
                elements.Add(legacy);
            }

            foreach (var el in xml.Elements())
            {
                if (Elements.TryGetValue(el.Name.LocalName, out var factory))
                {
                    var x = factory();
                    x.Init(el, this);
                    this.elements.Add(x);
                }
            }
        }

        public void Reset()
        {
            this.Locked = true;
            this.angle = 0f;
            this.hasReleased = false;
            this.audio?.Stop();

            foreach (var e in elements)
            {
                e.Update(0f);
            }
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
                        this.Update(this.angle);
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

                    this.Update(this.angle);
                }
            }
        }

        void Update(float angle)
        {
            this.elements.ForEach(x => x.Update(angle));
        }

        public interface IHandleElement
        {
            void Init(XElement xml, HandleScript script);

            void Update(float angle);
        }

        class RotateHandle : IHandleElement
        {
            float factor;
            Vector3 axis;
            Transform transform;
            Quaternion originalRotation;

            public void Init(XElement xml, HandleScript handle)
            {
                transform = handle.grenade.transform.Find((string)xml.Attribute("path") ?? "HandleTransform");
                originalRotation = transform.localRotation;
                axis = GrenadesMod.ParseV3((string)xml.Attribute("axis") ?? "1,0,0") ?? Vector3.right;
                factor = (float?)xml.Attribute("factor") ?? 1f;
            }

            public void Update(float angle)
            {
                transform.localRotation = originalRotation * Quaternion.AngleAxis(angle * factor, axis);
            }
        }

        class AnimateHandle : IHandleElement
        {
            HandleScript handle;
            Animator animator;

            public void Init(XElement xml, HandleScript handle)
            {
                this.handle = handle;
                animator = handle.grenade.transform.Find((string)xml.Attribute("path") ?? "HandleAnimation")?.GetComponent<Animator>();
                animator.StopPlayback();
            }

            public void Update(float angle)
            {
                animator.playbackTime = angle / handle.released;
            }
        }
    }
}
