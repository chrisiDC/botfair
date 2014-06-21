using System;
using System.Collections.Generic;
using System.Linq;
using BotFair.BusinessLayer.BetEngine.Masters;
using BotFair.DataLayer;

namespace BotFair.BusinessLayer.BetEngine
{
    public class Config
    {
        static List<IBetMaster> masters = new List<IBetMaster>();
        private static bool reloadMasters = true;
        private static IEnumerable<BotDataSet.BetMastersRow> activeMasters;

        public static bool ReloadMasters { get { return reloadMasters; } set { reloadMasters = value; } }

        public static void RegisterMasters(int marketId)
        {
            if (reloadMasters)
            {
                activeMasters = Cache.Instance.GetBetMasters().Where(item => item.active == true).ToArray();

                reloadMasters = false;
            }

            foreach (BotDataSet.BetMastersRow row in activeMasters)
            {
                var t = Type.GetType(row._class);
                var createdMaster = (IBetMaster)Activator.CreateInstance(t, marketId, row.id);
                masters.Add(createdMaster);
                BetEngine.RegisterBetMaster(marketId, createdMaster);
            }
        }

        public static void UnRegisterMasters(int marketId)
        {
            var master = masters[marketId];

            BetEngine.UnRegisterBetMaster(marketId, master);

            masters.Remove(master);
        }
    }
}
