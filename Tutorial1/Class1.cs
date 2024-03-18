using Deadpan.Enums.Engine.Components.Modding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tutorial1
{
    public class Tutorial1 : WildfrostMod
    {
        public Tutorial1(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.tutorial";

        public override string[] Depends => new string[0];

        public override string Title => "The 1st Tutorial of Many (or a Few)";

        public override string Description => "The goal of this tutorial is to create a modifier system (think daily voyage bell) and make it a mod.";

        protected override void Load()
        {
            base.Load();
            Events.OnCardDataCreated += BigBooshu;
            Events.OnCardDataCreated += ScaryEnemies;
        }

        protected override void Unload()
        {
            base.Unload();
            Events.OnCardDataCreated -= BigBooshu;
            Events.OnCardDataCreated -= ScaryEnemies;
        }


        
            
        private void BigBooshu(CardData cardData)
        {
            UnityEngine.Debug.Log("[Tutorial1] New CardData Created: " + cardData.name);
            if (cardData.name == "BerryPet")
            {
                cardData.hp = 99;
                cardData.damage = 99;
                UnityEngine.Debug.Log("[Tutorial1] Booshu!");
            }
        }

        //Don't forget to hook this method onto OnCardDataCreated in the Load and Unload methods. 
        private void ScaryEnemies(CardData cardData)
        {
            switch (cardData.cardType.name)
            {
                case "Miniboss":
                case "Boss":
                case "BossSmall":
                case "Enemy":
                    cardData.attackEffects = CardData.StatusEffectStacks.Stack(cardData.attackEffects, new CardData.StatusEffectStacks[1]
                    {
                        new CardData.StatusEffectStacks( Get<StatusEffectData>("Haze"), 1)
                    });
                break;
            }
        }
    }

}
