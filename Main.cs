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
            string fileDir = @"\\cndubrocphy\Physics\Eclipse\Scripting\ComplexityMetric\Results";
            ExternalPlanSetup pln = context.ExternalPlanSetup;
            StreamWriter sw = new StreamWriter(Path.Combine(fileDir, context.Patient.Id + "_" + pln.Id + ".csv"));
            sw.WriteLine(context.Patient.Id + ", " + pln.Id);
            sw.WriteLine("Beam Id, Machine, Beam Energy, Beam MU, Beam Time(s), Aperture/Jaw Area, Perimeter/Area (mm-1), OG Edge/Area (mm-1)," +
                " Edge/Area (mm-1), Closed Leaf Gap (mm), Average Leaf Speed (mm/s), Average Gantry Accel (deg/s/CP)");
            string prntTxt = "";
            List<BeamControlPoints> bmCPsLs = new List<BeamControlPoints>();
            ComplexityMetrics complexity = new ComplexityMetrics();
            double muDsR, apertOpgR, normPrmtrAreaR, orgEdgeLenAreaR, normEdgeLenAreaR, leafGaps, leafSpeed, gantryAccel;
            foreach (Beam bm in pln.Beams)
            {
                if (bm.MLC != null)
                {
                    BeamControlPoints bmCPs = new BeamControlPoints(bm);
                    //foreach (BeamControlPointGantry cp in bmCPs.bmCPGantryLs)
                    //{
                    //    sw.WriteLine("-For cp " + cp.idx + ", the MU is " + cp.cpMU.ToString() + ", the gantry speed is " + cp.cpGantryV.ToString() +
                    //        " deg/s, with dose rate equals " + cp.cpDsRt.ToString() + " MU/min.");
                    //}
                    //foreach (BeamControlPointAperture cp in bmCPs.bmCPApertLs)
                    //{
                    //    sw.WriteLine("-For cp " + cp.idx + ", the average MU is " + cp.avgMU + ", the open jaw perimeter = " + cp.jawPerimeter +
                    //        ", with area = " + cp.jawArea + ", and there are " + cp.apertures.Count +
                    //        " apertures and " + cp.leafGaps + "mm of total leaf gaps within opening.");
                    //    foreach (ControlPointAperture ap in cp.apertures)
                    //    {
                    //        sw.Write("aperture has area " + ap.area + ", perimeter " + ap.perimeter + "...  ");
                    //    }
                    //    sw.WriteLine("");
                    //}
                    if (bmCPs.beamMU != 0 && !Double.IsNaN(bmCPs.beamMU))
                    {
                        prntTxt += "For beam " + bmCPs.id + ", the total MU = " + bmCPs.beamMU.ToString("0.###") + ", and the beam time = " + 
                            bmCPs.beamTm.ToString("0.#") + " sec.\n";
                        sw.Write(bmCPs.id + ", " + bm.TreatmentUnit.Id + ", " + bm.EnergyModeDisplayName + ", " + bmCPs.beamMU + ", " + bmCPs.beamTm + ", ");
                        List<BeamControlPoints> currBmCPs = new List<BeamControlPoints>() { bmCPs };
                        apertOpgR = complexity.ComputeApertureJawOpenRatio(currBmCPs);
                        normPrmtrAreaR = complexity.ComputeApertureMUWeightedPerimeterAreaRatio(currBmCPs);
                        orgEdgeLenAreaR = complexity.ComputeOriginalEdgeLengthAreaRatio(currBmCPs);
                        normEdgeLenAreaR = complexity.ComputeApertureMUWeightedEdgeLengthAreaRatio(currBmCPs);
                        leafGaps = complexity.ComputeLeafGaps(currBmCPs);
                        leafSpeed = complexity.ComputeAverageLeafSpeed(currBmCPs);
                        gantryAccel = complexity.ComputeAverageGantryAcceleration(currBmCPs);
                        prntTxt += "- The aperture area/jaw opening ratio = " + apertOpgR.ToString("0.##") +
                            ", \n  and the complexity metric = " + normEdgeLenAreaR.ToString("0.##") + " mm-1.\n\n";
                        sw.WriteLine(apertOpgR + ", " + normPrmtrAreaR + ", " + orgEdgeLenAreaR + ", " + normEdgeLenAreaR +
                            ", " + leafGaps + ", " + leafSpeed + ", " + gantryAccel);
                        bmCPsLs.Add(bmCPs);
                    }
                }
            }
            //double muDsR = complexity.ComputeMUDoseRatio(bmCPsLs, pln.DosePerFraction.Dose);
            //double apertCtrPtR = complexity.ComputeAverageAperture(bmCPsLs);
            //double apertOpgR = complexity.ComputeApertureJawOpenRatio(bmCPsLs);
            //double prmtrAreaR = complexity.ComputePerimeterAreaRatio(bmCPsLs);
            //double normPrmtrAreaR = complexity.ComputeApertureMUWeightedPerimeterAreaRatio(bmCPsLs);
            //double edgeLenAreaR = complexity.ComputeEdgeLengthAreaRatio(bmCPsLs);
            //double orgEdgeLenAreaR = complexity.ComputeOriginalEdgeLengthAreaRatio(bmCPsLs);
            //double normEdgeLenAreaR = complexity.ComputeApertureMUWeightedEdgeLengthAreaRatio(bmCPsLs);
            //double leafGaps = complexity.ComputeLeafGaps(bmCPsLs);
            //double gantryAccel = complexity.ComputeAverageGantryAcceleration(bmCPsLs);
            //Dictionary<int, double> apertHist = complexity.ComputeApertureHistogram(bmCPsLs, 200);
            //double avgApertArea = complexity.ComputeAverageApertureArea(bmCPsLs);
            //double apertAreaSkew = complexity.ComputeApertureSkewness(bmCPsLs);
            //sw.WriteLine("The MU over prescribed dose ratio is " + muDsR.ToString() + ".");
            //sw.WriteLine("The avg no. of apertures is " + apertCtrPtR.ToString() + ".");
            //sw.WriteLine("The aperture area over jaw opening ratio is " + apertOpgR.ToString() + ".");
            //sw.WriteLine("The aperture perimeter over area ratio is " + prmtrAreaR.ToString() + "mm-1.");
            //sw.WriteLine("The aperture MU weighted perimeter over area ratio is " + normPrmtrAreaR.ToString() + "mm-1.");
            //sw.WriteLine("The aperture open edge length over area ratio is " + edgeLenAreaR.ToString() + "mm-1.");
            //sw.WriteLine("The aperture MU weighted aperture open edge length over area ratio is " + normEdgeLenAreaR.ToString() + "mm-1.");
            //sw.WriteLine("The original aperture open edge length over area ratio is " + orgEdgeLenAreaR.ToString() + "mm-1.");
            //sw.WriteLine("The average closed leaf gaps within jaw opening is " + leafGaps + "mm.");
            //sw.WriteLine("The average gantry acceleration is " + gantryAccel + "deg/s/ctrl pt.");
            //sw.WriteLine("The average aperture area is " + avgApertArea + "mm^2.");
            //sw.WriteLine("The skewness of aperture are is " + apertAreaSkew + ".");
            //sw.WriteLine("Aperture area histogram: 100 - " + apertHist[200].ToString() + ", 300 - " + apertHist[400].ToString() +
            //    ", 500 - " + apertHist[600].ToString() + ", 700 - " + apertHist[800].ToString() + ", 900 - " + apertHist[1000].ToString() +
            //    ", 1100 - " + apertHist[1200].ToString() + ", 1300 - " + apertHist[1400].ToString() + ", 1500 - " + apertHist[1600].ToString() +
            //    ", 1700 - " + apertHist[1800].ToString() + ", 1900 - " + apertHist[2000].ToString() + ", 2100 - " + apertHist[2200].ToString());
            muDsR = complexity.ComputeMUDoseRatio(bmCPsLs, pln.DosePerFraction.Dose);
            apertOpgR = complexity.ComputeApertureJawOpenRatio(bmCPsLs);
            normPrmtrAreaR = complexity.ComputeApertureMUWeightedPerimeterAreaRatio(bmCPsLs);
            orgEdgeLenAreaR = complexity.ComputeOriginalEdgeLengthAreaRatio(bmCPsLs);
            normEdgeLenAreaR = complexity.ComputeApertureMUWeightedEdgeLengthAreaRatio(bmCPsLs);
            leafGaps = complexity.ComputeLeafGaps(bmCPsLs);
            leafSpeed = complexity.ComputeAverageLeafSpeed(bmCPsLs);
            gantryAccel = complexity.ComputeAverageGantryAcceleration(bmCPsLs);
            prntTxt += "The total beam time = " + (bmCPsLs.Sum(bm => bm.beamTm)/60).ToString("0.#") + " min, overall MU/dose ratio = " + muDsR.ToString("0.##") + 
                ",\nwith aperture area/jaw opening ratio = " + apertOpgR.ToString("0.##") + 
                ",\nand complexity metric = " + normEdgeLenAreaR.ToString("0.##") + " mm-1.";
            MessageBox.Show(prntTxt);
            sw.WriteLine("Total:, , , " + bmCPsLs.Sum(bmcp => bmcp.beamMU) + ", " + bmCPsLs.Sum(bmcp => bmcp.beamTm) + ", " +
                apertOpgR + ", " + normPrmtrAreaR + ", " + orgEdgeLenAreaR + ", " + normEdgeLenAreaR + ", " +
                leafGaps + ", " + leafSpeed + ", " + gantryAccel);
            sw.Close();
        }
    }
}
