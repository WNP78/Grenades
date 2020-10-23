using System;
using EasyMenu;
using MelonLoader;
using UnityEngine;
using UnhollowerRuntimeLib;
using BoneworksModdingToolkit;
using Log = MelonLoader.MelonLogger;

using StressLevelZero.Data;

using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;

namespace WNP78.Grenades
{
    public class GrenadesMod : MelonMod
    {
        public static GrenadesMod Instance { get; set; }

        public override void OnApplicationStart()
        {
            Instance = this;
            base.OnApplicationStart();
            ClassInjector.RegisterTypeInIl2Cpp<Grenade>();

            var folder = new DirectoryInfo(Path.Combine(Path.GetDirectoryName(Application.dataPath), "UserData", "Grenades"));

            if (!folder.Exists)
            {
                folder.Create();
            }
            else
            {
                foreach (var file in folder.EnumerateFiles("*.grenade"))
                {
                    try
                    {
                        var bundle = AssetBundle.LoadFromFile(file.FullName);
                        bundle.hideFlags = HideFlags.DontUnloadUnusedAsset;
                        TextAsset text = bundle.LoadAsset<TextAsset>("Grenades.xml");
                        var xml = XDocument.Parse(text.text);

                        foreach (var grenadeXml in xml.Root.Elements("Grenade"))
                        {
                            var prefab = bundle.LoadAsset<GameObject>((string)grenadeXml.Attribute("prefab"));
                            prefab.hideFlags = HideFlags.DontUnloadUnusedAsset;
                            Shaders.ReplaceDummyShaders(prefab);
                            prefab.AddComponent<Grenade>().Init(grenadeXml);
                            SpawnMenu.AddItem(prefab,
                                (string)grenadeXml.Attribute("name") ?? "[Grenade]",
                                (int?)grenadeXml.Attribute("pool") ?? 4,
                                (CategoryFilters)Enum.Parse(typeof(CategoryFilters), (string)grenadeXml.Attribute("category") ?? "GADGETS", true));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.LogError($"Failed when loading grenade bundle: {file.Name}\n{e.Message}");
                    }
                }
            }
        }
    }
}
