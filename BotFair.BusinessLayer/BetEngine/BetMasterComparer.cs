using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotFair.BusinessLayer.BetEngine
{
    class BetMasterComparer : IEqualityComparer<IBetMaster>
    {
        public bool Equals(IBetMaster x, IBetMaster y)
        {
            return (x.GetMasterId() == y.GetMasterId()) && (x.GetMarketId() == y.GetMarketId());
        }

        public int GetHashCode(IBetMaster obj)
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                // Suitable nullity checks etc, of course :)
                hash = hash * 23 + obj.GetMarketId().GetHashCode();
                hash = hash * 23 + obj.GetMasterId().GetHashCode();
       
                return hash;
            }
        }
    }
}
