using PlanSum2Plan;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;

[assembly: ESAPIScript(IsWriteable = true)]

namespace VMS.TPS
{
    class Script
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext scriptcontext)
        {
            Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            if (scriptcontext.PlanSumsInScope.Count() == 0)
            {
                MessageBox.Show("No plan sums are active in the context.", "Error");
                return;
            }
            else
            {
                scriptcontext.Patient.BeginModifications();
                Window1 dialog = new Window1(scriptcontext);
                dialog.ShowDialog();
            }
        }
    }
}
