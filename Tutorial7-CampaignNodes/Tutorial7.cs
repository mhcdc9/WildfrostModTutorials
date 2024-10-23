using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Tutorial7_CampaignNodes
{
    public class Tutorial7 : WildfrostMod
    {
        public override string GUID => "mhcdc9.wildfrost.tutorial";

        public override string[] Depends => new string[0];

        public override string Title => "Tutorial 7";

        public override string Description => "Learn how to make a simple map node.";

        internal static Tutorial7 instance;

        internal static GameObject PrefabHolder;

        public Tutorial7(string baseDirectory) : base(baseDirectory) 
        {
            instance = this;
        }

        public static List<object> assets = new List<object>();

        public bool preLoaded = false;

        internal CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);

        internal Sprite ScaledSprite(string fileName, int pixelsPerUnit = 100)
        {
            Texture2D tex = ImagePath(fileName).ToTex();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, (20f*pixelsPerUnit)/(tex.height*100f)), pixelsPerUnit);
        }

        internal T TryGet<T>(string name) where T : DataFile
        {
            T data;
            if (typeof(StatusEffectData).IsAssignableFrom(typeof(T)))
                data = base.Get<StatusEffectData>(name) as T;
            else
                data = base.Get<T>(name);

            if (data == null)
                throw new Exception($"TryGet Error: Could not find a [{typeof(T).Name}] with the name [{name}] or [{Extensions.PrefixGUID(name, this)}]");

            return data;
        }

        private void CreateModAssets()
        {
            //Put builder code here
            assets.Add(new CampaignNodeTypeBuilder(this)
                .Create<CampaignNodeTypePortal>("PortalNode")
                .WithZoneName("Portal")
                .WithCanEnter(true)
                .WithInteractable(true)
                .WithCanLink(true)
                .WithCanSkip(true) //If you want this node unskippable, replace this line with .WithMustClear(true)
                .WithLetter("p")
                .SubscribeToAfterAllBuildEvent(
                (data) =>
                {
                    MapNode mapNode = TryGet<CampaignNodeType>("CampaignNodeGold").mapNodePrefab.InstantiateKeepName(); //There's a lot of things in one of these prefabs
                    mapNode.name = GUID + ".Portal";
                    data.mapNodePrefab = mapNode;

                    StringTable uiText = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);
                    string key = mapNode.name + "Ribbon";
                    uiText.SetString(key, "Mysterious Portal");
                    mapNode.label.GetComponentInChildren<LocalizeStringEvent>().StringReference = uiText.GetString(key);
                    mapNode.spriteOptions = new Sprite[2] { ScaledSprite("MapPortalOpen1.png",200), ScaledSprite("MapPortalOpen2.png",200) };
                    mapNode.clearedSpriteOptions = new Sprite[2] { ScaledSprite("MapPortalClosed1.png", 200), ScaledSprite("MapPortalClosed2.png", 200) };

                    GameObject nodeObject = mapNode.gameObject;       //MapNode is a MonoBehaviour, so it must be attached to a GameObject.
                    UnityEngine.Object.DontDestroyOnLoad(nodeObject); //Ensures your reference doesn't poof out of existence.
                    nodeObject.transform.SetParent(PrefabHolder.transform);
                })
            );

            preLoaded = true;
        }

        public override void Load()
        {
            PrefabHolder = new GameObject("mhcdc9.wildfrost.tutorial");
            UnityEngine.Object.DontDestroyOnLoad(PrefabHolder);
            PrefabHolder.SetActive(false);

            if (!preLoaded) { CreateModAssets(); }
            base.Load();
            CreateLocalizedStrings();
            //AddToPopulator();
            //Events.OnCampaignLoadPreset += InsertPortalViaPreset;
            Events.OnSceneLoaded += InsertPortalViaSpecialEvent;
        }

        public override void Unload()
        {
            PrefabHolder.Destroy();

            base.Unload();
            //RemoveFromPopulator();
            //Events.OnCampaignLoadPreset -= InsertPortalViaPreset;
            Events.OnSceneLoaded -= InsertPortalViaSpecialEvent;
        }

        private void InsertPortalViaPreset(ref string[] preset)
        {
            //See References for the two possible presets.
            //Lines 0 + 1: Node types
            //Line 2: Battle Tier (fight 1, fight 2, etc)
            //Line 3: Zone (Snow Tundra, Ice Caves, Frostlands)
            char letter = 'S'; //S is for Snowdwell, b is for non-boss, B is for boss.
            int targetAmount = 1;

            for(int i=0; i < preset[0].Length; i++)
            {
                if (preset[0][i] == letter)
                {
                    targetAmount--;
                    if (targetAmount == 0)
                    {
                        preset[0] = preset[0].Insert(i + 1, "p");//"p" for portal
                        for(int j=1; j < preset.Length; j++)
                        {
                            preset[j] = preset[j].Insert(i + 1, preset[j][i].ToString()); //Whatever the ref node used
                        }
                        break; //Once the portal is placed, no need for other portals.
                    }
                }
            }
        }


        public int[] addToTiers = new int[] { 0, 1, 2, 3, 4 };//First two Acts
        public int amountToAdd = 2;
        public void AddToPopulator()
        {
            CampaignPopulator populator = TryGet<GameMode>("GameModeNormal").populator;
            foreach(int i in addToTiers)
            {
                CampaignTier tier = populator.tiers[i];
                List<CampaignNodeType> list = tier.rewardPool.ToList();
                for (int j=0; j<amountToAdd; j++)
                {
                    list.Add(TryGet<CampaignNodeType>("PortalNode"));                
                }

                tier.rewardPool = list.ToArray();
            }
        }

        public void RemoveFromPopulator()
        {
            CampaignPopulator populator = TryGet<GameMode>("GameModeNormal").populator;
            foreach (int i in addToTiers)
            {
                CampaignTier tier = populator.tiers[i];
                List<CampaignNodeType> list = tier.rewardPool.ToList();
                list.RemoveAll(x => x == null || x.ModAdded == this);
                tier.rewardPool = list.ToArray();
            }
        }

        public void InsertPortalViaSpecialEvent(Scene scene)//The Scene class is from the UnityEngine.SceneManagement namespace
        {
            if (scene.name == "Campaign")
            {
                SpecialEventsSystem specialEvents = GameObject.FindObjectOfType<SpecialEventsSystem>();//Only 1 of these exists among all scenes
                SpecialEventsSystem.Event eve = new SpecialEventsSystem.Event()
                {
                    requiresUnlock = null,
                    nodeType = TryGet<CampaignNodeType>("PortalNode"),
                    replaceNodeTypes = new string[] { "CampaignNodeReward" },
                    minTier = 2,
                    perTier = new Vector2Int(0,2),
                    perRun = new Vector2Int(2,4)
                };
                specialEvents.events = specialEvents.events.AddItem(eve).ToArray();
            }
        }

        //Credits to Hopeful for this method
        public override List<T> AddAssets<T, Y>()
        {
            if (assets.OfType<T>().Any())
                Debug.LogWarning($"[{Title}] adding {typeof(Y).Name}s: {assets.OfType<T>().Select(a => a._data.name).Join()}");
            return assets.OfType<T>().ToList();
        }

        //Call this method in Load()
        private void CreateLocalizedStrings()
        {
            string GoldKey = "mhcdc9.wildfrost.tutorial.PortalNode.GoldKey";
            string InjuryKey = "mhcdc9.wildfrost.tutorial.PortalNode.InjuryKey";
            string AllInjuryKey = "mhcdc9.wildfrost.tutorial.PortalNode.AllInjuryKey";
            string CharmKey = "mhcdc9.wildfrost.tutorial.PortalNode.CharmKey";

        StringTable uiText = LocalizationHelper.GetCollection("UI Text", SystemLanguage.English);
            uiText.SetString(GoldKey, "Found riches!");
            CampaignNodeTypePortal.GoldString = uiText.GetString(GoldKey);
            uiText.SetString(InjuryKey, "{0} injured");
            CampaignNodeTypePortal.InjuryString = uiText.GetString(InjuryKey);
            uiText.SetString(AllInjuryKey, "Too injured to continue");
            CampaignNodeTypePortal.AllInjuryString = uiText.GetString(AllInjuryKey);
            uiText.SetString(CharmKey, "Found {0}!");
            CampaignNodeTypePortal.CharmString = uiText.GetString(CharmKey);
        }
    }


}
