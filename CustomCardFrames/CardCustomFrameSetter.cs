using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/* Change Checklist:
 * - Changed namespace?
 * - [Optional] Extend the class for more functionality?
 */

/*
 * Card Elements
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

//Please change the name of the namespace to reflect your mod
namespace CustomCardFrames
{
    //If your custom card frame needs differ drastically, consider extending this class for each frame.
    //Call AddCustomFrame<T> instead of AddCustomFrame
    public class CardCustomFrameSetter : MonoBehaviour
    {
        public CardFrameSetter cardFrameSetter;
        public Dictionary<string, Sprite> sprites;
        public string pool;

        public virtual void Init(Dictionary<string, Sprite> dictionary, string pool)
        {
            cardFrameSetter = gameObject.GetComponentInChildren<CardFrameSetter>();
            sprites = dictionary;
            this.pool = pool;
        }

        //Rather than outright replacing the CardFrameSetter, it's easier just to "extend" it
        public virtual void Load(int num)
        {
            if (cardFrameSetter.loaded) { return; }

            cardFrameSetter.Load(num);

            //There might be a spriterenderer or ImageSprite you want to use (unlikely). Then this for loop needs to be changed.
            foreach(Image image in GetComponentsInChildren<Image>(true))
            {
                if (sprites.ContainsKey(image.name))
                {
                    image.sprite = GetSprite(image.name, num);
                }
            }

            /* [Depreciated] Not all card types use SpriteSetters
            foreach (var spriteLoader in cardFrameSetter.spriteSetters)
            {
                if (!sprites.ContainsKey(spriteLoader.name))
                    continue;

                switch (spriteLoader.type)
                {
                    case AddressableTieredSpriteLoader.Type.SpriteRenderer:
                        spriteLoader.spriteRenderer.sprite = GetSprite(spriteLoader.name, num);
                        break;
                    case AddressableTieredSpriteLoader.Type.Image:
                        spriteLoader.image.sprite = GetSprite(spriteLoader.name, num);
                        break;
                    case AddressableTieredSpriteLoader.Type.ImageSprite:
                        spriteLoader.imageSprite.SetSprite(GetSprite(spriteLoader.name, num));
                        break;
                }
            }

            //Description isn't dynamically set, so we can set it during the card creation 
            if (sprites.ContainsKey("Description"))
            {
                AddressableTieredSpriteLoader nameTag = cardFrameSetter.spriteSetters.FirstOrDefault(s => s.name == "NameTag");
                Image image = nameTag?.image?.transform?.parent?.GetComponent<Image>();
                if (image != null)
                {
                    image.sprite = sprites["Description"];
                }
            }
            */
        }

        public virtual Sprite GetSprite(string name, int num) => sprites[name];


        //If you extend this class, these methods might be useful
        public virtual void CreateCallback() { } //Occurs when the card is first created; might be useless.
        public virtual void GetCallback() { } //Occurs when the card is being reused by another card
        public virtual void PoolCallback() { } //Occurs when the card is no longer needs to be on-screen
        public virtual void DestroyCallback() { } //Occurs when the card is destroyed (OnDestroy also works)

    }
}
