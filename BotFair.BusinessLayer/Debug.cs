using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace BotFair.BusinessLayer
{
    public class Debug:EntityBase
    {
  
        public DataSet GetCache()
        {
            return cache.GetAll();
        }

        protected override void Init()
        {
         
        }
    }
}
