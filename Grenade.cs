﻿namespace WNP78.Grenades
{
    using StressLevelZero.Interaction;
    using StressLevelZero.Props.Weapons;
    using System;
    using System.Xml.Linq;
    using UnityEngine;

    public class Grenade : MonoBehaviour
    {
        // Required for Il2CPP
        public Grenade(IntPtr ptr) : base(ptr) { }

        public ExplosionModule explosion;

        public HandleScript handle;

        public PinScript pin;

        public float fuseTime;

        private float timer;

        private bool ticking = false;

        private bool init = false;

        private WeaponSlot mySlot;

        public HandWeaponSlotReciever CurrentSlot { get; set; }

        private Action onPinPulled;

        public event Action OnPinPulledEvent
        {
            [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
            add
            {
                onPinPulled += value;
            }
            [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
            remove
            {
                onPinPulled -= value;
            }
        }


        private Action onHandleReleased;

        public event Action OnHandleReleasedEvent
        {
            [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
            add
            {
                onHandleReleased += value;
            }
            [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
            remove
            {
                onHandleReleased -= value;
            }
        }

        private void Start()
        {
            if (!init)
            {
                var xml = GrenadesMod.Instance.GetXMLForGrenade(this);
                if (xml != null)
                {
                    this.Init(xml);
                }
            }

            mySlot = this.GetComponent<WeaponSlot>();
            mySlot?.onSlotRemove.AddListener((Action)this.OnSlotRemove);
        }

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        internal void Init(XElement xml)
        {
            fuseTime = (float?)xml.Attribute("fuse") ?? 5f;

            XElement el = xml.Element("Explode");
            if (el != null)
            {
                this.explosion = new ExplosionModule();
                this.explosion.Init(el, this);
            }

            el = xml.Element("Handle");
            if (el != null)
            {
                var obj = this.transform.Find((string)el.Attribute("path") ?? "HandleTransform");
                if (obj != null)
                {
                    this.handle = obj.gameObject.AddComponent<HandleScript>();
                    this.handle.Init(el, this);
                }
            }

            el = xml.Element("Pin");
            if (el != null)
            {
                var obj = this.transform.Find((string)el.Attribute("grip") ?? "PinGrip");
                if (obj != null)
                {
                    this.pin = obj.gameObject.AddComponent<PinScript>();
                    this.pin.Init(el, this);
                }
            }

            init = true;
        }

        public void OnPinPulled()
        {
            if (this.handle != null)
            {
                this.handle.Locked = false;
            }
            else
            {
                this.timer = this.fuseTime;
                this.ticking = true;
            }

            this.onPinPulled?.Invoke();
        }

        public void OnHandleReleased()
        {
            this.timer = this.fuseTime;
            this.ticking = true;
            this.onHandleReleased?.Invoke();
        }

        void Update()
        {
            if (this.ticking)
            {
                this.timer -= Time.deltaTime;

                if (this.timer <= 0f)
                {
                    this.Explode();
                    this.ticking = false;
                }
            }
        }

        void OnEnable()
        {
            this.Reset();
        }

        void Explode()
        {
            this.explosion.Explode();
        }

        void Reset()
        {
            this.timer = 0f;
            this.ticking = false;
            this.pin?.Reset();
            this.explosion?.Reset();

            if (this.handle != null)
            {
                this.handle.Reset();
                this.handle.Locked = this.pin != null;
            }

            var rb = this.GetComponentInParent<Rigidbody>();
            if (rb != null)
            {
                rb.velocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }

            var host = this.GetComponent<InteractableHost>();
            host?.EnableInteraction();
            host?.EnableColliders();
            host?.EnableFarHover();
            RemoveFromSlots();
        }

        private void RemoveFromSlots()
        {
            if (this.CurrentSlot != null && this.CurrentSlot.m_SlottedWeapon == this.mySlot)
            {
                this.CurrentSlot.DropWeapon();
            }
        }

        void OnSlotRemove()
        {
            this.CurrentSlot = null;
        }
    }
}
