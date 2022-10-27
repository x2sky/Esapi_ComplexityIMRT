//////////////////////////////////////////////////////////////////////
///Functions that compute different complexity metrics
///Functions:
/// - ComputeMUDoseRatio(beamCPsList, prescribedDs): Compute traditional modulation factor
/// - ComputeTotalApertureMU(beamCPsList): Compute the total MU contributed by each aperture
/// - ComputeAverageAperture(beamCPsList): Compute the MU weighted average number of apertures
/// - ComputeApertureJawOpenRatio(beamCPsList): Compute the MU weighted aperture area over jaw opening area ratio 
/// - ComputePerimeterAreaRatio(beamCPsList): Compute the MU weighted aperture perimeter over aperture area ratio
/// - ComputeApertureMUWeightedPerimeterAreaRatio(beamCPsList): Compute the apperture MU weighted aperture perimeter over aperture area ratio
/// - ComputeOriginalEdgeLengthAreaRatio(beamCPsList): Compute the original MU weighted control point weighted aperture open leaf edge length over aperture area ratio
/// - ComputeEdgeLengthAreaRatio(beamCPsList): Compute the MU weighted aperture open leaf edge length over aperture area ratio
/// - ComputeApertureMUWeightedEdgeLengthAreaRatio(beamCPsList): Compute the apperture MU weighted aperture open leaf edge length over aperture area ratio
/// - ComputeApertureHistogram(beamCPsList, binSize): Generate the histogram of the aperture area, bin size = binSz mm^2
/// - ComputeAverageApertureArea(beamCPsList): Compute the average aperture area
/// - ComputeApertureSkewness(beamCPsList): Compute the skewness of the aperture area
/// - ComputeLeafGaps(beamCPsList): Compute the MU weighted leaf gaps within the opening jaw
/// - ComputeAverageLeafSpeed(bmCPsLs): Compute the MU weighted average leaf speed
/// - ComputeAverageGantryAcceleration(beamCPsList): Compute the average change in gantry speed
/// - AreBeamsValid(beamCPsList): Check if all beams have MUs, note that beamCPsList would have MLC control points
/// 
///--version 1.0.0.0
///Becket Hui 2022/10
///
//////////////////////////////////////////////////////////////////////
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
        public double ComputeMUDoseRatio(List<BeamControlPoints> bmCPsLs, double presDs)
        // Compute the MU to prescribed dose ratio //
        {
            double totMU = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                totMU = bmCPsLs.Sum(bm => bm.beamMU);
            }
            return totMU / presDs;
        }
        public double ComputeTotalApertureMU(List<BeamControlPoints> bmCPsLs)
        // Compute the total MU contributed by each aperture //
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
        public double ComputeAverageAperture(List<BeamControlPoints> bmCPsLs)
        // Compute the MU weighted average number of apertures //
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
        public double ComputeApertureJawOpenRatio(List<BeamControlPoints> bmCPsLs)
        // Compute the overall MU weighted aperture area over jaw opening area ratio //
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
        public double ComputePerimeterAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute the overall MU weighted aperture perimeter over aperture area ratio //
        {
            double totPrmtrAreaR = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = bmCPsLs.Sum(bm => bm.beamMU);
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
        public double ComputeApertureMUWeightedPerimeterAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute the overall apperture MU weighted aperture perimeter over aperture area ratio //
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
        public double ComputeOriginalEdgeLengthAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute the original overall MU weighted control point weighted aperture open leaf edge length over aperture area ratio //
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
        public double ComputeEdgeLengthAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute the overall MU weighted aperture open leaf edge length over aperture area ratio //
        {
            double totEdgeLenAreaR = 0;
            if (AreBeamsValid(bmCPsLs))
            {
                double totMU = bmCPsLs.Sum(bm => bm.beamMU);
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
        public double ComputeApertureMUWeightedEdgeLengthAreaRatio(List<BeamControlPoints> bmCPsLs)
        // Compute the overall apperture MU weighted aperture open leaf edge length over aperture area ratio //
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
        public Dictionary<int, double> ComputeApertureHistogram(List<BeamControlPoints> bmCPsLs, int binSz)
        // Generate the histogram of the aperture area, bin size = binSz mm^2 //
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
        public double ComputeAverageApertureArea(List<BeamControlPoints> bmCPsLs)
        // Compute the average aperture area //
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
        public double ComputeApertureSkewness(List<BeamControlPoints> bmCPsLs)
        // Compute the skewness of the aperture area //
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
        public double ComputeLeafGaps(List<BeamControlPoints> bmCPsLs)
        // Compute the overall MU weighted leaf gaps within the opening jaw //
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
        public double ComputeAverageLeafSpeed(List<BeamControlPoints> bmCPsLs)
        // Compute the MU weighted average leaf speed
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
        public double ComputeAverageGantryAcceleration(List<BeamControlPoints> bmCPsLs)
        //Compute the average gantry acceleration between each control point
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
