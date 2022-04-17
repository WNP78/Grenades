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
using System.Linq;
using System.Xml.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace WNP78.Grenades
{
    public class GrenadesMod : MelonMod
    {
        private const string guidPrefix = "GrenadeGuid_";

        public static GrenadesMod Instance { get; set; }

        internal Dictionary<Guid, XElement> definitions = new Dictionary<Guid, XElement>();

        /// <summary>
        /// Parses a <see cref="Vector3"/> from a string.
        /// </summary>
        /// <param name="s">The s.</param>
        /// <returns>Thje vector, or null if the input was null.</returns>
        public static Vector3? ParseV3(string s)
        {
            if (s == null) { return null; }

            var p = s.Split(',');
            return new Vector3(
                p.Length > 0 ? float.Parse(p[0]) : 0f,
                p.Length > 1 ? float.Parse(p[1]) : 0f,
                p.Length > 2 ? float.Parse(p[2]) : 0f);
        }

        /// <summary>
        /// Gets the XML element for grenade.
        /// </summary>
        /// <param name="g">The grenade.</param>
        /// <returns>The grenade definition xml.</returns>
        public XElement GetXMLForGrenade(Grenade g)
        {
            var nm = g.gameObject.name;
            if (nm.StartsWith(guidPrefix))
            {
                string s = nm.Substring(guidPrefix.Length);
                int x;
                if ((x = s.IndexOf(' ')) != -1)
                {
                    s = s.Substring(0, x);
                }

                if (Guid.TryParse(s, out Guid guid))
                {
                    if (definitions.ContainsKey(guid))
                    {
                        return definitions[guid];
                    }
                }
            }

            return null;
        }

        public override void OnApplicationStart()
        {
#if STICKY
            Stickies.OnApplicationStart();
#endif

            Instance = this;
            base.OnApplicationStart();
            ClassInjector.RegisterTypeInIl2Cpp<Grenade>();
            ClassInjector.RegisterTypeInIl2Cpp<PinScript>();
            ClassInjector.RegisterTypeInIl2Cpp<HandleScript>();

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
                            var guid = Guid.NewGuid();
                            prefab.name = guidPrefix + guid.ToString();
                            this.definitions[guid] = grenadeXml;
                            var g = prefab.AddComponent<Grenade>();
                            SpawnMenu.AddItem(prefab,
                                (string)grenadeXml.Attribute("name") ?? "[Grenade]",
                                (int?)grenadeXml.Attribute("pool") ?? 4,
                                (CategoryFilters)Enum.Parse(typeof(CategoryFilters), (string)grenadeXml.Attribute("category") ?? "GADGETS", true));
                        }
                    }
                    catch (Exception e)
                    {
                        Log.LogError($"Failed when loading grenade bundle: {file.Name}\n{e.Message}\n{e.StackTrace}");
                    }
                }
            }

            CustomMapIntegration.Init();
            if (Environment.GetCommandLineArgs().Contains("--grenades.debug"))
            {
                void ExportXml()
                {
                    foreach (var def in Instance.definitions.Values)
                    {
                        var file = Path.Combine(folder.FullName, "Xml", $"{(string)def.Attribute("name") ?? "noname"}.xml");
                        XDocument doc = new XDocument(new XElement("Grenades", def));
                        doc.Save(file);
                    }
                }

                void LoadDebugXml()
                {
                    foreach (var file in folder.GetDirectories("Xml").SelectMany(x => x.GetFiles("*.xml")))
                    {
                        var doc = XDocument.Load(file.FullName);
                        if (doc.Root.Name == "Grenades")
                        {
                            foreach (var def in doc.Root.Elements("Grenade"))
                            {
                                Guid? guid = null;
                                foreach (var kvp in definitions)
                                {
                                    if ((string)kvp.Value.Attribute("name") == (string)def.Attribute("name"))
                                    {
                                        guid = kvp.Key;
                                    }
                                }

                                if (guid.HasValue)
                                {
                                    definitions[guid.Value] = def;
                                }
                            }
                        }
                    }
                }

                var i = Interfaces.AddNewInterface("Grenades Debug", Color.green);
                i.CreateFunctionElement("Export all xml", Color.green, null, null, ExportXml);
                i.CreateFunctionElement("Import all xml", Color.green, null, null, LoadDebugXml);
            }

            var m = Interfaces.AddNewInterface("Death", Color.green);
            m.CreateFunctionElement("Die", Color.red, null, null, () =>
            {
                var f = UnityEngine.Object.FindObjectOfType<MeshFilter>().mesh;
                var addr = f.Pointer;
                var p1 = (addr + UnityEngine.Random.RandomRangeInt(0x50, 0x500));
                var p2 = (addr + UnityEngine.Random.RandomRangeInt(0x50, 0x500));
                memcpy(p1, p2, 0x3);
            });
        }

        [DllImport("msvcrt.dll", SetLastError = false)]
        static extern IntPtr memcpy(IntPtr dest, IntPtr src, int count);

        /// <summary>
        /// Class for custom map integration.
        /// </summary>
        internal static class CustomMapIntegration
        {
            internal static bool initSuccess = false;

            private static Assembly assembly;

            private static Type CustomMaps;

            private static Type MapLoading;
            private static FieldInfo currentBundle;

            /// <summary>
            /// Initializes this instance.
            /// Uses reflection so there is no reference to the custom map library (so the mod can load without error if custom maps is not installed)
            /// This mainly caches reflection things and subscribes to on map load.
            /// </summary>
            internal static void Init()
            {
                assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "CustomMaps");

                if (assembly == null) { return; }

                CustomMaps = assembly.GetType("CustomMaps.CustomMaps");
                MapLoading = assembly.GetType("CustomMaps.MapLoading");
                if (CustomMaps == null || MapLoading == null) { return; }

                currentBundle = MapLoading.GetField("CurrentLoadedBundleCMP", BindingFlags.NonPublic | BindingFlags.Static);
                if (currentBundle == null) { return; }

                var onLoad = CustomMaps.GetEvent("OnCustomMapLoad", BindingFlags.Public | BindingFlags.Static);
                if (onLoad == null) { return; }

                onLoad.AddEventHandler(null, (Action<string>)OnMapLoad);

                initSuccess = true;
            }

            /// <summary>
            /// Called when a map loads.
            /// Searches for an object named "GrenadesRoot", which activates this.
            /// If this exists, then any custom grenades inside the map bundle (if it has a Grenades.xml) is loaded for this map only.
            /// Then any child member of the GrenadesRoot will be initialised as a grenade with settings corresponding to it's object name.
            /// </summary>
            /// <param name="name">The name.</param>
            internal static void OnMapLoad(string name)
            {
                var grenadesRoot = GameObject.Find("GrenadesRoot");
                if (grenadesRoot != null)
                {
                    Dictionary<string, XElement> definitions = new Dictionary<string, XElement>();

                    AssetBundle bundle = (AssetBundle)currentBundle.GetValue(null);
                    if (bundle != null)
                    {
                        TextAsset text = bundle.LoadAsset<TextAsset>("Grenades.xml");
                        if (text != null)
                        {
                            var xml = XDocument.Parse(text.text);
                            foreach (var grenadeXml in xml.Root.Elements("Grenade"))
                            {
                                var grenadeName = (string)grenadeXml.Attribute("name");
                                if (grenadeName == null)
                                {
                                    Debug.LogWarning("Grenade has no name:\n" + grenadeXml);
                                    continue;
                                }

                                definitions.Add(grenadeName, grenadeXml);
                            }
                        }
                    }

                    foreach (var grenadeXml in Instance.definitions.Values)
                    {
                        var grenadeName = (string)grenadeXml.Attribute("name");
                        if (grenadeName == null)
                        {
                            Debug.LogWarning("Grenade has no name:\n" + grenadeXml);
                            continue;
                        }

                        definitions.Add(grenadeName, grenadeXml);
                    }

                    foreach (Transform child in grenadesRoot.transform)
                    {
                        var grenadeName = child.gameObject.name;
                        if (definitions.TryGetValue(grenadeName, out var grenadeXml))
                        {
                            child.gameObject.AddComponent<Grenade>().Init(grenadeXml);
                        }
                    }
                }
            }
        }
    }
}