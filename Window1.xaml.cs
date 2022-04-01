using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace PlanSum2Plan
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Window1 : Window
    {
        public ScriptContext scriptcontext;

        public Window1(ScriptContext scriptcontext)
        {
            InitializeComponent();
            this.scriptcontext = scriptcontext;
            AddComboList();
        }

        private void AddComboList()
        {
            List<string> plansums = new List<string>() { };
            foreach (var ps in this.scriptcontext.PlanSumsInScope)
            {
                this.comboBox1.Items.Add(ps.Id);
            }
            this.comboBox1.SelectedIndex = 0;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            CreatePlan();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void IsNameAvailable(object sender, TextChangedEventArgs e)
        {
            string currentName = this.TextBox1.Text;
            List<string> PlanNames = CollectPlanNames();

            if (PlanNames.Contains(currentName) || currentName.Length > 13 || currentName.Length < 1)
            {
                this.LabelSuccess.Content = "\u274C";
                this.LabelSuccess.Foreground = Brushes.Red;
                this.OKButton.IsEnabled = false;
            }
            else
            {
                this.LabelSuccess.Content = "\u2714";
                this.LabelSuccess.Foreground = Brushes.Green;
                this.OKButton.IsEnabled = true;
            }
        }

        private List<string> CollectPlanNames()
        {
            List<string> plannames = new List<string>() { };
            string plansumname = this.comboBox1.SelectedValue.ToString();
            var plansum = this.scriptcontext.PlanSumsInScope.Single(ps => ps.Id == plansumname);

            foreach (var plan in plansum.Course.PlanSetups)
            {
                plannames.Add(plan.Id);
            }
            return plannames;
        }

        private void CreatePlan()
        {
            string plansumname = this.comboBox1.SelectedValue.ToString();
            var plansum = this.scriptcontext.PlanSumsInScope.Single(ps => ps.Id == plansumname);

            ExternalPlanSetup planWithStrSet = (ExternalPlanSetup)plansum.PlanSetups.First(); // only for use with construction method

            ExternalPlanSetup newPlan = (ExternalPlanSetup)null;
            try
            {
                newPlan = plansum.Course.AddExternalPlanSetupAsVerificationPlan(plansum.StructureSet, planWithStrSet);
                newPlan.Id = this.TextBox1.Text;
            }
            catch (Exception e)
            {
                MessageBox.Show("Cannot create plan " + this.TextBox1.Text + ".\n\n" + e.StackTrace, "Error");
                return;
            }

            try
            {
                TransferDose(newPlan, plansum);
            }
            catch (Exception f)
            {
                MessageBox.Show(f.Message + "\n" + f.StackTrace, "Error");
            }

            MessageBox.Show("The selected PlanSum was converted into a verification plan.", "Message");
            this.Close();

        }

        private void TransferDose(ExternalPlanSetup newPlan, PlanSum plansum)
        {
            int fractions = 1;
            DoseValue dosePerFraction = plansum.Dose.DoseMax3D;
            double treatPercentage = 1;

            newPlan.SetPrescription(fractions, dosePerFraction, treatPercentage);

            newPlan.PlanNormalizationValue = 100;

            EvaluationDose evalDose = newPlan.CopyEvaluationDose(plansum.Dose);

            double maxNewDoseVal = GetMaxDoseVal(evalDose, newPlan);
            double maxOldDoseVal = GetMaxDosePlanSumVal(plansum.Dose);

            int[,,] doseMatrix = GetDoseVoxelsFromDose(evalDose);

            double scaling = maxOldDoseVal / maxNewDoseVal;

            int Xsize = evalDose.XSize;
            int Ysize = evalDose.YSize;
            int Zsize = evalDose.ZSize;

            for (int k = 0; k < Zsize; k++)
            {
                int[,] plane = new int[Xsize, Ysize];
                for (int i = 0; i < Xsize; i++)
                {
                    for (int j = 0; j < Ysize; j++)
                    {
                        plane[i, j] = (int)(doseMatrix[k, i, j] * scaling);
                    }
                }
                evalDose.SetVoxels(k, plane);
            }
        }

        public double GetMaxDoseVal(Dose dose, ExternalPlanSetup plan)
        {
            DoseValue maxDose = dose.DoseMax3D;
            double maxDoseVal = maxDose.Dose;

            if (maxDose.IsRelativeDoseValue)
            {
                if (plan.TotalDose.Unit == DoseValue.DoseUnit.cGy)
                {
                    maxDoseVal = maxDoseVal * plan.TotalDose.Dose / 10000.0;
                }
                else
                {
                    maxDoseVal = maxDoseVal * plan.TotalDose.Dose / 100.0;
                }
            }

            if (maxDose.Unit == DoseValue.DoseUnit.cGy)
            {
                maxDoseVal = maxDoseVal / 100.0;
            }
            return maxDoseVal;
        }

        public double GetMaxDosePlanSumVal(Dose dose)
        {
            DoseValue maxDose = dose.DoseMax3D;
            double maxDoseVal = maxDose.Dose;

            if (maxDose.Unit == DoseValue.DoseUnit.cGy)
            {
                maxDoseVal = maxDoseVal / 100.0;
            }
            return maxDoseVal;
        }


        public int[,,] GetDoseVoxelsFromDose(Dose dose)
        {
            int Xsize = dose.XSize;
            int Ysize = dose.YSize;
            int Zsize = dose.ZSize;

            int[,,] doseMatrix = new int[Zsize, Xsize, Ysize];

            // Get whole dose matrix from context
            for (int k = 0; k < Zsize; k++)
            {
                int[,] plane = new int[Xsize, Ysize];
                dose.GetVoxels(k, plane);

                for (int i = 0; i < Xsize; i++)
                {
                    for (int j = 0; j < Ysize; j++)
                    {
                        doseMatrix[k, i, j] = plane[i, j];
                    }
                }
            }
            return doseMatrix;
        }
    }

}
