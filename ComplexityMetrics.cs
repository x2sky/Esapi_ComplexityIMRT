////////////////////////////////////////////////////////////////////////////////////////////
///Functions that compute different complexity metrics
///Functions:
/// - ComputeMUDoseRatio(beamCPsList, prescribedDs): Compute traditional MU over dose modulation factor
/// - ComputeTotalApertureMU(beamCPsList): Compute total MU contributed by each aperture
/// - ComputeAverageAperture(beamCPsList): Compute MU weighted overall average number of apertures
/// - ComputeApertureJawOpenRatio(beamCPsList): Compute MU weighted open aperture area over open jaw area ratio 
/// - ComputePerimeterAreaRatio(beamCPsList): Compute overall MU weighted open aperture perimeter over area ratio
/// - ComputeAveragePerimeterAreaRatio(beamCPsList): Compute average aperture MU weighted perimeter over aperture area ratio, w/ respect to control point
/// - ComputeOriginalEdgeLengthAreaRatio(beamCPsList): Compute original EM ratio: overall MU & CP weighted open leaf edge length over aperture area ratio
/// - ComputeEdgeLengthAreaRatio(beamCPsList): Compute overall aperture MU weighted open leaf edge length over aperture area ratio
/// - ComputeEquivSqLength(beamCPsList): Compute length of equivalent square with the same leaf edge length over area ratio  **RECOMMEND**
/// - ComputeAverageEdgeLengthAreaRatio(beamCPsList): Compute average aperture MU weighted open leaf edge length over aperture area ratio, w/ respect to control point
/// - ComputeApertureHistogram(beamCPsList, binSize): Generate histogram of the aperture area, bin size = binSz mm^2
/// - ComputeAverageApertureArea(beamCPsList): Compute average aperture areaw, w/ respect to control point
/// - ComputeApertureSkewness(beamCPsList): Compute skewness of the aperture area
/// - ComputeLeafGaps(beamCPsList): Compute MU weighted leaf gaps within opening jaw
/// - ComputeAverageLeafSpeed(bmCPsLs): Compute MU weighted overall average leaf speed
/// - ComputeAverageGantryAcceleration(beamCPsList): Compute MU weighted overall average change in gantry speed
/// - AreBeamsValid(beamCPsList): Check if all beams have MUs, note that beamCPsList would have MLC control points
///
///--version 1.0.0.2
/// Change description
/// Change methods to static
/// 
/// Becket Hui 2023/09
/// 
///--version 1.0.0.1
/// Changed the previous definition of normalized Edge Area Ratio to average Edge Area Ratio
/// Added another definition for normalized Edge Area Ratio
/// Same change applied to Perimeter Area Ratio
/// Applied 2x inverse of normalized edge area ratio to get equivalent square length as complexity metric
///  
/// Becket Hui 2022/12
///
///--version 1.0.0.0
/// Becket Hui 2022/10
///
////////////////////////////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Schema;
using VMS.TPS.Common.Model.API;

namespace complexityIMRT
{
    internal class ComplexityMetrics
    {
        public static double ComputeMUDoseRatio(List<BeamControlPoints> bmCPsLs, double presDs)
        // Compute traditional MU over dose modulation factor //
        {
            double totMU = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                totMU = bmCPsLs.Sum(bm => bm.beamMU);
            }
            return totMU / presDs;
        }
        public static double ComputeTotalApertureMU(List<BeamControlPoints> bmCPsLs)
        // Compute total MU contributed by each aperture //
        {
            double totMU = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    totMU += bmCPs.bmCPApertLs.Sum(ctrPt => (ctrPt.avgMU * ctrPt.apertures.Count));
                }
            }
            return totMU;
        }
        public static double ComputeAverageAperture(List<BeamControlPoints> bmCPsLs)
        // Compute MU weighted overall average number of apertures //
        {
            double apertRate = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = bmCPsLs.Sum(bm => bm.beamMU);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    apertRate += bmCPs.bmCPApertLs.Sum(ctrPt => (ctrPt.avgMU * ctrPt.apertures.Count)) / totMU;
                }
            }
            return apertRate;
        }
        public static double ComputeApertureJawOpenRatio(List<BeamControlPoints> bmCPsLs)
        // Compute MU weighted open aperture area over open jaw area ratio //
        {
            double totApertJawR = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = bmCPsLs.Sum(bm => bm.beamMU);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        double apertJawR = ctrPt.apertures.Sum(ap => ap.area) / ctrPt.jawArea;
                        totApertJawR += ctrPt.avgMU * apertJawR / totMU;
                    }
                }
            }
            return totApertJawR;
        }
        public static double ComputePerimeterAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute overall MU weighted open aperture perimeter over area ratio //
        {
            double totMUPrmtr = 0;
            double totMUArea = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        if (ctrPt.apertures.Count > 0)
                        {
                            totMUPrmtr += ctrPt.avgMU * ctrPt.apertures.Sum(ap => ap.perimeter);
                            totMUArea += ctrPt.avgMU * ctrPt.apertures.Sum(ap => ap.area);
                        }
                    }
                }
            }
            return totMUPrmtr/totMUArea;
        }
        public static double ComputeAveragePerimeterAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute average aperture MU weighted perimeter over aperture area ratio, w/ respect to control point //
        {
            double totPrmtrAreaR = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = ComputeTotalApertureMU(bmCPsLs);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        double prmtrAreaR = ctrPt.apertures.Sum(ap => ap.perimeter / ap.area);  // return 0 if apertures empty
                        totPrmtrAreaR += ctrPt.avgMU * prmtrAreaR / totMU;
                    }
                }
            }
            return totPrmtrAreaR;
        }
        public static double ComputeOriginalEdgeLengthAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute original EM ratio: overall MU & CP weighted open leaf edge length over aperture area ratio //
        {
            double totEdgeLenAreaR = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = bmCPsLs.Sum(bm => bm.beamMU);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        if (ctrPt.apertures.Count > 0)
                        {
                            double edgeLenAreaR = ctrPt.apertures.Sum(ap => ap.edgeLen) / ctrPt.apertures.Sum(ap => ap.area);
                            totEdgeLenAreaR += ctrPt.avgMU * edgeLenAreaR / totMU;
                        }
                    }
                }
            }
            return totEdgeLenAreaR;
        }
        public static double ComputeEdgeLengthAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute overall aperture MU weighted open leaf edge length over aperture area ratio //
        {
            double totMUEdgeLen = 0;
            double totMUArea = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = bmCPsLs.Sum(bm => bm.beamMU);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        if (ctrPt.apertures.Count > 0)
                        {
                            totMUEdgeLen += ctrPt.avgMU * ctrPt.apertures.Sum(ap => ap.edgeLen);
                            totMUArea += ctrPt.avgMU * ctrPt.apertures.Sum(ap => ap.area);
                        }
                    }
                }
            }
            return totMUEdgeLen/totMUArea;
        }
        public static double ComputeEquivSqLength(List<BeamControlPoints> bmCPsLs)
        // Compute length of equivalent square with the same leaf edge length over area ratio //
        {
            double totMUEdgeLen = 0;
            double totMUArea = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = bmCPsLs.Sum(bm => bm.beamMU);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        if (ctrPt.apertures.Count > 0)
                        {
                            totMUEdgeLen += ctrPt.avgMU * ctrPt.apertures.Sum(ap => ap.edgeLen);
                            totMUArea += ctrPt.avgMU * ctrPt.apertures.Sum(ap => ap.area);
                        }
                    }
                }
            }
            return 2 * totMUArea/totMUEdgeLen;
        }
        public static double ComputeAverageEdgeLengthAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute average aperture MU weighted open leaf edge length over aperture area ratio, w/ respect to control point //
        {
            double totEdgeLenAreaR = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = ComputeTotalApertureMU(bmCPsLs);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        double edgeLenAreaR = ctrPt.apertures.Sum(ap => ap.edgeLen / ap.area);  // return 0 if apertures empty
                        totEdgeLenAreaR += ctrPt.avgMU * edgeLenAreaR / totMU;
                    }
                }
            }
            return totEdgeLenAreaR;
        }
        public static Dictionary<int, double> ComputeApertureHistogram(List<BeamControlPoints> bmCPsLs, int binSz)
        // Generate histogram of the aperture area, bin size = binSz mm^2 //
        {
            Dictionary <int, double> apertHist = new Dictionary<int, double>();
            int NBins = (int) Math.Floor(120000.0 / binSz);  // max area = 400 x 300 mm^2
            for (int idx = 0; idx < NBins; idx++)
            {
                apertHist.Add((idx + 1) * binSz, 0);  // create bin of binSz mm^2
            }
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = ComputeTotalApertureMU(bmCPsLs);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        foreach (ControlPointAperture apert in ctrPt.apertures)
                        {
                            int binKey = (int) Math.Ceiling(apert.area / binSz) * binSz;
                            apertHist[binKey] += ctrPt.avgMU / totMU;
                        }
                    }
                }
            }
            return apertHist;
        }
        public static double ComputeAverageApertureArea(List<BeamControlPoints> bmCPsLs)
        // Compute average aperture areaw, w/ respect to control point //
        {
            double avgArea = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = ComputeTotalApertureMU(bmCPsLs);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        foreach (ControlPointAperture apert in ctrPt.apertures)
                        {
                            avgArea += ctrPt.avgMU * apert.area / totMU;
                        }
                    }
                }
            }
            return avgArea;
        }
        public static double ComputeApertureSkewness(List<BeamControlPoints> bmCPsLs)
        // Compute skewness of the aperture area //
        {
            double skewness = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = ComputeTotalApertureMU(bmCPsLs);
                // First compute average aperture area weighted by MU //
                double avg = ComputeAverageApertureArea(bmCPsLs);
                // Then calculate standard deviation & 3rd moment //
                double varnce = 0;
                double momt3 = 0;
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    foreach (BeamControlPointAperture ctrPt in bmCPs.bmCPApertLs)
                    {
                        foreach (ControlPointAperture apert in ctrPt.apertures)
                        {
                            varnce += ctrPt.avgMU / totMU * Math.Pow(apert.area - avg, 2);
                            momt3 += ctrPt.avgMU / totMU * Math.Pow(apert.area, 3);
                        }
                    }
                }
                skewness = (momt3 - 3 * avg * varnce - Math.Pow(avg, 3)) / Math.Pow(varnce, 1.5);
            }
            return skewness;
        }
        public static double ComputeLeafGaps(List<BeamControlPoints> bmCPsLs)
        // Compute MU weighted leaf gaps within opening jaw //
        {
            double totLeafGaps = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = bmCPsLs.Sum(bm => bm.beamMU);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    totLeafGaps += bmCPs.bmCPApertLs.Sum(cp => cp.avgMU * cp.leafGaps) / totMU;
                }
            }
            return totLeafGaps;
        }
        public static double ComputeAverageLeafSpeed(List<BeamControlPoints> bmCPsLs)
        // Compute MU weighted overall average leaf speed //
        {
            double totLeafSpeed = 0;
            double totMU = 1;
            if (AreBeamsValid(bmCPsLs))
            {
                totMU = bmCPsLs.Sum(bm => bm.beamMU);
                foreach (BeamControlPoints bmCPs in bmCPsLs)
                {
                    totLeafSpeed += bmCPs.bmCPDynLs.Sum(cp => cp.cpMU*cp.cpAvgLeafV);
                }
            }
            return totLeafSpeed / totMU;
        }
        public static double ComputeAverageGantryAcceleration(List<BeamControlPoints> bmCPsLs)
        //Compute MU weighted overall average change in gantry speed //
        {
            double totGantryAccel = 0;
            double totMU = 1;
            if (AreBeamsValid(bmCPsLs))
            {
                totMU = bmCPsLs.Sum(bm => bm.beamMU);
                foreach(BeamControlPoints bmCPs in bmCPsLs)
                {
                    double cpMU0 = 0;
                    double cpGantryV0 = 0;
                    for(int idxCP = 0;idxCP < bmCPs.bmCPDynLs.Count; idxCP++)
                    {
                        totGantryAccel += 0.5 * (bmCPs.bmCPDynLs[idxCP].cpMU + cpMU0) * Math.Abs(bmCPs.bmCPDynLs[idxCP].cpGantryV - cpGantryV0);
                        cpMU0 = bmCPs.bmCPDynLs[idxCP].cpMU;
                        cpGantryV0 = bmCPs.bmCPDynLs[idxCP].cpGantryV;
                    }
                    totGantryAccel += 0.5 * (0 + cpMU0) * Math.Abs(0 - cpGantryV0);
                }
            }
            return totGantryAccel / totMU;
        }
        private static bool AreBeamsValid(List<BeamControlPoints> bmCPsLs)
        // Check if all beams have MUs //
        {
            if (bmCPsLs.Count == 0) return false;
            foreach (BeamControlPoints bmCPs in bmCPsLs)
            {
                if (bmCPs.beamMU == 0 || Double.IsNaN(bmCPs.beamMU))
                {
                    return false;
                }
            }
            return true;
        }
    }
}
