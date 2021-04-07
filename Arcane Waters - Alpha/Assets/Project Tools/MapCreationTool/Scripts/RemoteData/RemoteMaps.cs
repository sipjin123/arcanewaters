using System.Collections.Generic;
using System.Linq;
using MapCreationTool.Serialization;

namespace MapCreationTool
{
   public class RemoteMaps : RemoteData<Map>
   {
      public Dictionary<int, Map> maps { get; private set; }

      public SelectOption[] formMapsSelectOptions () {
         return
            Enumerable.Repeat(new SelectOption("-1", ""), 1)
            .Union(maps.Values.Select(m => new SelectOption(m.id.ToString(), m.name)))
            .ToArray();
      }

      protected override List<Map> fetchData () {
         // TODO: make this run using async operator
         return DB_Main.exec(DB_Main.getMaps);
      }

      protected override void setData (List<Map> data) {
         maps = new Dictionary<int, Map>();

         foreach (Map map in data) {
            if (!maps.ContainsKey(map.id)) {
               maps.Add(map.id, map);
            }
         }
      }
   }
}