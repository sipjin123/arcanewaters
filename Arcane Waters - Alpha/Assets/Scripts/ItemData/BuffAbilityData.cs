using UnityEngine;

public class BuffAbilityData : BasicAbilityData
{
   #region Public Variables

   #endregion

   // Builder scriptable object instance builder
   public static BuffAbilityData CreateInstance (BuffAbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      BuffAbilityData data = CreateInstance<BuffAbilityData>();

      // Sets base battle item data
      data.setBaseBattleItemData(datacopy);

      // Sets base ability data
      data.setBaseAbilityData(datacopy);

      // Sets attack ability item properties
      data.setBuffDuration(datacopy.getBuffDuration());
      data.setBuffType(datacopy.getBuffType());
      data.setBuffActionType(datacopy.getBuffActionType());
      data.setBuffIcon(datacopy.getBuffIcon());
      data.setBuffValue(datacopy.getBuffValue());

      return data;
   }

   /// <summary>
   /// Used for creating a BuffAbilityData, only on item creation window
   /// </summary>
   public static BuffAbilityData CreateInstance (BasicAbilityData basicAbilityData, float buffDuration, BuffType buffType,
      BuffActionType buffActionType, Sprite buffIcon, int buffValue) {
      BuffAbilityData data = CreateInstance<BuffAbilityData>();

      // Sets base ability data
      data.setBaseBattleItemData(basicAbilityData);
      data.setBaseAbilityData(basicAbilityData);

      // Sets attack ability item properties
      data.setBuffDuration(buffDuration);
      data.setBuffType(buffType);
      data.setBuffActionType(buffActionType);
      data.setBuffIcon(buffIcon);
      data.setBuffValue(buffValue);

      return data;
   }

   #region Custom Helper Methods

   public float getTotalAnimLength (BattlerBehaviour attacker, BattlerBehaviour target) {
      return 1;
   }

   #endregion

   // Setters
   public void setBuffDuration (float value) { _duration = value; }
   public void setBuffType (BuffType value) { _buffType = value; }
   public void setBuffActionType (BuffActionType value) { _buffActionType = value; }
   public void setBuffIcon (Sprite value) { _icon = value; }
   public void setBuffValue (int value) { _value = value; }

   // Getters
   public float getBuffDuration () { return _duration; }
   public BuffType getBuffType () { return _buffType; }
   public BuffActionType getBuffActionType () { return _buffActionType; }
   public Sprite getBuffIcon () { return _icon; }
   public int getBuffValue () { return _value; }

   #region Private Variables

   [SerializeField] private BuffActionType _buffActionType;
   [SerializeField] private BuffType _buffType;
   [SerializeField] private Sprite _icon;
   [SerializeField] private float _duration;

   // This will be the value in which we will increase or reduce the value, depending on the buff action
   // (For example, if the action is defense, and the value is 10, and it is a buff, then we will increase defense by 10, percentage or raw value) 
   [SerializeField] private int _value;

   #endregion
}

public enum BuffType
{
   UNDEFINED = 0,
   Buff = 1,
   Debuff = 2
}

public enum BuffActionType
{
   UNDEFINED = 0,
   Defense = 1,      // If debuff, this will reduce defense instead of increasing it.
   Haste = 2,        // If debuff, this will increase the cooldown instead of reducing it
   Regeneration = 3  // If debuff, this will decrease health overtime, instead of increasing it
}
