using UnityEngine;

[System.Serializable]
public class BattleItemData : ScriptableObject
{
   #region Public Variables

   #endregion

   // Create setter methods to set the values of the object
   protected void setName (string value) { _itemName = value; }
   protected void setItemID (int value) { _itemID = value; }
   protected void setDescription (string value) { _itemDescription = value; }
   protected void setLevelRequirement (int value) { _levelRequirement = value; }
   protected void setItemElement (Element value) { _elementType = value; }
   protected void setHitAudioClip (AudioClip value) { _hitAudioClip = value; }
   protected void setHitEffect (Sprite[] value) { _hitSprites = value; }
   protected void setBattleItemType (BattleItemType value) { _battleItemType = value; }
   protected void setItemIcon (Sprite value) { _itemIcon = value; }
   protected void setClassRequirement (Weapon.Class value) { _classRequirement = value; }

   // Create getter methods to get the methods of the object
   public string getName () { return _itemName; }
   public int getItemID () { return _itemID; }
   public string getDescription () { return _itemDescription; }
   public int getLevelRequirement () { return _levelRequirement; }

   public Element getElementType () { return _elementType; }

   public AudioClip getHitAudioClip () { return _hitAudioClip; }
   public Sprite[] getHitEffect () { return _hitSprites; }

   public BattleItemType getBattleItemType () { return _battleItemType; }
   public Sprite getItemIcon () { return _itemIcon; }
   public Weapon.Class getClassRequirement () { return _classRequirement; }

   /// <summary>
   /// Created a new instance with all the basic values required for a new BattleItem
   /// </summary>
   /// <returns> Newly created battle item data, not to be used in game
   /// this data needs to be used to create an ability or a weapon. </returns>
   public static BattleItemData CreateInstance (int itemID, string name, string desc, Element elemType,
       AudioClip hitClip, Sprite[] hitSprites, BattleItemType battleItemType, Sprite itemIcon, int levelRequirement) {
      BattleItemData data = CreateInstance<BattleItemData>();

      data.setName(name);
      data.setDescription(desc);
      data.setItemID(itemID);
      data.setLevelRequirement(levelRequirement);

      data.setItemElement(elemType);

      data.setHitAudioClip(hitClip);
      data.setHitEffect(hitSprites);

      data.setBattleItemType(battleItemType);
      data.setItemIcon(itemIcon);

      return data;
   }

   /// <summary>
   /// Gets all base battle item data and sets it to this object.
   /// </summary>
   /// <param name="battleItemData"></param>
   protected void setBaseBattleItemData (BattleItemData battleItemData) {
      // Basic battle item data
      setItemID(battleItemData.getItemID());
      setName(battleItemData.getName());
      setDescription(battleItemData.getDescription());
      setItemIcon(battleItemData.getItemIcon());
      setLevelRequirement(battleItemData.getLevelRequirement());
      setHitEffect(battleItemData.getHitEffect());
      setItemElement(battleItemData.getElementType());
      setHitAudioClip(battleItemData.getHitAudioClip());
      setClassRequirement(battleItemData.getClassRequirement());
      setBattleItemType(battleItemData.getBattleItemType());
   }

   #region Private Variables

   // Basic items that all battle items will have
   // Do not make the variables public

   // Most basic data that each BattleItem holds
   [SerializeField] private int _itemID;
   [SerializeField] private string _itemName;
   [SerializeField] private string _itemDescription;
   [SerializeField] private Sprite _itemIcon;
   [SerializeField] private int _levelRequirement;

   // Effect that will be executed when the ability hits the target, it can be a buff/debuff too
   [SerializeField] private Sprite[] _hitSprites;

   // Main combat data that this item holds
   [SerializeField] private Element _elementType;
   [SerializeField] private AudioClip _hitAudioClip;

   // Player class required to be able to use this item
   [SerializeField] private Weapon.Class _classRequirement;

   // Used mainly for the item builder, not in game
   [SerializeField] private BattleItemType _battleItemType;

   #endregion
}

public enum BattleItemType
{
   UNDEFINED = 0,
   Ability = 1,
   Weapon = 2
}
