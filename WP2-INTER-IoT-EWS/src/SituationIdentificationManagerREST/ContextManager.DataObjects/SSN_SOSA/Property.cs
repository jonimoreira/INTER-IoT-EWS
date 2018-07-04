using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.SSN
{
    public class Property: IEsperEvent
    {
        public object Identifier { get; private set; }

        public Property(object identifier)
        {
            this.Identifier = identifier;
        }

    }
}
