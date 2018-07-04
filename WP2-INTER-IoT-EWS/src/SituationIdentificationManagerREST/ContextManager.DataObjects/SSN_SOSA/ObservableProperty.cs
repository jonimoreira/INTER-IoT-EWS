using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace INTERIoTEWS.Context.DataObjects.SOSA
{
    public class ObservableProperty : SSN.Property
    {
        public ObservableProperty(object identifier, string label) : base(identifier)
        {
            this.Label = label;
        }

        public string Label { get; private set; }

    }
}
