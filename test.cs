#if STICKY
using System;
using System.Xml.Linq;
using UnityEngine;
using WNP78.Grenades;
using UnhollowerBaseLib.Attributes;
using UnhollowerRuntimeLib;

class StickyGrenadeAction : ExplosionModule.ExplosionAction
{
    float breakForce = 1000f;
    float breakTorque = 1000f;

    public StickyGrenadeAction(XElement xml, ExplosionModule explosionModule) : base (xml, explosionModule)
    {
        this.breakForce = (float?)xml.Attribute("breakForce") ?? this.breakForce;
        this.breakTorque = (float?)xml.Attribute("breakTorque") ?? this.breakTorque;
    }

    public override void Run(ExplosionModule module, Grenade grenade)
    {
        Debug.Log("Add sticky");
        var script = grenade.gameObject.AddComponent<StickyScript>();
        script.BreakForce = this.breakForce;
        script.BreakTorque = this.breakTorque;
    }

    public override void Reset(ExplosionModule module, Grenade grenade)
    {
        UnityEngine.Object.Destroy(grenade.gameObject.GetComponent<StickyScript>());
    }
}

class Stickies
{
    public static void OnApplicationStart()
    {
        ClassInjector.RegisterTypeInIl2Cpp<StickyScript>();
        ExplosionModule.CustomActions.Add("StickyGrenade", (xml, exp) => new StickyGrenadeAction(xml, exp));
    }
}

class StickyScript : MonoBehaviour
{
    // Required for Il2CPP
    public StickyScript(IntPtr ptr) : base(ptr) { }

    FixedJoint joint;

    public float BreakForce { [HideFromIl2Cpp] get; [HideFromIl2Cpp] set; }
    public float BreakTorque { [HideFromIl2Cpp] get; [HideFromIl2Cpp] set; }

    void OnCollisionEnter(Collision collision)
    {
        if (joint != null)
        {
            return;
        }

        Debug.Log("Stick");
        joint = gameObject.AddComponent<FixedJoint>();
        joint.breakForce = BreakForce;
        joint.breakTorque = BreakTorque;
        joint.connectedBody = collision.contacts[0].otherCollider.gameObject.GetComponentInParent<Rigidbody>();
    }

    void OnDestroy()
    {
        if (joint != null)
        {
            Destroy(joint);
        }
    }
}
#endif