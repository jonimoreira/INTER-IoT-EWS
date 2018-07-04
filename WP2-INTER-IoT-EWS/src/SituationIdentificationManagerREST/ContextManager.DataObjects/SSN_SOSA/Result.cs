using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.SOSA
{
    public class Result : IEsperEvent
    {
        public string hasUnit { get; set; }
        public double hasValue { get; set; }
    }
}
