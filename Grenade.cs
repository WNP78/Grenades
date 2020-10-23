namespace WNP78.Grenades
{
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

        public void Init(XElement xml)
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
        }

        public void OnPinPulled()
        {
            this.handle.Locked = false;
        }

        public void OnHandleReleased()
        {
            this.timer = this.fuseTime;
            this.ticking = true;
        }

        void Update()
        {
            if (this.ticking)
            {
                this.timer -= Time.deltaTime;

                if (this.timer <= 0f)
                {
                    this.Explode();
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
            this.gameObject.SetActive(false);
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
        }
    }
}
