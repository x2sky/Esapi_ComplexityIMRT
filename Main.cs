////////////////////////////////////////////////////////////////////////////////////////////
///Main function that displays the complexity metrics:
/// Save csv file to predefined location with some selective plan complexity parameters.
/// Display open aperture area/jaw opening ratio & equivalent sqaure length complexity in Eclipse.
/// The metrics are computed per beam and per plan. 
///
///--version 1.0.0.0
/// Becket Hui 2022/12
///
////////////////////////////////////////////////////////////////////////////////////////////

using complexityIMRT;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using VMS.TPS.Common.Model.API;

namespace VMS.TPS
{
    public class Script
    {
        public Script()
        {
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Execute(ScriptContext context)
        {
            string fileDir = @"Folder Location";
            ExternalPlanSetup pln = context.ExternalPlanSetup;
            StreamWriter sw = new StreamWriter(Path.Combine(fileDir, context.Patient.Id + "_" + pln.Id + ".csv"));
            sw.WriteLine(context.Patient.Id + ", " + pln.Id);
            sw.WriteLine("Beam Id, Machine, Beam Energy, Beam MU, Beam Time(s), Aperture/Jaw Area, Perimeter/Area (mm-1), Org Edge Metric (mm-1)," +
                " Eq Sq Length (mm), Closed Leaf Gap (mm), Average Leaf Speed (mm/s), Average Gantry Accel (deg/s/CP)");
            string prntTxt = "";
            List<BeamControlPoints> bmCPsLs = new List<BeamControlPoints>();
            ComplexityMetrics complexity = new ComplexityMetrics();
            double muDsR, apertOpgR, normPrmtrAreaR, orgEdgeLenAreaR, eqSqLen, leafGaps, leafSpeed, gantryAccel;
            foreach (Beam bm in pln.Beams)
            {
                if (bm.MLC != null)
                {
                    BeamControlPoints bmCPs = new BeamControlPoints(bm);

                    if (bmCPs.beamMU != 0 && !Double.IsNaN(bmCPs.beamMU))
                    {
                        prntTxt += "For beam " + bmCPs.id + ", the total MU = " + bmCPs.beamMU.ToString("0.###") + ", and the beam time = " + 
                            bmCPs.beamTm.ToString("0.#") + " sec.\n";
                        sw.Write(bmCPs.id + ", " + bm.TreatmentUnit.Id + ", " + bm.EnergyModeDisplayName + ", " + bmCPs.beamMU + ", " + bmCPs.beamTm + ", ");
                        List<BeamControlPoints> currBmCPs = new List<BeamControlPoints>() { bmCPs };
                        apertOpgR = complexity.ComputeApertureJawOpenRatio(currBmCPs);
                        normPrmtrAreaR = complexity.ComputePerimeterAreaRatio(currBmCPs);
                        orgEdgeLenAreaR = complexity.ComputeOriginalEdgeLengthAreaRatio(currBmCPs);
                        eqSqLen = complexity.ComputeEquivSqLength(currBmCPs);
                        leafGaps = complexity.ComputeLeafGaps(currBmCPs);
                        leafSpeed = complexity.ComputeAverageLeafSpeed(currBmCPs);
                        gantryAccel = complexity.ComputeAverageGantryAcceleration(currBmCPs);
                        prntTxt += "- The aperture area/jaw opening ratio = " + apertOpgR.ToString("0.##") +
                            ", \n  and the equivalent square length complexity = " + eqSqLen.ToString("0.##") + " mm.\n\n";
                        sw.WriteLine(apertOpgR + ", " + normPrmtrAreaR + ", " + orgEdgeLenAreaR + ", " + eqSqLen +
                            ", " + leafGaps + ", " + leafSpeed + ", " + gantryAccel);
                        bmCPsLs.Add(bmCPs);
                    }
                }
            }
            muDsR = complexity.ComputeMUDoseRatio(bmCPsLs, pln.DosePerFraction.Dose);
            apertOpgR = complexity.ComputeApertureJawOpenRatio(bmCPsLs);
            normPrmtrAreaR = complexity.ComputePerimeterAreaRatio(bmCPsLs);
            orgEdgeLenAreaR = complexity.ComputeOriginalEdgeLengthAreaRatio(bmCPsLs);
            eqSqLen = complexity.ComputeEquivSqLength(bmCPsLs);
            leafGaps = complexity.ComputeLeafGaps(bmCPsLs);
            leafSpeed = complexity.ComputeAverageLeafSpeed(bmCPsLs);
            gantryAccel = complexity.ComputeAverageGantryAcceleration(bmCPsLs);
            prntTxt += "The total beam time = " + (bmCPsLs.Sum(bm => bm.beamTm)/60).ToString("0.#") + " min, overall MU/dose ratio = " + muDsR.ToString("0.##") + 
                ",\nwith aperture area/jaw opening ratio = " + apertOpgR.ToString("0.##") + 
                ",\nand equivalent sqaure length complexity = " + eqSqLen.ToString("0.##") + " mm.";
            MessageBox.Show(prntTxt);
            sw.WriteLine("Total:, , , " + bmCPsLs.Sum(bmcp => bmcp.beamMU) + ", " + bmCPsLs.Sum(bmcp => bmcp.beamTm) + ", " +
                apertOpgR + ", " + normPrmtrAreaR + ", " + orgEdgeLenAreaR + ", " + eqSqLen + ", " +
                leafGaps + ", " + leafSpeed + ", " + gantryAccel);
            sw.Close();
        }
    }
}
