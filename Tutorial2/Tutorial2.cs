using Deadpan.Enums.Engine.Components.Modding;
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

        private List<CardDataBuilder> cards;
        private List<StatusEffectDataBuilder> statusEffects;
        private bool preLoaded = false;

        private StatusEffectDataBuilder StatusCopy(string oldName, string newName)
        {
            StatusEffectData data = Get<StatusEffectData>(oldName).InstantiateKeepName();
            data.name = newName;
            StatusEffectDataBuilder builder = data.Edit<StatusEffectData,StatusEffectDataBuilder>();
            builder.Mod = this;
            return builder;
        }

        private CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(Get<StatusEffectData>(name), amount);

        private void CreateModAssets()
        {
            statusEffects = new List<StatusEffectDataBuilder>();

            //Status 0: Summon Shade Snake
            statusEffects.Add(
                StatusCopy("Summon Fallow", "Summon Shade Snake")
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectSummon)data).summonCard = Get<CardData>("shadeSnake"); //Alternatively, I could've put mhcdc9.wildfrost.tutorial.shadeSnake
                })
                );
            //Debug.Log("[Tutorial] Summon Shade Snake Added.");

            //Status 1: Instant Summon Shade Snake
            statusEffects.Add(
                StatusCopy("Instant Summon Fallow", "Instant Summon Shade Snake")
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectInstantSummon)data).targetSummon = Get<StatusEffectData>("Summon Shade Snake") as StatusEffectSummon;
                })
                );
            //Debug.Log("[Tutorial] Instant Summon Shade Snake Added.");

            //Status 2: Summon Snake On Deploy
            statusEffects.Add(
                StatusCopy("When Deployed Summon Wowee", "When Deployed Summon Shade Snake")
                .WithText("When deployed, summon {0}")
                .WithTextInsert("<card=mhcdc9.wildfrost.tutorial.shadeSnake>")
                .SubscribeToAfterAllBuildEvent(delegate (StatusEffectData data)
                {
                    ((StatusEffectApplyXWhenDeployed)data).effectToApply = Get<StatusEffectData>("Instant Summon Shade Snake");
                })
                );
            //Debug.Log("[Tutorial] Summon Shade Snake When Deployed Added.");

            //Status 3: Trigger When Shade Serpent In Row Attacks
            statusEffects.Add(
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
                        ((StatusEffectTriggerWhenCertainAllyAttacks)data).ally = Get<CardData>("shadeSerpent");
                    })
                );
            //Debug.Log("[Tutorial] Trigger When Shade Serpent In Row Added.");


            cards = new List<CardDataBuilder>();

            //Card 0: Shade Snake
            cards.Add(
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
            cards.Add(
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
        public override void Load()
        {
            if (!preLoaded) { CreateModAssets(); } //The if statement is a flourish really. It makes the 2nd load of Load-Unload-Load faster.
            base.Load();
        }

        public override void Unload()
        {
            base.Unload();
        }

        public override List<T> AddAssets<T, Y>() //This method is called 6-7 times in base.Load() for each Builder. Can you name them all?
        {
            var typeName = typeof(T).Name;
            //Debug.Log("[Tutorial] " + typeName);
            switch(typeName)
            {
                case nameof(CardDataBuilder):
                    return cards.Cast<T>().ToList();
                case nameof(StatusEffectDataBuilder):
                    return statusEffects.Cast<T>().ToList();
                default:
                    return null;
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
