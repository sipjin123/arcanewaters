using System;

[Serializable]
public class BattlerXMLContent
{
   // Id of the xml entry
   public int xmlId;

   // Data of the battler content
   public BattlerData battler;

   // Determines if the entry is enabled in the database
   public bool isEnabled;
}
