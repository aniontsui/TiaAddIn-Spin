﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Siemens.Engineering.AddIn.Menu;

namespace SpinAddIn
{
    public interface SpinAddinMenuRegistrationService
    {
        void Register(ContextMenuAddInRoot menuRoot);
    }
}
