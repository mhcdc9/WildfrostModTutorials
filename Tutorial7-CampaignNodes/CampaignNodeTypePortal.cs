using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Tutorial7_CampaignNodes
{
    internal class CampaignNodeTypePortal : CampaignNodeType
    {
        

        [Serializable]
        public enum Results
        {
            Gold = 0,
            Injury = 1,
            Charm = 2
        }

        int minCharmEvent = 4;

        //Called during campaign population, determines the data to give to node.
        public override IEnumerator SetUp(CampaignNode node)
        {
            node.data = new Dictionary<string, object>();

            List<Results> results = new List<Results>
            { 
                Results.Gold,
                Results.Gold,
                Results.Injury,
                Results.Injury,
                Results.Injury
            };
            results = results.InRandomOrder().ToList(); //Randomize the results
            results.Insert(0, Results.Gold);
            results.Insert(Dead.Random.Range(minCharmEvent, results.Count), Results.Charm); //Add a charm somewhere at the end

            CharacterRewards component = References.Player.GetComponent<CharacterRewards>();
            CardUpgradeData cardUpgradeData = component.Pull<CardUpgradeData>(node, "Charms");

            node.data.Add("clicks", -1);
            node.data.Add("results", new SaveCollection<Results>(results));
            node.data.Add("charm", cardUpgradeData.name);

            return base.SetUp(node); //Placeholder
        }

        //Called when the map node is clicked on.
        public override IEnumerator Run(CampaignNode node)
        {
            SaveCollection<Results> results = (SaveCollection<Results>)node.data["results"];
            int click = (int)node.data["clicks"];
            click++;
            node.data["clicks"] = click;

            if (click < results.Count)
            {
                switch (results[click])
                {
                    case Results.Gold:
                        yield return ObtainGold(node);
                        break;
                    case Results.Injury:
                        yield return AddInjury(node);
                        break;
                    case Results.Charm:
                        yield return ObtainCharm(node);
                        break;
                }
            }

            if ((int)node.data["clicks"] >= results.Count-1)
            {
                node.SetCleared();
            }
            References.Map.Continue();
        }

        public IEnumerator ObtainCharm(CampaignNode node)
        {
            CardUpgradeData upgrade = Tutorial7.instance.TryGet<CardUpgradeData>((string)node.data["charm"]);
            References.PlayerData.inventory.upgrades.Add(upgrade.Clone());
            Campaign.PromptSave();
            yield return TextPopUp(node, CharmString, upgrade.title);
        }

        public IEnumerator AddInjury(CampaignNode node)
        {
            bool flag = false;
            string s = "";
            foreach(CardData data in References.PlayerData.inventory.deck.InRandomOrder())
            {
                if (data.cardType.name == "Friendly" && data.injuries.Count == 0)
                {
                    data.injuries.Add(Tutorial7.instance.SStack("Injury",1));
                    s = data.title;
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                node.data["clicks"] = 999;
            }
            Campaign.PromptSave();
            yield return TextPopUp(node, flag ? InjuryString : AllInjuryString, s);
        }

        public static LocalizedString GoldString;
        public static LocalizedString InjuryString;
        public static LocalizedString AllInjuryString;
        public static LocalizedString CharmString;

        public IEnumerator TextPopUp(CampaignNode node, LocalizedString key, string textInsert = "")
        {
            string s = key.GetLocalizedString();
            s = s.Format(textInsert);

            NoTargetTextSystem system = UnityEngine.Object.FindObjectOfType<NoTargetTextSystem>();
            if (system == null)
            {
                yield break;
            }

            TMP_Text textElement = system.textElement;
            textElement.text = s;
            system.PopText(References.Map.FindNode(Campaign.FindCharacterNode(References.Player)).transform.position);
            yield return new WaitForSeconds(0.5f);
        }

        public IEnumerator ObtainGold(CampaignNode node)
        {
            Character player = References.Player;
            Vector3 position = Vector3.zero;
            MapNew mapNew = UnityEngine.Object.FindObjectOfType<MapNew>();
            if ((object)mapNew != null)
            {
                MapNode mapNode = mapNew.FindNode(node);
                if ((object)mapNode != null)
                {
                    position = mapNode.transform.position;
                }
            }

            if ((bool)player && (bool)player.data?.inventory)
            {
                Events.InvokeDropGold(25, "GoldCave", player, position);
            }
            Campaign.PromptSave();
            yield return TextPopUp(node, GoldString);
            yield return new WaitForSeconds(0.5f);
        }

        //Called on the continue run screen. Returning true makes the run unable to continue.
        public override bool HasMissingData(CampaignNode node)
        {
            if (!node.data.ContainsKey("clicks") || !node.data.ContainsKey("results"))
            {
                return true;
            }

            if (node.data.TryGetValue("charm", out object value) && value is string upgradeName)
            {
                return (Tutorial7.instance.Get<CardUpgradeData>(upgradeName) == null);
            }
                
            return true;
        }
    }
}
