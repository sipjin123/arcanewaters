using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;
using System;

namespace BackgroundTool
{
   [Serializable]
   public class BackgroundContentData 
   {
      // The name of the background content
      public string backgroundName = MasterToolScene.UNDEFINED;

      // The id of the xml entry
      public int xmlId = 0;

      // The id of the creator of this data
      public int ownerId = 0;

      // The list of the sprites in this background
      public List<SpriteTemplateData> spriteTemplateList = new List<SpriteTemplateData>();

      // Determines the biome type of this bg data
      public Biome.Type biomeType = Biome.Type.Forest;

      // The weather type of the biome
      public WeatherEffectType weatherType;
   }
}