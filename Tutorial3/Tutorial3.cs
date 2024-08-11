using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEditor;
using Rewired.Utils;
using UnityEngine.Events;
using Unity.Services.Analytics;
using System.IO;
using System.Collections;

namespace Tutorial3
{
    public class Tutorial3 : WildfrostMod
    {
        public Tutorial3(string modDirectory) : base(modDirectory)
        {
        }

        private CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);

        private T TryGet<T>(string name) where T : DataFile
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

        public override string GUID => "mhcdc9.wildfrost.tutorial";

        public override string[] Depends => new string[0];

        public override string Title => "The Charm/Keyword and more Effects Tutorial";

        public override string Description => "The goal of this tutorial is to create a charm with a keyword.";

        private List<StatusEffectDataBuilder> statusEffects;
        private List<CardUpgradeDataBuilder> cardUpgrades;
        private List<KeywordDataBuilder> keywords;
        private bool preLoaded = false;


        private void CreateModAssets()
        {
            keywords = new List<KeywordDataBuilder>();

            keywords.Add(
                new KeywordDataBuilder(this)
                .Create("glacial")
                .WithTitle("Glacial")
                .WithTitleColour(new Color(0.85f, 0.44f, 0.85f)) //Light purple
                .WithShowName(true) //Shows name in Keyword box (as opposed to a nonexistant icon).
                .WithDescription("Apply equal <keyword=snow> or <keyword=frost> when the other is applied|Does not cause infinites!") //Format is body|note.
                .WithNoteColour(new Color(0.85f, 0.44f, 0.85f)) //Somewhat teal
                .WithBodyColour(new Color(0.2f,0.5f,0.5f))
                .WithCanStack(false)
                );

            cardUpgrades = new List<CardUpgradeDataBuilder>();

            cardUpgrades.Add(
                new CardUpgradeDataBuilder(this)
                .CreateCharm("CardUpgradeGlacial")
                .WithType(CardUpgradeData.Type.Charm)
                .WithImage("GlacialCharm.png")
                .WithTitle("Glacial Charm")
                .WithText($"Gain <keyword={Extensions.PrefixGUID("glacial",this)}>") //Get allows me to skip the GUID. This does not.
                .WithTier(2) //Affects cost in shops
                .SubscribeToAfterAllBuildEvent(delegate (CardUpgradeData data)
                {
                    data.effects = new CardData.StatusEffectStacks[1] { SStack("Apply Equal Snow And Frost", 1) };
                    CardScriptChangeBackground script = ScriptableObject.CreateInstance<CardScriptChangeBackground>();
                    script.imagePath = this.ImagePath("Frostail BG.png");
                    data.scripts = new CardScript[1] { script };
                })
                );



            
            statusEffects = new List<StatusEffectDataBuilder>();

            statusEffects.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectMeldXandY>("Apply Equal Snow And Frost")
                .WithCanBeBoosted(false)
                .WithText($"<keyword={Extensions.PrefixGUID("glacial",this)}>")
                .WithType("")
                .FreeModify<StatusEffectMeldXandY>(delegate(StatusEffectMeldXandY data)
                {
                    data.statusType1 = "snow";
                    data.statusType2 = "frost";
                    data.effectToApply = TryGet<StatusEffectData>("Snow").InstantiateKeepName();
                    data.effectToApply2 = TryGet<StatusEffectData>("Frost").InstantiateKeepName();
                    data.eventPriority = 1;
                })
                );

            preLoaded = true;
        }


        protected override void Load()
        {
            if (!preLoaded) { CreateModAssets(); }
            base.Load();
        }

        protected override void Unload()
        {
            base.Unload();
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            switch (typeName)
            {
                case nameof(CardUpgradeData):
                    return cardUpgrades.Cast<T>().ToList();
                case nameof(KeywordData):
                    return keywords.Cast<T>().ToList();
                case nameof(StatusEffectData):
                    return statusEffects.Cast<T>().ToList();
                default:
                    return null;
            }
        }
    }


    public class StatusEffectMeldXandY : StatusEffectData
    {
        public string statusType1;

        public string statusType2;

        public StatusEffectData effectToApply;//effectToApply from StatusEffectApplyX

        public StatusEffectData effectToApply2;

        protected override void Init()
        {
            base.OnApplyStatus += Run;
        }

        private IEnumerator Run(StatusEffectApply apply)
        {
            if (apply.effectData.type == statusType1)
            {
                return StatusEffectSystem.Apply(apply.target, target, effectToApply2, apply.count);
            }
            if (apply.effectData.type == statusType2)
            {
                return StatusEffectSystem.Apply(apply.target, target, effectToApply, apply.count);
            }
            Debug.Log("[Tutorial] Unreachable Code?");
            return null; 
        }

        public override bool RunApplyStatusEvent(StatusEffectApply apply)
        {
            if ( (target?.enabled != null && apply.applier == target) 
                && (apply.effectData?.type == statusType1 || apply.effectData?.type == statusType2) 
                && !(apply.effectData == effectToApply || apply.effectData == effectToApply2) )
            {
                return (apply.count > 0);
            }
            return false;
        }
    }

    public class CardScriptChangeBackground : CardScript
    {
        public string imagePath;
        public override void Run(CardData target)
        {
            target.backgroundSprite = imagePath.ToSprite();
        }
    }
}
