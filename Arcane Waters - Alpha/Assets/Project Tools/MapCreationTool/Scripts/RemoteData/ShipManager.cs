using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace MapCreationTool
{
   public class ShipManager : MonoBehaviour
   {
      public static event System.Action OnLoaded;

      public static ShipManager instance { get; private set; }

      private ShipData[] ships = new ShipData[0];

      public Dictionary<int, ShipData> idToShipData { get; private set; }

      public bool loaded { get; private set; }

      private void Awake () {
         instance = this;
      }

      private IEnumerator Start () {
         yield return new WaitUntil(() => ImageManager.self != null);

         loadAllShips();
      }

      public Texture2D getShipTexture (int id) {
         if (!idToShipData.ContainsKey(id)) {
            Debug.LogWarning($"Unrecognized ship ID {id}.");
            return null;
         }

         return ImageManager.getTexture(idToShipData[id].spritePath);
      }

      public Texture2D getFirstShipTexture () {
         return ImageManager.getTexture(ships[0].spritePath);
      }

      public SelectOption[] formSelectionOptions () {
         return ships.Select(ship => new SelectOption(
            ((int) ship.shipType).ToString(),
            ship.shipName)
         ).ToArray();
      }

      public SelectOption[] formGuildSelectionOptions () {
         List<SelectOption> optionList = new List<SelectOption>();
         foreach (GuildType guildType in Enum.GetValues(typeof(GuildType))) {
            SelectOption newOption = new SelectOption(((int) guildType).ToString(), guildType.ToString());
            optionList.Add(newOption);
         }
         return optionList.ToArray();
      }

      public int shipCount
      {
         get { return ships.Length; }
      }

      private void loadAllShips () {
         UnityThreadHelper.BackgroundDispatcher.Dispatch(() => {
            List<XMLPair> shipData = DB_Main.getShipXML();

            UnityThreadHelper.UnityDispatcher.Dispatch(() => {
               setData(shipData);
            });
         });
      }

      private void setData (List<XMLPair> shipDatas) {
         try {
            idToShipData = new Dictionary<int, ShipData>();

            foreach (XMLPair data in shipDatas) {
               ShipData shipData = Util.xmlLoad<ShipData>(new TextAsset(data.rawXmlData));
               int shipTypeID = (int) shipData.shipType;

               if (!idToShipData.ContainsKey(shipTypeID)) {
                  idToShipData.Add(shipTypeID, shipData);
               }
            }

            ships = idToShipData.OrderBy(n => n.Key).Select(n => n.Value).ToArray();
         } catch (Exception ex) {
            Utilities.warning("Failed to load ship manager. Exception:\n" + ex);
            UI.messagePanel.displayError("Failed to load ship manager. Exception:\n" + ex);
         }

         loaded = true;
         OnLoaded?.Invoke();
      }
   }
}