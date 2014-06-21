
using BotFair.DataLayer;
using BotFair.DataLayer.BetFairExchange;
using BotFair.DataLayer.BotFairGlobal;

namespace BotFair.BusinessLayer
{
    public abstract class EntityBase
    {

        protected Cache cache;

        public EntityBase()
        {
            this.cache = Cache.Instance;
            Init();
        }

        protected abstract void Init();
    }
}