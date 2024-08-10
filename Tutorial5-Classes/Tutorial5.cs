using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Tutorial5_Classes
{
    public class Tutorial5 : WildfrostMod
    {
        public override string GUID => "mhcdc9.wildfrost.tutorial";

        public override string[] Depends => new string[0];

        public override string Title => "Tutorial 5";

        public override string Description => "Learn how to combine a collection of cards, charms, and bells to make a tribe.";

        public static void AddComponent(string name)
        {
            Campaign.instance.gameObject.AddComponentByName(name);
        }

        public Tutorial5(string baseDirectory) : base(baseDirectory) { }

        public List<CardDataBuilder> cards = new List<CardDataBuilder>();
        public List<CardUpgradeDataBuilder> cardUpgrades = new List<CardUpgradeDataBuilder>();
        public List<StatusEffectDataBuilder> statusEffects = new List<StatusEffectDataBuilder>();
        public List<GameModifierDataBuilder> bells = new List<GameModifierDataBuilder>();
        public List<ClassDataBuilder> tribes = new List<ClassDataBuilder>();

        public bool preLoaded = false;

        private CardData.StatusEffectStacks SStack(string name, int amount) => new CardData.StatusEffectStacks(Get<StatusEffectData>(name), amount);
        private CardData.TraitStacks TStack(string name, int amount) => new CardData.TraitStacks(Get<TraitData>(name), amount);

        private CardDataBuilder CardCopy(string oldName, string newName)
        {
            CardData data = Get<CardData>(oldName).InstantiateKeepName();
            data.name = newName;
            CardDataBuilder builder = data.Edit<CardData, CardDataBuilder>();
            builder.Mod = this;
            return builder;
        }

        private ClassDataBuilder TribeCopy(string oldName, string newName)
        {
            ClassData data = Get<ClassData>(oldName).InstantiateKeepName();
            data.name = newName;
            ClassDataBuilder builder = data.Edit<ClassData, ClassDataBuilder>();
            builder.Mod = this;
            return builder;
        }

        private CardData[] CardList(params string[] names) => names.Select((s) => Get<CardData>(s)).ToArray();

        private void CreateModAssets()
        {
            cards.Add(CardCopy("Ruckus", "needleLeader")
                .WithCardType("Leader")
                .FreeModify(
                (data) =>
                {
                    CardScriptGiveUpgrade crown = ScriptableObject.CreateInstance<CardScriptGiveUpgrade>();
                    crown.name = "Give Crown";
                    crown.upgradeData = Get<CardUpgradeData>("Crown");
                    CardScriptAddRandomHealth health = ScriptableObject.CreateInstance<CardScriptAddRandomHealth>();
                    health.name = "Random Health";
                    health.healthRange = new Vector2Int(-2, 2);
                    CardScriptAddRandomDamage damage = ScriptableObject.CreateInstance<CardScriptAddRandomDamage>();
                    damage.name = "Give Damage";
                    damage.damageRange = new Vector2Int(-1, 1);
                    CardScriptAddRandomCounter counter = ScriptableObject.CreateInstance<CardScriptAddRandomCounter>();
                    counter.name = "Give Counter";
                    counter.counterRange = new Vector2Int(0, 1);
                    data.createScripts = new CardScript[] { crown, health, damage, counter };
                })
            );

            cards.Add(CardCopy("TrueFinalBoss6", "muncherLeader")
                .WithCardType("Leader")
                .SetStats(8,5,5)
                .FreeModify(
                (data) =>
                {
                    data.traits.Add(TStack("Draw", 1));

                    CardScriptGiveUpgrade crown = ScriptableObject.CreateInstance<CardScriptGiveUpgrade>();
                    crown.name = "Give Crown";
                    crown.upgradeData = Get<CardUpgradeData>("Crown");
                    CardScriptAddRandomHealth health = ScriptableObject.CreateInstance<CardScriptAddRandomHealth>();
                    health.name = "Random Health";
                    health.healthRange = new Vector2Int(-1, 3);
                    CardScriptAddRandomDamage damage = ScriptableObject.CreateInstance<CardScriptAddRandomDamage>();
                    damage.name = "Give Damage";
                    damage.damageRange = new Vector2Int(0, 2);
                    CardScriptAddRandomCounter counter = ScriptableObject.CreateInstance<CardScriptAddRandomCounter>();
                    counter.name = "Give Counter";
                    counter.counterRange = new Vector2Int(-1, 1);
                    data.createScripts = new CardScript[] { crown, health, damage, counter };
                })
            );

            cards.Add(CardCopy("Wrenchy", "superMuncher")
                .WithTitle("Super Muncher")
                .FreeModify(
                (data) =>
                {
                    data.traits.Add(TStack("Draw", 2));
                })
            );

            cardUpgrades.Add(new CardUpgradeDataBuilder(this)
                .Create("CardUpgradeSuperDraw")
                .WithTitle("Quickdraw Charm")
                .WithType(CardUpgradeData.Type.Charm)
                .WithImage(ImagePath("blueDraw.png"))
                .SetTraits(TStack("Draw",2), TStack("Consume",1))
                .FreeModify(
                (data) =>
                {
                    TargetConstraintIsItem item = ScriptableObject.CreateInstance<TargetConstraintIsItem>();
                    item.name = "Is Item";
                    TargetConstraintHasTrait consume = ScriptableObject.CreateInstance<TargetConstraintHasTrait>();
                    consume.name = "Does Not Have Consume";
                    consume.trait = Get<TraitData>("Consume");
                    consume.not = true;
                    data.targetConstraints = new TargetConstraint[] {item, consume};
                })
            );

            bells.Add(new GameModifierDataBuilder(this)
                .Create("BlessingCycler")
                .WithTitle("Sun Bell of Cycling")
                .WithDescription("At the end of each turn, draw until you have <3> cards in hand")
                .WithBellSprite(ImagePath("cycleBell.png").ToSprite())
                .WithDingerSprite(ImagePath("cycleDinger.png").ToSprite())
                .WithRingSfxEvent(Get<GameModifierData>("DoubleBlingsFromCombos").ringSfxEvent)
                .WithSystemsToAdd("Tutorial5_Classes.DrawToAmountModifierSystem")
                .FreeModify(
                (data) =>
                {
                    ScriptChangeHandSize handSize = ScriptableObject.CreateInstance<ScriptChangeHandSize>();
                    handSize.name = "Set Hand Size To 5";
                    handSize.set = true;
                    handSize.value = 3;
                    data.startScripts = new Script[] { handSize };
                })
            );

            tribes.Add(TribeCopy("Clunk", "Draw")
                .WithFlag(ImagePath("DrawFlag.png").ToSprite())
                .SubscribeToAfterAllBuildEvent(
                (data) =>
                {
                    data.leaders = CardList("needleLeader", "muncherLeader", "Leader1_heal_on_kill");
                    Inventory inventory = new Inventory();
                    inventory.deck.list = CardList("superMuncher", "SnowGlobe", "Sword", "Gearhammer", "Dart", "EnergyDart", "SunlightDrum", "Junkhead", "IceDice").ToList();
                    inventory.upgrades.Add(Get<CardUpgradeData>("CardUpgradeCritical"));
                    data.startingInventory = inventory;

                    RewardPool pool = Extensions.GetRewardPool("GeneralModifierPool");
                    pool.list = new List<DataFile> { Get<GameModifierData>("BlessingCycler"), Get<GameModifierData>("BlessingHealth"), Get<GameModifierData>("BlessingHand") };
                })
            );

            preLoaded = true;
        }

        public override void Load()
        {
            if (!preLoaded) { CreateModAssets(); }
            base.Load();

            GameMode gameMode = Get<GameMode>("GameModeNormal");
            gameMode.classes = gameMode.classes.Append(Get<ClassData>("Draw")).ToArray();

            Events.OnEntityCreated += FixImage;
        }

        public override void Unload()
        {
            base.Unload();

            GameMode gameMode = Get<GameMode>("GameModeNormal");
            gameMode.classes = RemoveNulls(gameMode.classes);

            Events.OnEntityCreated -= FixImage;
        }

        private void FixImage(Entity entity)
        {
            if (entity.display is Card card && !card.hasScriptableImage)
            {
                card.mainImage.gameObject.SetActive(true);
            }
        }

        internal T[] RemoveNulls<T>(T[] data) where T : DataFile
        {
            List<T> list = data.ToList();
            list.RemoveAll(x => x == null || x.ModAdded == this);
            return list.ToArray();
        }

        public override List<T> AddAssets<T, Y>()
        {
            var typeName = typeof(Y).Name;
            switch (typeName)
            {
                case nameof(CardData):
                    return cards.Cast<T>().ToList();
                case nameof(CardUpgradeData):
                    return cardUpgrades.Cast<T>().ToList();
                case nameof(StatusEffectData):
                    return statusEffects.Cast<T>().ToList();
                case nameof(GameModifierData):
                    return bells.Cast<T>().ToList();
                case nameof(ClassData):
                    return tribes.Cast<T>().ToList();
                default:
                    return null;
            }
        }
    }
   
    public class DrawToAmountModifierSystem : GameSystem
    {
        public int amount = 3;
        private void OnEnable()
        {
            Events.OnBattleTurnEnd += BattleTurnEnd;
        }

        private void OnDisable()
        {
            Events.OnBattleTurnEnd -= BattleTurnEnd;
        }

        private void BattleTurnEnd(int turn)
        {
            if (!Battle.instance.ended && References.Player.handContainer.Count < amount)
            {
                int amountToDraw = amount - References.Player.handContainer.Count;
                ActionQueue.Stack(new ActionDraw(References.Player, amountToDraw));
            }
        }
    }

    [HarmonyPatch(typeof(GameObjectExt), "AddComponentByName")]
    class PatchAddComponent
    {
        static Component Postfix(Component __result, GameObject gameObject, string componentName)
        {
            if (__result == null)
            {
                Type type = Type.GetType(componentName + ",Tutorial5-Classes");
                if (type != null)
                {
                    return gameObject.AddComponent(type);
                }
            }
            return __result;
        }
    }
}
