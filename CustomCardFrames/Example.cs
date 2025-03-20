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

namespace CustomCardFrames
{
    public class CustomCardFrames : WildfrostMod
    {

        public override string GUID => "mhcdc9.wildfrost.frames";

        public override string[] Depends => new string[0];

        public override string Title => "Custom Card Frames";

        public override string Description => "Some example code for making custom card frames";

        public static CustomCardFrames instance;

        public T TryGet<T>(string name) where T : DataFile
        {
            T data;
            if (typeof(StatusEffectData).IsAssignableFrom(typeof(T)))
                data = base.Get<StatusEffectData>(name) as T;
            else if (typeof(KeywordData).IsAssignableFrom(typeof(T)))
                data = base.Get<KeywordData>(name.ToLower()) as T;
            else
                data = base.Get<T>(name);

            if (data == null)
                throw new Exception($"TryGet Error: Could not find a [{typeof(T).Name}] with the name [{name}] or [{Extensions.PrefixGUID(name, this)}]");

            return data;
        }

        public CustomCardFrames(string modDirectory) : base(modDirectory) { instance = this; }

        public static bool preLoaded; 

        public void CreateModAssets()
        {
            //Presumably you have something here
            preLoaded = true;
        }

        public static string[] spriteStrings = { "Frame", "NameTag", "Mask", "FrameOutline", "DescriptionBox" };
        /*
        * Full List of Card Elements
        * - Frame
        * - FrameOutline
        * - NameTag
        * - DescriptionBox
        * - Mask
        * - Back
        * - Fill
        * - Background
        * - Image
        * - Interaction                    | Holds the Splatter Surface, already affected by Mask
        * - HealthIcon(Clone)              | If it has any (might be reset during battle?)
        * - AttackIcon(Clone)              | If it has any (might be reset during battle?)
        */

        public void CreateCustomCardFrames()
        {
            //Somewhere else, possibleSprites is defined like this:
            //public string[] possibleSprites = new string[] { "Frame", "NameTag", "Mask", "FrameOutline", "DescriptionBox" };
            // The full list of changeables can be found in CardCustomFrameSetter as well  
            Dictionary<string, Sprite> dictionary = new Dictionary<string, Sprite>();
            foreach (var names in spriteStrings)
            {
                //Using a very specific naming convention here. You don't have to, but the code will look slightly longer.
                dictionary[names] = ImagePath($"Companion {names} Block.png").ToSprite();
            }
            CustomCardFrameSystem.AddCustomFrame("mhcdc9.Block", "Friendly", dictionary);



            //Second card frame changes only some parts, and makes them 2-tall (my sprites not configured corectly for this)
            Dictionary<string, Sprite> dictionaryTeeth = new Dictionary<string, Sprite>();
            dictionaryTeeth["NameTag"] = ImagePath($"Companion NameTag Block.png").ToSprite();
            dictionaryTeeth["DescriptionBox"] = ImagePath($"Companion DescriptionBox Block.png").ToSprite();
            CustomCardFrameSystem.AddCustomFrame("mhcdc9.Teeth", "Boss", dictionary);
        }

        public override void Load()
        {
            if (!preLoaded)
            {
                CreateModAssets();
                CreateCustomCardFrames();
            }
            base.Load();
        }

        public override void Unload()
        {
            base.Unload();
        }

        
    }
}
