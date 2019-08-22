using UnityEngine;

// Christopher Palacios

[System.Serializable]
public class BattleItemData : ScriptableObject
{
   #region Public Variables

   #endregion

   // Create setter methods to set the values of the object.
   protected void setName (string value) { _abilityName = value; }
   protected void setItemID (int value) { _itemID = value; }
   protected void setDescription (string value) { _abilityDescription = value; }
   protected void setItemElement (ItemElementType value) { _elementType = value; }
   protected void setItemDamage (int value) { _baseDamage = value; }
   protected void setHitAudioClip (AudioClip value) { _hitAudioClip = value; }
   protected void setHitParticle (ParticleSystem value) { _hitParticle = value; }
   protected void setBattleItemType (BattleItemType value) { _battleItemType = value; }
   protected void setItemIcon (Sprite value) { _itemIcon = value; }
   protected void setClassRequirement (Weapon.Class value) { _classRequirement = value; }

   // Create getter methods to get the methods of the object.
   public string getName { get { return _abilityName; } }
   public int getItemID { get { return _itemID; } }
   public string getDescription { get { return _abilityDescription; } }
   public int getBaseDamage { get { return _baseDamage; } }
   public ItemElementType getElementType { get { return _elementType; } }
   public AudioClip getHitAudioClip { get { return _hitAudioClip; } }
   public ParticleSystem getHitParticle { get { return _hitParticle; } }
   public BattleItemType getBattleItemType { get { return _battleItemType; } }
   public Sprite getItemIcon { get { return _itemIcon; } }
   public Weapon.Class getClassRequirement { get { return _classRequirement; } }

   /// <summary>
   /// Created a new instance with all the basic values required for a new BattleItem.
   /// </summary>
   /// <returns> Newly created battle item data, not to be used in game
   /// this data needs to be used to create an ability or a weapon. </returns>
   public static BattleItemData CreateInstance (int itemID, string name, string desc, int baseDmg, ItemElementType elemType,
       AudioClip hitClip, ParticleSystem hitParticle, BattleItemType battleItemType, Sprite itemIcon) {
      BattleItemData data = CreateInstance<BattleItemData>();

      data.setName(name);
      data.setDescription(desc);
      data.setItemID(itemID);

      data.setItemDamage(baseDmg);
      data.setItemElement(elemType);

      data.setHitAudioClip(hitClip);
      data.setHitParticle(hitParticle);

      data.setBattleItemType(battleItemType);
      data.setItemIcon(itemIcon);

      return data;
   }

   #region Private Variables

   // Basic items that all battle items will have.
   // In this case we will only use Abilities and in battle weapon data.
   // Do not make the variables public.

   // Most basic data that each BattleItem holds.
   [SerializeField] private int _itemID;
   [SerializeField] private string _abilityName;
   [SerializeField] private string _abilityDescription;
   [SerializeField] private Sprite _itemIcon;

   // Main combat data that this item holds.
   [SerializeField] private int _baseDamage;
   [SerializeField] private ItemElementType _elementType;
   [SerializeField] private AudioClip _hitAudioClip;
   [SerializeField] private ParticleSystem _hitParticle;
   
   // Player class required to be able to use this item.
   [SerializeField] private Weapon.Class _classRequirement;

   // Used mainly for the item builder, not in game.
   [SerializeField] private BattleItemType _battleItemType;

   #endregion
}

public enum BattleItemType
{
   UNDEFINED = 0,
   Ability = 1,
   Weapon = 2
}

public enum ItemElementType
{
   UNDEFINED = 0,
   Physical = 1,
   Fire = 2,
   Earth = 3,
   Air = 4,
   Water = 5,
   Heal = 6
}
