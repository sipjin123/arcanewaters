using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Globalization;

#if IS_SERVER_BUILD
using MySql.Data.MySqlClient;
#endif
[Serializable]
public class CraftingIngredients : RecipeItem
{
    #region Public Variables

    // The Type
    public enum Type
    {
        None = 0, Lizard_Scale = 1, Lizard_Claw = 2, Ore= 3, Lumber = 4, Flint = 5, 
    }

    // The CraftingIngredients Class
    public enum Class { Any = 0, Melee = 1, Ranged = 2, Magic = 3 }

    // The type
    public Type type;

    #endregion

    public CraftingIngredients()
    {
        this.type = Type.None;
    }

#if IS_SERVER_BUILD

    public CraftingIngredients(MySqlDataReader dataReader)
    {
        this.type = (CraftingIngredients.Type)DataUtil.getInt(dataReader, "itmType");
        this.id = DataUtil.getInt(dataReader, "itmId");
        this.category = (Item.Category)DataUtil.getInt(dataReader, "itmCategory");
        this.itemTypeId = DataUtil.getInt(dataReader, "itmType");
        this.data = DataUtil.getString(dataReader, "itmData");

        // Defaults
        this.color1 = (ColorType)DataUtil.getInt(dataReader, "itmColor1");
        this.color2 = (ColorType)DataUtil.getInt(dataReader, "itmColor2");

        foreach (string kvp in this.data.Split(','))
        {
            if (!kvp.Contains("="))
            {
                continue;
            }

            // Get the left and right side of the equal
            /*string key = kvp.Split('=')[0];
            string value = kvp.Split('=')[1];

            if ("color1".Equals(key)) {
               this.color1 = (ColorType) Convert.ToInt32(value);
            }*/
        }
    }

#endif
    public CraftingIngredients(int id, CraftingIngredients.Type recipeType, int primaryColorId, int secondaryColorId)
    {
        this.category = Category.CraftingIngredients;
        this.id = id;
        this.type = recipeType;
        this.itemTypeId = (int)recipeType;
        this.count = 1;
        this.color1 = (ColorType)primaryColorId;
        this.color2 = (ColorType)secondaryColorId;
        this.data = "";
    }

    public CraftingIngredients(int id, CraftingIngredients.Type recipeType, ColorType primaryColorId, ColorType secondaryColorId)
    {
        this.category = Category.CraftingIngredients;
        this.id = id;
        this.type = recipeType;
        this.itemTypeId = (int)recipeType;
        this.count = 1;
        this.color1 = primaryColorId;
        this.color2 = secondaryColorId;
        this.data = "";
    }

    public CraftingIngredients(int id, int itemTypeId, ColorType color1, ColorType color2, string data, int count = 1)
    {
        this.category = Category.CraftingIngredients;
        this.id = id;
        this.count = count;
        this.itemTypeId = itemTypeId;
        this.type = (Type)itemTypeId;
        this.color1 = color1;
        this.color2 = color2;
        this.data = data;
    }

    public override string getDescription()
    {
        switch (type)
        {
            case Type.Lizard_Claw:
                return "A well-made claw.";
            case Type.Lizard_Scale:
                return "A powerful Scale.";
            case Type.Ore:
                return "A shiny ore.";
            case Type.Flint:
                return "A Flint.";
            case Type.Lumber:
                return "A Lumber.";
            default:
                return "";
        }
    }

    public override string getTooltip()
    {
        Color color = Rarity.getColor(getRarity());
        string colorHex = ColorUtility.ToHtmlStringRGBA(color);

        return string.Format("<color={0}>{1}</color> ({2}, {3})\n\n{4}\n\nDamage = <color=red>{5}</color>",
           "#" + colorHex, getName(), color1, color2, getDescription(), getDamage());
    }

    public override string getName()
    {
        return getName(type);
    }


    public static string getName(CraftingIngredients.Type recipeType)
    {
        switch (recipeType)
        {
            case Type.Lizard_Claw:
                return "Lizard Claw";
            case Type.Lizard_Scale:
                return "Lizard Scale";
            case Type.Ore:
                return "Ore";
            case Type.Lumber:
                return "Lumber";
            case Type.Flint:
                return "Flint";
            default:
                return "";
        }
    }

    public int getDamage()
    {
        foreach (string kvp in this.data.Replace(" ", "").Split(','))
        {
            if (!kvp.Contains("="))
            {
                continue;
            }

            // Get the left and right side of the equal
            string key = kvp.Split('=')[0];
            string value = kvp.Split('=')[1];

            if ("damage".Equals(key))
            {
                return Convert.ToInt32(value);
            }
        }

        return 0;
    }

    public virtual float getDamage(Ability.Element element)
    {
        // Placeholder
        return 10f;
    }

    public static int getBaseDamage(Type craftingIngredientType)
    {
        switch (craftingIngredientType)
        {
            default:
                return 10;
        }
    }

    public static float getDamageModifier(Rarity.Type rarity)
    {
        float randomModifier = Util.getBellCurveFloat(1.0f, .1f, .90f, 1.10f);

        switch (rarity)
        {
            case Rarity.Type.Uncommon:
                return randomModifier * 1.2f;
            case Rarity.Type.Rare:
                return randomModifier * 1.5f;
            case Rarity.Type.Epic:
                return randomModifier * 1.5f;
            case Rarity.Type.Legendary:
                return randomModifier * 3f; ;
            default:
                return randomModifier;
        }
    }

    public static Class getClass(CraftingIngredients.Type type)
    {
        switch (type)
        {
            default:
                return Class.Melee;
        }
    }

    public static CraftingIngredients getEmpty()
    {
        return new CraftingIngredients(0, CraftingIngredients.Type.None, ColorType.None, ColorType.None);
    }

    public static CraftingIngredients generateRandom(int itemId, Type craftingIngredients)
    {
        // Decide what the rarity should be
        Rarity.Type rarity = Rarity.getRandom();

        // Alter the damage based on the rarity
        float baseDamage = getBaseDamage((Type)craftingIngredients);
        int damage = (int)(baseDamage * getDamageModifier(rarity));

        // Alter the price based on the rarity
        int price = (int)(getBaseSellPrice(Category.CraftingIngredients, (int)craftingIngredients) * Rarity.getItemShopPriceModifier(rarity));

        // Let's use nice numbers
        damage = Util.roundToPrettyNumber(damage);
        price = Util.roundToPrettyNumber(price);

        string data = string.Format("damage={0}, rarity={1}, price={2}", damage, (int)rarity, price);
        int stockCount = Rarity.getRandomItemStockCount(rarity);
        CraftingIngredients craftingIngredients2 = new CraftingIngredients(itemId, (int)craftingIngredients, ColorType.Black, ColorType.White, data, stockCount);

        return craftingIngredients2;
    }

    public override bool canBeTrashed()
    {
        switch (this.type)
        {

            default:
                return base.canBeTrashed();
        }
    }

    public override string getIconPath()
    {
        return "Icons/CraftingIngredients/" + this.type;
    }

    public override ColorKey getColorKey()
    {
        DebugCustom.Print("Ingredients: " + this.type + " : " + Global.player.gender);
        return new ColorKey(Global.player.gender, this.type);
    }

    #region Private Variables

    #endregion
}
