# Grenades
This is a mod for Boneworks that adds a framework for grenades, as well as some default grenades. This repository is specifically the framework. The default grenades can be found in the bonetome download [here](https://bonetome.com/boneworks/code/661/).

# Making Custom Grenades
I reccomend using the same workflow/project as you would use to make items with the [Custom Items SDK](https://bonetome.com/boneworks/code/211/), as it has a lot of useful resources like shaders etc. However there is a custom build script for grenades (you could use the normal asset bundle exporting workflow from custom items, but I find this easier because it can build a single bundle instead of all at once).
First, import the unity package that is in the mod downloads in UserData/Grenades/ and load the default grenades scene. Here, you can see the stock grenade prefabs, as well as a GameObject called "Exporter". This is for exporting assets into an AssetBundle.

There are 2 parts to making a custom grenade: the prefab, and the XML data. The prefab is the actual grenade object and is made in the normal way you would make a custom item for boneworks - with a rigidbody, an InteractableHost, colliders, grips, and meshes. The XML data is what makes it a grenade, and defines it's behaviour as a grenade. If you're only making a simple grenade, you could keep most of the XML the same from the default Grenades.xml

The grenade XML consists of a file in your project assets that must be called `Grenades.xml`. This should be an XML document with a root element called `Grenades`, which contains an element with the tag `Grenade` for each grenade you are using. Below is effectively a documentation of every option there is within this with example values.

```xml
<?xml version="1.0" encoding="utf-8" ?>
<Grenades>
  <Grenade name="Grenade Name" prefab="GrenadePrefab.prefab" fuse="5" pool="10" category="GADGETS">
    <!-- name - The name of the grenade, as it appears in the spawn menu. Also used for custom maps. -->
    <!-- prefab - The file name of the grenade's prefab to be loaded to the spawn gun. -->
    <!-- fuse - The fuse time, in seconds. -->
    <!-- pool - The size of this grenade's object pool. You cannot spawn more than this at one time. -->
    <!-- category - The cateogry of the spawn gun to show the grenade in. -->
    
    <Pin grip="PinGrip" pullForce="350" audio="PinSound">
      <!-- The pin element adds a pin behaviour to the grenade. Unless you detonate the grenade via some other mod script, you must have a pin to use the grenade.
           The pin is simply a grip with a force threshold. If the force acting between the hand and the grip exceeds this then it will be "pulled" and activate.
           At this point, if there is a handle element, then that handle element is unpinned. Else, the fuse starts ticking as soon as the pin is pulled. -->
      <!-- grip - the tranform path (can use forward slashes to go down the transform heirarchy) to the object which has a Grip script on it. -->
      <!-- pullForce - the force in newtons required to pull it -->
      <!-- audio - optional - the transform path to an object with an AudioPlayer on it, that will be played when the pin is pulled -->
    
      <Transform path="Ring" heldRotation="90,90,90"/>
      <Transform path="Pin"/>
        <!-- The Transform element refers to a transform that is part of the pin. As such it will be hidden when the pin is pulled.
             There is also the option to rotate it when the pin is being held. This is used for the ring on the on the stock frag grenade
             (which these example elements have been copied from) where the pin is rotated down against the side of the grenade by default
             but when it is held it rotates to face outwards so it looks as if it could be realistically held. -->
        <!-- path - The path to the transform. This will be hidden when the pin is pulled. -->
        <!-- heldRotation - optional - the rotation it should set itself to when held -->
    </Pin>
    
    <Handle grip="MainGrip" open="25" threshold="22" released="200" degreesPerSecond="700" fingers="0,1,1,1,0" audio="HandleSound">
      <!-- The handle (or "spoon") is a part of the grenade that triggers when the main grip on the grenade is released or relaxed (so when the grenade is thrown).
           It triggers the fuse countdown and can be customised in a few ways. It is also optional. -->
      <!-- grip - the path to the transform with the main grip on it. -->
      <!-- open - the angle in degrees that the handle would reach if the fingers were fully open. -->
      <!-- threshold - the angle, beyond which, that the handle will not return if it's held with the fingers closed. Past this point,
                       the handle has released and the fuse will start ticking. It will then proceed to move to the "released" angle -->
      <!-- released - the angle which the handle rotates to once it has been released. -->
      <!-- degreesPerSecond - the maximum rate at which the handle will rotate outwards. It can rotate inwards instantly to match the curl of the fingers, and 
                              outwards as much as the fingers let it, but no faster than this rate. -->
      <!-- fingers - This is a list of 5 numbers (correlating to 5 fingers, including thumb). If the number is zero, the finger does not affect the handle.
                     If you set it to something else, the finger curl value for that finger is multiplied by this before being used. This means that a lower
                     value would cause the handle to be held in less. -->
      <!-- audio - optional - the path to a gameobject with an audio player that will be played when the handle releases. -->
           
      <Rotate axis="1,0,0" path="HandlePivot" factor="1"/>
        <!-- The rotate element specifies the transform of the handle that rotates. There can be multiple of these. -->
        <!-- path - the path to the transform to be rotated -->
        <!-- axis - the axis to rotate around in local space -->
        <!-- factor - a coefficient that the angle will be rotated by for this transform -->
        
    </Handle>
    
    <Explode>
      <!-- This module is run when the grenade explodes. It runs a programmable sequence of actions with customisable delays and durations.
           The actions are run one after another, in the order you put them in the XML.
           Every action supports a parameter "delay" - this causes the execution to pause for this amount of time in seconds.
           During a delay, subsequent actions will not be executed.
           Some actions also have a "duration" attribute which will determine how long the action lasts. For these long-lasting actions,
           the subsequent actions will start immediately, without waiting for long lasting actions to complete. -->
    
      <Audio path="SoundEffect"/>
        <!-- This plays an audio player at the transform path specified. It will also be temporarily unparented and kept at the position of the explosion. -->
    
      <Force radius="7" force="15" upwardsModifier="1" duration="1.5" />
        <!-- This exerts an "explosive" force on nearby gameobjects. -->
        <!-- radius - the radius in metres that the blast should act in. -->
        <!-- force - the base force to be exerted. This scales based on distance. T
                     he type is impulse if there is no duration, and acceleration if there is duration. -->
        <!-- upwardsModifier - increasing this value tends to send things flying more upwards than outwards. -->
        <!-- duration - if this is set, the force acts over this period of seconds. This is used for the sucking
                        effect in the void grenade, along with a negative force value -->
      
      <Shrapnel count="300" damage="20" cartType="Cal_45" velocity="50" mass="0.2" tracer="false"/>
        <!-- This sends an array of bullets flying in all directions, to deal damage -->
        <!-- count - the number of bullets -->
        <!-- damage, velocity, mass, tracer - properties of the bullets -->
        <!-- cartType - also a property of the bullet, can be:
             "Cal5_5", "Cal_5_56x45", "Cal_9mm", "Cal_40", "Cal_45", "Cal_7_62x39", "Shot_12_Guage", "Balloon", "ArtilleryShell" or "Cal_308" -->
      
      <Effect type="Sparks" scale="10"/>
        <!-- This spawns one of boneworks' particle effects. -->
        <!-- type - the effect type. Can be:
             "BloodBag", "Wooden", "Paper", "Anime", "Dust", "Digitize", "Glass", "Sparks", "Steam" or "Confetti" -->
        <!-- scale - the scale of the effect -->
      
      <Transform path="void" scale="1000,1000,1000" rotation="60,90,100" duration="1.9"/>
        <!-- This changes the properties on a transform instantly or over a set duration of time -->
        <!-- path - the path to the transform. -->
        <!-- position - optional - the value to set the local position to -->
        <!-- rotation - optional - the value to set the local rotation to -->
        <!-- scale - optional - the value to set the local scale to -->
        <!-- duration - optional - the time these changes are lerp-ed over -->

      <Despawn />
        <!-- Simply makes the grenade disappear until it is respawned with the utility gun. -->
    </Explode>
```
