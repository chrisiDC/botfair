using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using BotFair.DataLayer;
using BotFair.DataLayer.BotDataSetTableAdapters;
using Tools.Aspects;

namespace BotFair.BusinessLayer
{
    public class Log : EntityBase
    {
      
        public void LogEvent(int id, string type, string msg)
        {
            Cache.Instance.Log(id, type, msg);
        }

     
        public void FlushEvents()
        {
            Cache.Instance.CommitLog();
            Cache.Instance.ClearLog();
        }

        protected override void Init()
        {

        }

    
        public void Clear()
        {
            Cache.Instance.GetLog().Rows.Clear();
        }
    }
}
