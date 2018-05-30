﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ContextManagerREST.SituationIdentification.CEP
{
    /// <summary>
    /// Helper Interface to work around limitations of NEsper.
    /// NEsper seems to be unable to recognize classes in libraries. With the aid of implemented interface, we can simply search automatically for those classes and make them known to NEsper.
    /// </summary>
    public interface IEsperEvent
    {
    }
}
