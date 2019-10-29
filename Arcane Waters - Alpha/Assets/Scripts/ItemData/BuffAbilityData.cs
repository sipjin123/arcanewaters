using UnityEngine;

public class BuffAbilityData : BasicAbilityData
{
   #region Public Variables

   // Determines the action type
   public BuffActionType buffActionType;

   // Determines the buff type
   public BuffType buffType;

   // The image path of the icon
   public string iconPath;

   // The duration of the skill
   public float duration;

   // This will be the value in which we will increase or reduce the value, depending on the buff action
   // (For example, if the action is defense, and the value is 10, and it is a buff, then we will increase defense by 10, percentage or raw value) 
   public int value;

   #endregion

   public BuffAbilityData () { }

   // Builder scriptable object instance builder
   public static BuffAbilityData CreateInstance (BuffAbilityData datacopy) {
      // If a new value needs to be added to the abilitydata class, it needs to be included in here!
      BuffAbilityData data = new BuffAbilityData();

      // Sets base battle item data
      data.setBaseBattleItemData(datacopy);

      // Sets base ability data
      data.setBaseAbilityData(datacopy);

      // Sets attack ability item properties
      data.duration = datacopy.duration;
      data.buffType = datacopy.buffType;
      data.buffActionType = datacopy.buffActionType;
      data.iconPath = datacopy.iconPath;
      data.value = datacopy.value;

      return data;
   }

   /// <summary>
   /// Used for creating a BuffAbilityData, only on item creation window
   /// </summary>
   public static BuffAbilityData CreateInstance (BasicAbilityData basicAbilityData, float buffDuration, BuffType buffType,
      BuffActionType buffActionType, string buffIcon, int buffValue) {
      BuffAbilityData data = new BuffAbilityData();

      // Sets base ability data
      data.setBaseBattleItemData(basicAbilityData);
      data.setBaseAbilityData(basicAbilityData);

      // Sets attack ability item properties
      data.duration = buffDuration;
      data.buffType = buffType;
      data.buffActionType = buffActionType;
      data.iconPath = buffIcon;
      data.value = buffValue;

      return data;
   }

   #region Custom Helper Methods

   public float getTotalAnimLength (BattlerBehaviour attacker, BattlerBehaviour target) {
      return 1;
   }

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
