using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using Mirror;

namespace BackgroundTool
{
   public class BackgroundContentData 
   {
      // The name of the background content
      public string backgroundName = MasterToolScene.UNDEFINED;

      // The id of the xml entry
      public int xmlId = 0;

      // The list of the sprites in this background
      public List<SpriteTemplateData> spriteTemplateList = new List<SpriteTemplateData>();
   }
}