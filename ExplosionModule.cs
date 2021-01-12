namespace WNP78.Grenades
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Xml.Linq;

    using UnityEngine;

    using StressLevelZero.Pool;
    using StressLevelZero.Combat;

    [Serializable]
    public class ExplosionModule
    {
        public static Dictionary<string, Func<XElement, ExplosionModule, ExplosionAction>> CustomActions = new Dictionary<string, Func<XElement, ExplosionModule, ExplosionAction>>();

        public Grenade grenade;

        private List<ExplosionAction> actions = new List<ExplosionAction>();

        IEnumerator coroutine;

        [UnhollowerBaseLib.Attributes.HideFromIl2Cpp]
        public void Init(XElement xml, Grenade parent)
        {
            this.grenade = parent;

            ExplosionAction Process(XElement el)
            {
                switch (el.Name.LocalName)
                {
                    case "Effect":
                        return new Effect(el, this);
                    case "Force":
                        return new Force(el, this);
                    case "Shrapnel":
                        return new Shrapnel(el, this);
                    case "Audio":
                        return new Audio(el, this);
                    case "Despawn":
                        return new Despawn(el, this);
                    case "Transform":
                        return new Transformation(el, this);
                }

                if (CustomActions.ContainsKey(el.Name.LocalName))
                {
                    return CustomActions[el.Name.LocalName](el, this);
                }

                MelonLoader.MelonLogger.LogWarning("Unknown explosion effect: " + el.Name.LocalName);
                return null;
            }
            
            foreach (var el in xml.Elements())
            {
                var res = Process(el);
                if (res != null)
                {
                    this.actions.Add(res);
                }
            }
        }

        public void Explode()
        {
            this.coroutine = this.ExplodeCoro();
            MelonLoader.MelonCoroutines.Start(this.coroutine);
        }

        private IEnumerator ExplodeCoro()
        {
            foreach (var act in this.actions)
            {
                if (act.delay > 0f)
                {
                    yield return new WaitForSeconds(act.delay);
                }

                act.Run(this, this.grenade);
            }
        }

        public void Reset()
        {
            if (this.coroutine != null)
            {
                MelonLoader.MelonCoroutines.Stop(this.coroutine);
            }

            foreach (var action in this.actions)
            {
                action.Reset(this, this.grenade);
            }
        }

        void OnDestroy()
        {
            foreach (var action in this.actions)
            {
                if (action is IDisposable disp)
                {
                    disp.Dispose();
                }
            }
        }

        public abstract class ExplosionAction
        {
            public float delay;

            public ExplosionAction(XElement xml, ExplosionModule module)
            {
                this.delay = (float?)xml.Attribute("delay") ?? 0f;
            }

            public abstract void Run(ExplosionModule module, Grenade grenade);
            public virtual void Reset(ExplosionModule module, Grenade grenade) { }
        }

        public class Effect : ExplosionAction
        {
            public PoolSpawner.BlasterType type;
            public float scale;

            public Effect(XElement xml, ExplosionModule module) : base(xml, module)
            {
                if (!Enum.TryParse((string)xml.Attribute("type") ?? "", true, out this.type))
                {
                    this.type = PoolSpawner.BlasterType.Dust;
                }

                this.scale = (float?)xml.Attribute("scale") ?? 1f;
            }

            public override void Run(ExplosionModule module, Grenade grenade)
            {
                PoolSpawner.SpawnBlaster(this.type, grenade.transform.position, Quaternion.identity, Vector3.one * this.scale);
            }
        }

        public class Force : ExplosionAction
        {
            public float radius;
            public float force;
            public float upwardsModifier;
            public float duration;
            private IEnumerator coro;

            public Force(XElement xml, ExplosionModule module) : base(xml, module)
            {
                this.radius = (float?)xml.Attribute("radius") ?? 5f;
                this.force = (float?)xml.Attribute("force") ?? 15f;
                this.upwardsModifier = (float?)xml.Attribute("upwardsModifier") ?? 1f;
                this.duration = (float?)xml.Attribute("duration") ?? 0f;
            }

            public override void Run(ExplosionModule module, Grenade grenade)
            {
                List<Rigidbody> bodies = new List<Rigidbody>();
                foreach (var col in Physics.OverlapSphere(grenade.transform.position, this.radius))
                {
                    if (col.attachedRigidbody != null && !col.attachedRigidbody.isKinematic && !bodies.Contains(col.attachedRigidbody))
                    {
                        bodies.Add(col.attachedRigidbody);
                    }
                }

                if (duration > 0f)
                {
                    coro = RunCoro(grenade, bodies);
                    MelonLoader.MelonCoroutines.Start(coro);
                }
                else
                {
                    foreach (var body in bodies)
                    {
                        body?.AddExplosionForce(this.force, grenade.transform.position, this.radius, 1f, ForceMode.Impulse);
                    }
                }
            }

            public override void Reset(ExplosionModule module, Grenade grenade)
            {
                if (coro != null)
                {
                    MelonLoader.MelonCoroutines.Stop(coro);
                    coro = null;
                }
            }

            private IEnumerator RunCoro(Grenade grenade, List<Rigidbody> bodies)
            {
                float timer = 0f;
                while (timer < this.duration)
                {
                    foreach (var body in bodies)
                    {
                        body.AddExplosionForce(this.force, grenade.transform.position, this.radius, 1f, ForceMode.Acceleration);
                    }
                    yield return null;
                    timer += Time.deltaTime;
                }
            }
        }

        public class Shrapnel : ExplosionAction, IDisposable
        {
            public int count;
            public BulletObject projectile;

            public Shrapnel(XElement xml, ExplosionModule module) : base(xml, module)
            {
                this.projectile = ScriptableObject.CreateInstance<BulletObject>();

                if (!Enum.TryParse((string)xml.Attribute("cartType") ?? "", true, out Cart cart))
                {
                    cart = Cart.Cal_45;
                }

                var vars = new AmmoVariables()
                {
                    AttackDamage = (float?)xml.Attribute("damage") ?? 20f,
                    AttackType = AttackType.Piercing,
                    cartridgeType = cart,
                    ExitVelocity = (float?)xml.Attribute("velocity") ?? 50f,
                    ProjectileMass = (float?)xml.Attribute("mass") ?? 0.2f,
                    Tracer = (bool?)xml.Attribute("tracer") ?? false
                };

                this.projectile.ammoVariables = vars;
                this.count = (int?)xml.Attribute("count") ?? 100;
            }

            public override void Run(ExplosionModule module, Grenade grenade)
            {
                for (int i = 0; i < this.count; i++)
                {
                    PoolSpawner.SpawnProjectile(grenade.transform.position, UnityEngine.Random.rotation, this.projectile, grenade.name, null);
                }
            }

            public void Dispose()
            {
                UnityEngine.Object.Destroy(this.projectile);
            }
        }

        public class Audio : ExplosionAction
        {
            private AudioSource source;
            private Transform original;

            public Audio(XElement xml, ExplosionModule module) : base(xml, module)
            {
                source = module.grenade.transform.Find((string)xml.Attribute("path") ?? "Audio")?.GetComponent<AudioSource>();
                source.playOnAwake = false;
                original = source.transform.parent;
            }

            public override void Run(ExplosionModule module, Grenade grenade)
            {
                source.transform.parent = null;
                source.transform.position = grenade.transform.position;
                source.outputAudioMixerGroup = BoneworksModdingToolkit.Audio.sfxMixer;
                source.Play();
            }
        }

        public class Despawn : ExplosionAction
        {
            public Despawn(XElement xml, ExplosionModule module) : base(xml, module)
            {
            }

            public override void Run(ExplosionModule module, Grenade grenade)
            {
                grenade.gameObject.SetActive(false);
            }
        }

        public class Transformation : ExplosionAction
        {
            private Vector3 position;
            private Quaternion rotation;
            private Vector3 scale;

            private Vector3 originalPos;
            private Quaternion originalRot;
            private Vector3 originalScale;

            private Vector3 startPos;
            private Quaternion startRot;
            private Vector3 startScale;

            private float duration;
            private Transform transform;
            private IEnumerator coro;

            public Transformation(XElement xml, ExplosionModule module) : base(xml, module)
            {
                this.transform = module.grenade.transform.Find((string)xml.Attribute("path") ?? "");
                if (this.transform == null) { return; }

                this.position = GrenadesMod.ParseV3((string)xml.Attribute("position")) ?? transform.localPosition;
                this.rotation = Quaternion.Euler(GrenadesMod.ParseV3((string)xml.Attribute("rotation")) ?? transform.localEulerAngles);
                this.scale = GrenadesMod.ParseV3((string)xml.Attribute("scale")) ?? transform.localScale;
                this.duration = (float?)xml.Attribute("duration") ?? 0f;

                this.originalPos = this.transform.localPosition;
                this.originalRot = this.transform.localRotation;
                this.originalScale = this.transform.localScale;
            }

            public override void Run(ExplosionModule module, Grenade grenade)
            {
                if (this.transform == null)
                {
                    return;
                }

                if (this.duration > 0f)
                {
                    this.startPos = this.transform.localPosition;
                    this.startRot = this.transform.localRotation;
                    this.startScale = this.transform.localScale;
                    this.coro = this.RunCoro(module, grenade);
                    MelonLoader.MelonCoroutines.Start(this.coro);
                }
                else
                {
                    this.transform.localPosition = this.position;
                    this.transform.localRotation = this.rotation;
                    this.transform.localScale = this.scale;
                }
            }

            public override void Reset(ExplosionModule module, Grenade grenade)
            {
                if (this.coro != null)
                {
                    MelonLoader.MelonCoroutines.Stop(this.coro);
                    this.coro = null;
                }

                if (this.transform != null)
                {
                    this.transform.localPosition = this.originalPos;
                    this.transform.localRotation = this.originalRot;
                    this.transform.localScale = this.originalScale;
                }
            }

            private IEnumerator RunCoro(ExplosionModule module, Grenade grenade)
            {
                float timer = 0f;
                while (timer < this.duration)
                {
                    timer += Time.deltaTime;

                    float t = timer / this.duration;
                    this.transform.localPosition = Vector3.Lerp(this.startPos, this.position, t);
                    this.transform.localRotation = Quaternion.Lerp(this.startRot, this.rotation, t);
                    this.transform.localScale = Vector3.Lerp(this.startScale, this.scale, t);

                    yield return null;
                }
            }
        }
    }
}
