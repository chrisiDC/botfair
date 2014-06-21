using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BotFair.BusinessLayer
{
    public class ServiceException:Exception
    {

        public ServiceException(string info):base(info)
    {

    }
    }
}
