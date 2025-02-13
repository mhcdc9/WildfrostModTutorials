using Deadpan.Enums.Engine.Components.Modding;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Tutorial4_Patching
{
    public class Tutorial4 : WildfrostMod
    {
        public Tutorial4(string modDirectory) : base(modDirectory)
        {
        }

        public override string GUID => "mhcdc9.wildfrost.tutorial4";

        public override string[] Depends => new string[0];

        public override string Title => "Tutorial 4";

        public override string Description => "Learn how to patch the more convential mechanics out of the game";

        protected override void Load()
        {
            Events.OnBattlePreTurnStart += ResetCounter;
            base.Load();
        }

        protected override void Unload()
        {
            Events.OnBattlePreTurnStart -= ResetCounter;
            base.Unload();
        }

        public void ResetCounter(int _)
        {
            PatchNoTargetDance.count = 0;
            Traverse.Create(typeof(NoTargetTextSystem)).Field("instance").Field("shakeDurationRange").SetValue(new Vector2(0.3f, 0.4f));
            Traverse.Create(typeof(NoTargetTextSystem)).Field("instance").Field("shakeAmount").SetValue(new Vector3(1f, 0f, 0f));
            Debug.Log("[Tutorial] Cleared");
        }
    }

    [HarmonyPatch(typeof(CheckAchievements), "Start")]
    class PatchAchievements { static bool Prefix() => false; }

    [HarmonyPatch(typeof(NoTargetTextSystem), "_Run", new Type[]
    {
        typeof(Entity),
        typeof(NoTargetType),
        typeof(object[]),
    })]
    class PatchNoTargetDance
    {
        internal static int count = 0;

        static IEnumerator Etcetera(NoTargetTextSystem __instance, Entity entity, string s)
        {
            yield return Sequences.WaitForAnimationEnd(entity);
            TMP_Text textElement = Traverse.Create(__instance).Field("textElement").GetValue<TMP_Text>();
            textElement.text = s;
            Traverse.Create(__instance).Method("PopText", new Type[1] { typeof(Vector3) }, new object[1] { entity.transform.position }).GetValue();
            yield return new WaitForSeconds(0.4f);
        }

        static bool Prefix(ref IEnumerator __result, NoTargetTextSystem __instance, ref Vector2 ___shakeDurationRange, ref Vector2 ___shakeAmount, Entity entity)
        {
            count++;
            Debug.Log($"[Tutorial] Prefix: {count}");
            if (count == 3)
            {
                //Traverse.Create(__instance).Field("shakeDurationRange").SetValue(new Vector2(0.15f, 0.2f));
                //Traverse.Create(__instance).Field("shakeAmount").SetValue(new Vector3(0.75f, 0f, 0f));
                ___shakeDurationRange = new Vector2(0.15f, 0.2f);
                ___shakeAmount = new Vector3(0.75f, 0f, 0f);
            }
            if (count == 5)
            {
                __result = Etcetera(__instance, entity, "You get the idea");
            }
            if (count % 10 == 0)
            {
                __result = Etcetera(__instance, entity, $"{count}!");
            }
            if (count >= 5)
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Dead.Random), nameof(Dead.Random.Range), new Type[]
    {
        typeof(int),
        typeof(int)
    })]
    class PatchRandom
    {
        static void Postfix(int __result, int minInclusive, int maxInclusive)
        {
            Debug.Log($"[Tutorial] [{minInclusive}, {maxInclusive}] -> {__result}");
        }
    }

    [HarmonyPatch(typeof(StatusEffectData), "GetAmount")]
    class PatchAmountVariance
    {
        static int Postfix(int __result)
        {
            int r = Dead.Random.Range(-2, 2);
            return Math.Max(0, __result + r);
        }
    }

    [HarmonyPatch(typeof(Hit), "CalculateAttackEffectAmount")]
    class PatchAttackAmountVariance
    {
        static int Postfix(int __result)
        {
            int r = Dead.Random.Range(-2, 2);
            return Math.Max(0, __result + r);
        }
    }

    [HarmonyPatch(typeof(ChooseNewCardSequence),nameof(ChooseNewCardSequence.Run))]
    class PatchPickMe
    {
        static IEnumerator Postfix(IEnumerator __result, ChooseNewCardSequence __instance, GameObject ___cardGroupLayout, CardContainer ___cardContainer)
        {
            ///GameObject cardLayout = (GameObject) Traverse.Create(__instance).Field("cardGroupLayout").GetValue();
            ///CardContainer cardContainer = (CardContainer)Traverse.Create(__instance).Field("cardContainer").GetValue();

            while (__result.MoveNext()) //Move to the next item on the IEnumerator list
            {
                //Debug.Log("[Tutorial] Next");
                object obj = __result.Current;
                if (obj == null && ___cardGroupLayout.activeSelf && Dead.PettyRandom.Range(0f, 1f) < 0.1f) //In the range we care about
                {
                    Debug.Log("[Tutorial] Hit!");
                    ___cardContainer.RandomItem().curveAnimator.Ping(); //Ping!
                    yield return Sequences.Wait(0.4f);
                }
                else
                {
                    yield return __result.Current;
                }
            }
            Debug.Log("[Tutorial] Ending");
        }
    }
}
