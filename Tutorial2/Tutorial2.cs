using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tutorial2
{
    public class Tutorial2 : WildfrostMod
    {
        public Tutorial2(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.tutorial";

        public override string[] Depends => new string[0];

        public override string Title => "The Unit/Status Effect Tutorial";

        public override string Description => "The goal of this tutorial is to create two custom cards and two custom status effects.";

        private bool preLoaded = false;

        public static List<object> assets = new List<object>();

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

        private StatusEffectDataBuilder StatusCopy(string oldName, string newName)
        {
            StatusEffectData data = TryGet<StatusEffectData>(oldName).InstantiateKeepName();
            data.name = GUID + "." + newName;
            StatusEffectDataBuilder builder = data.Edit<StatusEffectData, StatusEffectDataBuilder>();
            builder.Mod = this;
            return builder;
        }

        private CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(TryGet<StatusEffectData>(name), amount);

        private void CreateModAssets()
        {
            //Code for Status Effects

            //Status 0: Summon Shade Snake
            assets.Add(
                StatusCopy("Summon Fallow", "Summon Shade Snake")
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectSummon)data).summonCard = TryGet<CardData>("shadeSnake"); //Alternatively, I could've put mhcdc9.wildfrost.tutorial.shadeSnake
                })
                );
            //Debug.Log("[Tutorial] Summon Shade Snake Added.");

            //Status 1: Instant Summon Shade Snake
            assets.Add(
                StatusCopy("Instant Summon Fallow", "Instant Summon Shade Snake")
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectInstantSummon)data).targetSummon = TryGet<StatusEffectData>("Summon Shade Snake") as StatusEffectSummon;
                })
                );
            //Debug.Log("[Tutorial] Instant Summon Shade Snake Added.");

            //Status 2: Summon Snake On Deploy
            assets.Add(
                StatusCopy("When Deployed Summon Wowee", "When Deployed Summon Shade Snake")
                .WithText("When deployed, summon {0}")
                .WithTextInsert("<card=mhcdc9.wildfrost.tutorial.shadeSnake>")
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectApplyXWhenDeployed)data).effectToApply = TryGet<StatusEffectData>("Instant Summon Shade Snake");
                })
                );
            //Debug.Log("[Tutorial] Summon Shade Snake When Deployed Added.");

            //Status 3: Trigger When Shade Serpent In Row Attacks
            assets.Add(
                new StatusEffectDataBuilder(this)
                .Create<StatusEffectTriggerWhenCertainAllyAttacks>("Trigger When Shade Serpent In Row Attacks")
                .WithCanBeBoosted(false)
                .WithText("Trigger when {0} in row attacks")
                .WithTextInsert("<card=mhcdc9.wildfrost.tutorial.shadeSerpent>")
                .WithType("")
                .FreeModify(
                    delegate(StatusEffectData data)
                    {
                        data.isReaction = true;
                        data.stackable = false;
                    })
                .SubscribeToAfterAllBuildEvent(
                    delegate(StatusEffectData data)
                    {
                        ((StatusEffectTriggerWhenCertainAllyAttacks)data).ally = TryGet<CardData>("shadeSerpent");
                    })
                );
            //Debug.Log("[Tutorial] Trigger When Shade Serpent In Row Added.");


            //Code for Cards

            //Card 0: Shade Snake
            assets.Add(
                new CardDataBuilder(this).CreateUnit("shadeSnake", "Shade Snake")
                .SetSprites("ShadeSnake.png", "ShadeSnake BG.png")
                .SetStats(4, 3, 0)
                .WithCardType("Summoned")
                .WithFlavour("Hissssssssss") //Should not show up anymore.
                //.SetStartWithEffect(SStack("Trigger When Ally In Row Attacks",1))
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                        SStack("Trigger When Shade Serpent In Row Attacks",1)
                    };
                })
                );

            //Card 1: Shade Serpent
            assets.Add(
                new CardDataBuilder(this).CreateUnit("shadeSerpent", "Shade Serpent")
                .SetSprites("ShadeSerpent.png", "ShadeSerpent BG.png")
                .SetStats(8,1,3)
                .WithCardType("Friendly")
                .AddPool("MagicUnitPool") //Shademancers
                .SubscribeToAfterAllBuildEvent(delegate (CardData data)
                {
                    data.startWithEffects = new CardData.StatusEffectStacks[1]
                    {
                        SStack("When Deployed Summon Shade Snake", 1)
                    };
                })
                );

            preLoaded = true;
        }
        protected override void Load()
        {
            if (!preLoaded) { CreateModAssets(); } //The if statement is a flourish really. It makes the 2nd load of Load-Unload-Load faster.
            base.Load();
        }

        protected override void Unload()
        {
            base.Unload();
            UnloadFromClasses();
        }

        //Credits to Hopeful for this method
        public override List<T> AddAssets<T, Y>()
        {
            if (assets.OfType<T>().Any())
                Debug.LogWarning($"[{Title}] adding {typeof(Y).Name}s: {assets.OfType<T>().Count()}");
            return assets.OfType<T>().ToList();
        }

        public void UnloadFromClasses()
        {
            List<ClassData> tribes = AddressableLoader.GetGroup<ClassData>("ClassData");
            foreach(ClassData tribe in tribes)
            {
                if (tribe == null || tribe.rewardPools == null) { continue; } //This isn't even a tribe; skip it.

                foreach(RewardPool pool in tribe.rewardPools)
                {
                    if (pool == null) { continue; }; //This isn't even a reward pool; skip it.

                    pool.list.RemoveAllWhere((item) => item == null || item.ModAdded == this); //Find and remove everything that needs to be removed.
                }
            }
        }
    }



    //Status Effect Class
    public class StatusEffectTriggerWhenCertainAllyAttacks : StatusEffectTriggerWhenAllyAttacks
    {
        //Cannot change allyInRow or againstTarget without some publicizing. Shade Snake is sad :(

        public CardData ally;

        public override bool RunHitEvent(Hit hit)
        {
            //Debug.Log(hit.attacker?.name);
            if (hit.attacker?.name == ally.name)
            {
                return base.RunHitEvent(hit);
            }
            return false;
        }
    }
}
