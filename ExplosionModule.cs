namespace WNP78.Grenades
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Linq;

    using UnityEngine;

    using StressLevelZero.Pool;
    using StressLevelZero.Combat;

    [Serializable]
    public class ExplosionModule
    {
        private Transform _audioParent;

        public Grenade grenade;

        public List<Effect> effects = new List<Effect>();

        public Force? force;

        public Shrapnel? shrapnel;

        public AudioSource audio;

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        public void Init(XElement xml, Grenade parent)
        {
            this.grenade = parent;
            this.effects.Clear();

            foreach (var ef in xml.Elements("Effect"))
            {
                if (Enum.TryParse<PoolSpawner.BlasterType>((string)ef.Attribute("type") ?? "", true, out var type))
                {
                    this.effects.Add(new Effect()
                    {
                        type = type,
                        scale = (float?)ef.Attribute("scale") ?? 1f
                    });
                }
            }

            var el = xml.Element("Force");
            if (el != null)
            {
                this.force = new Force()
                {
                    radius = (float?)el.Attribute("radius") ?? 10f,
                    force = (float?)el.Attribute("force") ?? 10f,
                    upwardsModifier = (float?)el.Attribute("upwardsModifier") ?? 1f
                };
            }
            else
            {
                this.force = null;
            }

            el = xml.Element("Shrapnel");
            if (el != null)
            {
                var bullet = ScriptableObject.CreateInstance<BulletObject>();

                if (!Enum.TryParse((string)el.Attribute("cartType") ?? "", true, out Cart cart))
                {
                    cart = Cart.Cal_45;
                }

                var vars = new AmmoVariables()
                {
                    AttackDamage = (float?)el.Attribute("damage") ?? 20f,
                    AttackType = AttackType.Piercing,
                    cartridgeType = cart,
                    ExitVelocity = (float?)el.Attribute("velocity") ?? 50f,
                    ProjectileMass = (float?)el.Attribute("mass") ?? 0.2f,
                    Tracer = (bool?)el.Attribute("tracer") ?? false
                };

                bullet.ammoVariables = vars;

                this.shrapnel = new Shrapnel()
                {
                    count = (int?)el.Attribute("count") ?? 100,
                    projectile = bullet
                };
            }
            else
            {
                this.shrapnel = null;
            }

            el = xml.Element("Audio");
            if (el != null)
            {
                var source = grenade.transform.Find((string)el.Attribute("path") ?? "Audio");
                this.audio = source?.GetComponent<AudioSource>();
                this._audioParent = source?.parent;
            }
        }

        public void Explode()
        {
            if (this.force != null)
            {
                var force = this.force.Value;
                List<Rigidbody> bodies = new List<Rigidbody>();
                foreach (var col in Physics.OverlapSphere(grenade.transform.position, force.radius))
                {
                    if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic && !bodies.Contains(col.attachedRigidbody))
                    {
                        bodies.Add(col.attachedRigidbody);
                    }
                }

                foreach (var body in bodies)
                {
                    body.AddExplosionForce(force.force, grenade.transform.position, force.radius, 1f, ForceMode.Impulse);
                }
            }

            foreach (var effect in effects)
            {
                PoolSpawner.SpawnBlaster(effect.type, grenade.transform.position, Quaternion.identity, Vector3.one * effect.scale);
            }

            if (this.shrapnel != null)
            {
                var shrapnel = this.shrapnel.Value;
                for (int i = 0; i < shrapnel.count; i++)
                {
                    PoolSpawner.SpawnProjectile(grenade.transform.position, UnityEngine.Random.rotation, shrapnel.projectile, grenade.name, null);
                }
            }

            if (this.audio != null)
            {
                this.audio.transform.parent = null;
                this.audio.transform.position = grenade.transform.position;
                this.audio.loop = false;
                this.audio.Play();
            }
        }

        public void Reset()
        {
            this.audio.Stop();
            this.audio.transform.parent = this._audioParent;
            this.audio.transform.position = grenade.transform.position;
        }

        [Serializable]
        public struct Effect
        {
            public PoolSpawner.BlasterType type;
            public float scale;
        }

        [Serializable]
        public struct Force
        {
            public float radius;
            public float force;
            public float upwardsModifier;
        }

        [Serializable]
        public struct Shrapnel
        {
            public int count;
            public BulletObject projectile;
        }
    }
}
