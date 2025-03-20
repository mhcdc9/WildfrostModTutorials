using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine;
using UnityEngine.Pool;

/* Change Checklist:
 * - Changed namespace?
 * - Changed TryGetSpecialFrame?
 * - [Optional] Remove Debug.Log in AddFrameToPool()?
 */

//Please change the name of the namespace to reflect your mod
namespace CustomCardFrames
{
    [HarmonyPatch]
    internal class CustomCardFrameSystem
    {
        #region CHANGEABLE

        static string[] BlockNames = new string[] { "Blue", "Blunky", "Leader1_heal_on_kill" };
        static string[] TeethNames = new string[] { "Tusk", "Kokonut", "Tigris" };
        //This method is the criteria for which cards get custom frames.
        internal static bool TryGetSpecialFrame(CardData data, out string frame)
        {
            if (BlockNames.Contains(data.name))
            {
                frame = "mhcdc9.Block";
                return true;
            }
            else if (TeethNames.Contains(data.name))
            {
                frame = "mhcdc9.Teeth";
                return true;
            }
            frame = null;
            return false;
        }
        #endregion

        //Call this method in the Load() of your main mod class.
        //frameName: The name you will refer to your "CardType".
        //basePrefabName: The name of the CradType most similar to it (it will be copied)
        //dictionary: A dicitonary with the keys being Card elements and the replacement Sprites.
        // - There are 4 keys that will be read: Frame, NameTag, Mask, FrameOutline
        // - Not including a key will make the element default to the basePrefab
        //maxLevel: If the base CardType has multiple frame types (e.g. base, chiseled, golden), this number should match the highest index among them.
        // - Item, Clunker, Friendly: 2 (default)
        // - Leader, Enemy, Miniboss: 0 
        public static void AddCustomFrame(string frameName, string basePrefabName, Dictionary<string, Sprite> dictionary, int maxLevel = 2)
        {
                AddCustomFrame<CardCustomFrameSetter>(frameName, basePrefabName, dictionary, maxLevel);
        }

        //More general version
        public static void AddCustomFrame<T>(string frameName, string basePrefabName, Dictionary<string, Sprite> dictionary, int maxLevel = 2) where T: CardCustomFrameSetter
        {
            References.instance.StartCoroutine(AddFrameToPool<T>(frameName, basePrefabName, dictionary, maxLevel));
        }

        public static IEnumerator AddFrameToPool<T>(string frameName, string basePrefab, Dictionary<string, Sprite> dictionary, int maxLevel = 2) where T: CardCustomFrameSetter
        {
            Transform t = CardManager.instance.transform;
            CardType friendly = CustomCardFrames.instance.TryGet<CardType>(basePrefab); //Replace CustomCardFrames with your main mod class.
            AsyncOperationHandle<GameObject> handle = Addressables.LoadAssetAsync<GameObject>(friendly.prefabRef);
            yield return new WaitUntil(() => handle.IsDone);
            GameObject prefab = handle.Result.InstantiateKeepName();
            prefab.name = "(Card) " + frameName;
            CardFrameSetter oldSetter = prefab.GetComponentInChildren<CardFrameSetter>();
            T newSetter = prefab.AddComponent<T>();
            newSetter.Init(dictionary, frameName);


            GameObject.DontDestroyOnLoad(prefab);
            ObjectPool<Card> pool = new ObjectPool<Card>(() =>
            {
                GameObject obj = GameObject.Instantiate(prefab, CardManager.startPos, Quaternion.identity, t);
                obj.GetComponent<T>().Init(dictionary, frameName);
                obj.GetComponent<T>().CreateCallback();
                return obj.GetComponent<Card>();

            },
                delegate (Card card)
                {
                    card.OnGetFromPool();
                    card.entity.OnGetFromPool();
                    card.transform.position = CardManager.startPos;
                    card.transform.localRotation = Quaternion.identity;
                    card.transform.localScale = Vector3.one;
                    card.GetComponent<T>().GetCallback();
                    card.gameObject.SetActive(value: true);
                }, delegate (Card card)
                {
                    card.transform.SetParent(t);
                    card.OnReturnToPool();
                    card.entity.OnReturnToPool();
                    Events.InvokeCardPooled(card);
                    card.gameObject.SetActive(value: false);
                    card.GetComponent<T>().PoolCallback();
                }, delegate (Card card)
                {
                    card.GetComponent<T>().DestroyCallback();
                    UnityEngine.Object.Destroy(card.gameObject);
                }, collectionCheck: false, 10, 20);
            for (int i = 0; i <= maxLevel; i++)
            {
                CardManager.cardPools[frameName + i.ToString()] = pool;
            }

            prefab.SetActive(false);

            Debug.Log($"Custom Card Frame: {frameName}");
        }

        

        #region PATCHES

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.Get))]
        static bool PatchGet(CardData data, CardController controller, Character owner, bool inPlay, bool isPlayerCard, ref Card __result)
        {
            int num = (isPlayerCard ? CardFramesSystem.GetFrameLevel(data.name) : 0);
            if (data != null)
            {
                if (TryGetSpecialFrame(data, out string frame))
                {
                    Card card = CardManager.cardPools[$"{frame}{num}"].Get();
                    card.frameLevel = num;
                    card.entity.data = data;
                    card.entity.inPlay = inPlay;
                    card.hover.controller = controller;
                    card.entity.owner = owner;
                    card.frameSetter.GetComponent<CardCustomFrameSetter>().Load(num);
                    Events.InvokeEntityCreated(card.entity);
                    __result = card;
                    return false;
                }
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(CardManager), nameof(CardManager.ReturnToPool), new Type[]
        {
            typeof(Entity),
            typeof(Card)
        })]
        static bool PatchReturnToPool(Entity entity, Card card, ref bool __result)
        {
            if (GameManager.End || entity.inCardPool)
            {
                return true;
            }

            CardCustomFrameSetter setter = card.GetComponent<CardCustomFrameSetter>();
            if (setter == null)
            {
                return true;
            }

            if (!entity.returnToPool)
            {
                UnityEngine.Object.Destroy(entity.gameObject);
            }

            CardManager.cardPools[$"{setter.pool}{card.frameLevel}"].Release(card);
            __result = true;
            return false;
        }

        #endregion
    }
}
