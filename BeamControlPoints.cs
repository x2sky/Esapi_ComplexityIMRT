//////////////////////////////////////////////////////////////////////
///Beam Control Points class & functions defined for complexity IMRT script//
///Main Class:
/// BeamControlPoints:
///     idx, mlcModel, leafWidthLs, beamMU, beamTm, bmCPLs
///     BeamControlPoints(beam) - Extract information & compute aperture parameters from the beam
///     getMaxGantrySpeed(machineId) - get max gantry speed
///     getLeafWidths(mlcModel) - get mlc widths
///     ModCustom(a, n) - customized a % n
///Other Classes:
///     BeamControlPoint: class of single control point
///     ControlPointAperture: class of single aperture
///     LeafSpecs: class of leaf specifications
/// 
///--version 1.0.0.0
///Becket Hui 2022/10
///
//////////////////////////////////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace complexityIMRT
{
    internal class BeamControlPoints
    // Class for beam control points //
    {
        public string id { get; private set; }
        public string mlcModel { get; private set; }
        public List<LeafSpecs> leafWidthLs { get; private set; }  // leaf widths <y position, width> in mm
        public double beamMU { get; private set; }
        public double beamTm { get; private set; }
        public List<BeamControlPointAperture> bmCPApertLs { get; private set; }  // list of beam control point objects
        public List<BeamControlPointDynamic> bmCPDynLs { get; private set; } // gantry control point objects
        public BeamControlPoints(Beam bm)
        {
            if (bm.MLC == null) return;  // no MLC, return
            this.id = bm.Id;
            this.mlcModel = bm.MLC.Model;
            this.leafWidthLs = getLeafWidths(this.mlcModel);
            if (this.leafWidthLs.Count == 0) return;  // no MLC model, return
            this.beamMU = bm.Meterset.Value;  // MU for the beam
            if (this.beamMU == double.NaN) return;  // if no MU, return
            this.beamTm = 0;  // beam time in sec
            // Gather aperture information from each control point //
            this.bmCPApertLs = new List<BeamControlPointAperture>();
            for (int idxCP = 0; idxCP < bm.ControlPoints.Count; idxCP++)
            {
                // initialize & assign control point class variables //
                BeamControlPointAperture cp = new BeamControlPointAperture(idxCP, bm.ControlPoints[idxCP].JawPositions.X1, bm.ControlPoints[idxCP].JawPositions.X2,
                    bm.ControlPoints[idxCP].JawPositions.Y1, bm.ControlPoints[idxCP].JawPositions.Y2, 0);  // internal control point class
                float[,] cpLeafPos = bm.ControlPoints[idxCP].LeafPositions;  // leaf positions of control point idxCP
                if (idxCP == 0)  // first control point
                {
                    cp.avgMU = 0.5 * bm.ControlPoints[idxCP + 1].MetersetWeight * this.beamMU;
                }
                else if (idxCP == bm.ControlPoints.Count - 1)  // last control point
                {
                    cp.avgMU = 0.5 * (bm.ControlPoints[idxCP].MetersetWeight - bm.ControlPoints[idxCP - 1].MetersetWeight) * this.beamMU;
                }
                else  // all other control points in between
                {
                    cp.avgMU = 0.5 * (bm.ControlPoints[idxCP + 1].MetersetWeight - bm.ControlPoints[idxCP - 1].MetersetWeight) * this.beamMU;
                }
                // declare aperture specific parameters //
                bool openNow = false;
                double perimeter = 0;  // perimeter of aperture
                double edgeLen = 0;  // perimeter - leaf width of aperture
                double area = 0;  // area of aperture
                double lenOpen;  // open length between MLC pair from two banks
                double wdthOpen;  // open width of the MLC
                double edgeClose_N0;  // close edge (bank 0) in the negative side of the open aperture of the MLC pair
                double edgeClose_N1;  // close edge (bank 1) in the negative side of the open aperture of the MLC pair
                double edgeClose_P0;  // close edge (bank 0) in the positive side of the open aperture of the MLC pair
                double edgeClose_P1;  // close edge (bank 1) in the positive side of the open aperture of the MLC pair
                // process apperture specs from MLC and add to the control point class //
                for (int idxLf = 0; idxLf < this.leafWidthLs.Count; idxLf++)
                {
                    LeafSpecs leaf = this.leafWidthLs[idxLf];  // leaf specs of the current leaf pair
                    if (cp.jawY1 < leaf.yPos + 0.5 * leaf.width && leaf.yPos - 0.5 * leaf.width < cp.jawY2 &&
                        cp.jawX1 < cpLeafPos[0, idxLf] && cp.jawX2 > cpLeafPos[1, idxLf])  // if MLC pairs within jaw opening
                    {
                        lenOpen = cpLeafPos[1, idxLf] - cpLeafPos[0, idxLf];
                        if (lenOpen > 0.505) // the leaf opening must by above 0.5 mm
                        {
                            if (!openNow)
                            {
                                openNow = true;
                                perimeter = 0;
                                edgeLen = 0;
                                area = 0;
                            }
                            wdthOpen = Math.Min(leaf.width,
                                Math.Min(leaf.yPos + 0.5 * leaf.width - cp.jawY1, cp.jawY2 - leaf.yPos + 0.5 * leaf.width));  // open width = leaf width unless part blocked by jaw
                            // compute close edge length (MLC length side) surrounding the aperture of the MLC pair
                            if (cp.jawY1 >= leaf.yPos - 0.5 * leaf.width ||
                                cpLeafPos[1, idxLf - 1] - cpLeafPos[0, idxLf - 1] <= 0.505)  // edge is defined by the y1 jaw or previous leaf gap < 0.5
                            {
                                edgeClose_N0 = lenOpen;
                                edgeClose_N1 = 0;
                            }
                            else  // edge is defined by the previous mlc pairs
                            {
                                edgeClose_N0 = Math.Max(0, Math.Min(cpLeafPos[0, idxLf - 1] - cpLeafPos[0, idxLf], lenOpen));
                                edgeClose_N1 = Math.Max(0, Math.Min(cpLeafPos[1, idxLf] - cpLeafPos[1, idxLf - 1], lenOpen));
                            }
                            if (cp.jawY2 <= leaf.yPos + 0.5 * leaf.width ||
                                cpLeafPos[1, idxLf + 1] - cpLeafPos[0, idxLf + 1] <= 0.505) // edge is defined by the y2 jaw or next leaf gap < 0.5
                            {
                                edgeClose_P0 = lenOpen;
                                edgeClose_P1 = 0;
                            }
                            else // edge is defined by the next mlc pairs
                            {
                                edgeClose_P0 = Math.Max(0, Math.Min(cpLeafPos[0, idxLf + 1] - cpLeafPos[0, idxLf], lenOpen));
                                edgeClose_P1 = Math.Max(0, Math.Min(cpLeafPos[1, idxLf] - cpLeafPos[1, idxLf + 1], lenOpen));
                            }
                            perimeter += edgeClose_N0 + edgeClose_N1 + edgeClose_P0 + edgeClose_P1 + 2 * wdthOpen;
                            edgeLen += edgeClose_N0 + edgeClose_N1 + edgeClose_P0 + edgeClose_P1;
                            area += lenOpen * wdthOpen;
                            if (edgeClose_P0 == lenOpen || edgeClose_P1 == lenOpen)  // if the next mlc pairs close the current aperture
                            {
                                openNow = false;
                                cp.AddAperture(perimeter, edgeLen, area);  // add the completed apperture
                            }
                        }
                        else  // the leaf opening is 0.5 mm and within jaw opening
                        {
                            cp.leafGaps += leaf.width;  // add the 0.5 mm opening to leafgap
                        }
                    }
                }
                // add control point to the beam control point list
                bmCPApertLs.Add(cp);
            }
            // Gather dynamic information from each control point //
            if (bm.MLCPlanType == MLCPlanType.VMAT || bm.MLCPlanType == MLCPlanType.ArcDynamic)  // this part only works for arcs
            {
                this.bmCPDynLs = new List<BeamControlPointDynamic>();
                double bmMaxDsRt = bm.DoseRate / 60.0;  // dose rate in sec
                double bmGantryMaxV = getMaxGantrySpeed(bm.TreatmentUnit.Id);  // max gantry speed = 4.8 deg/s or 6.0 deg/s
                if (bmGantryMaxV == 0) return;  // no machine model, return
                double cpAvgLeafV = 0;
                double cpGantryAng0 = bm.ControlPoints[0].GantryAngle;
                double cpMU0 = bm.ControlPoints[0].MetersetWeight * this.beamMU;
                float[,] cpLeafPos0 = bm.ControlPoints[0].LeafPositions;
                for (int idxCP = 1; idxCP < bm.ControlPoints.Count; idxCP++)
                {
                    double cpGantryAng = bm.ControlPoints[idxCP].GantryAngle;
                    double cpMU = bm.ControlPoints[idxCP].MetersetWeight * this.beamMU;
                    double cpGantryV = bmGantryMaxV;
                    double cpDsRt = bmMaxDsRt;
                    double bestMUTm = (cpMU - cpMU0) / bmMaxDsRt;
                    double bestGantryTm = Math.Abs(ModCustom(cpGantryAng - cpGantryAng0 + 180, 360) - 180) / bmGantryMaxV;
                    // start computing overall leaf travel distance //
                    float[,] cpLeafPos = bm.ControlPoints[idxCP].LeafPositions;
                    double wLeafTravelDist = 0;
                    double jawY1 = Math.Min(bmCPApertLs[idxCP - 1].jawY1, bmCPApertLs[idxCP].jawY1);
                    double jawY2 = Math.Max(bmCPApertLs[idxCP - 1].jawY2, bmCPApertLs[idxCP].jawY2);
                    double jawX1 = Math.Min(bmCPApertLs[idxCP - 1].jawX1, bmCPApertLs[idxCP].jawX1);
                    double jawX2 = Math.Min(bmCPApertLs[idxCP - 1].jawX2, bmCPApertLs[idxCP].jawX2);
                    double widthYMax = bmCPApertLs.Max(cp => cp.jawY2 - cp.jawY1);
                    for (int idxLf = 0; idxLf < this.leafWidthLs.Count; idxLf++)  // start computing average speed //
                    {
                        LeafSpecs leaf = this.leafWidthLs[idxLf];  // leaf specs of the current leaf pair
                        if (jawY1 < leaf.yPos + 0.5 * leaf.width && leaf.yPos - 0.5 * leaf.width < jawY2)  // if MLC pairs within jaw opening
                        {
                            if ((cpLeafPos[1, idxLf] < jawX1 && cpLeafPos0[1, idxLf] < jawX1) ||
                                (cpLeafPos[0, idxLf] > jawX2 && cpLeafPos0[0, idxLf] > jawX2))  // if MLC opening is behind x jaw
                            {
                                wLeafTravelDist += 0;
                            }
                            else
                            {
                                wLeafTravelDist += leaf.width / widthYMax * 0.5 *
                                    (Math.Abs(cpLeafPos[1, idxLf] - cpLeafPos0[1, idxLf]) + Math.Abs(cpLeafPos[0, idxLf] - cpLeafPos0[0, idxLf]));
                            }
                        }
                    }
                    // compute speed
                    if (bestMUTm < bestGantryTm)
                    {
                        cpAvgLeafV = wLeafTravelDist / bestGantryTm;
                        cpDsRt = (cpMU - cpMU0) / bestGantryTm;
                        this.beamTm += bestGantryTm;
                    }
                    else
                    {
                        cpAvgLeafV = wLeafTravelDist / bestMUTm;
                        cpGantryV = Math.Abs(ModCustom(cpGantryAng - cpGantryAng0 + 180, 360) - 180) / bestMUTm;
                        this.beamTm += bestMUTm;
                    }
                    this.bmCPDynLs.Add(new BeamControlPointDynamic(idxCP - 1, cpGantryV, cpAvgLeafV, cpDsRt * 60.0, cpMU - cpMU0));
                    cpLeafPos0 = cpLeafPos;
                    cpGantryAng0 = cpGantryAng;
                    cpMU0 = cpMU;
                }
            }
        }
        private static double getMaxGantrySpeed(string machineId)
        // Get the maximum gantry speed based on machine... currently hard coded //
        {
            switch (machineId)
            {
                case "Everest":
                    return 4.8;
                case "K2":
                    return 4.8;
                case "Denali":
                    return 6.0;
                case "Taos":
                    return 6.0;
                default:
                    return 0.0;
            }
        }
        private static List<LeafSpecs> getLeafWidths(string mlcModel)
        // Get the leaf y positions (mm) and widths (mm) based on the MLC model... currently only varian coded //
        {
            List<LeafSpecs> widths = new List<LeafSpecs>();
            switch (mlcModel)
            {
                case "Varian High Definition 120":
                    for (int idxLeaf = 0; idxLeaf < 14; idxLeaf++)
                        widths.Add(new LeafSpecs(-107.5 + idxLeaf * 5, 5));  // 5 mm outer leaves
                    for (int idxLeaf = 14; idxLeaf < 46; idxLeaf++)
                        widths.Add(new LeafSpecs(-38.75 + (idxLeaf - 14) * 2.5, 2.5));  // 2.5 mm inner leaves
                    for (int idxLeaf = 46; idxLeaf < 60; idxLeaf++)
                        widths.Add(new LeafSpecs(42.5 + (idxLeaf - 46) * 5, 5));  // 5 mm outer leaves
                    break;
                case "Millennium 120":
                    for (int idxLeaf = 0; idxLeaf < 10; idxLeaf++)
                        widths.Add(new LeafSpecs(-195 + idxLeaf * 10, 10));  // 10 mm outer leaves
                    for (int idxLeaf = 10; idxLeaf < 50; idxLeaf++)
                        widths.Add(new LeafSpecs(-97.5 + (idxLeaf - 10) * 5, 5));  // 5 mm inner leaves
                    for (int idxLeaf = 50; idxLeaf < 60; idxLeaf++)
                        widths.Add(new LeafSpecs(105 + (idxLeaf - 50) * 10, 10));  // 10 mm outer leaves
                    break;
                default:
                    //do nothing
                    break;
            }
            return widths;
        }
        private static double ModCustom(double a, double n)
        // Customized modulous function to compute angle difference//
        {
            return a - Math.Floor(a / n) * n;
        }
    }
    public class BeamControlPointDynamic
    // Class for beam gantry speed control point //
    {
        public int idx;
        public double cpGantryV; // gantry speed
        public double cpAvgLeafV;  // average leaf speed
        public double cpDsRt; // dose rate
        public double cpMU; // MU
        public BeamControlPointDynamic(int idx, double cpGantryV, double cpAvgLeafV, double cpDsRt, double cpMU)
        {
            this.idx = idx;
            this.cpGantryV = cpGantryV;
            this.cpAvgLeafV = cpAvgLeafV;
            this.cpDsRt = cpDsRt;
            this.cpMU = cpMU;
        }
    }
    public class BeamControlPointAperture
    // Class for beam control point //
    {
        public int idx;
        public double jawX1, jawX2, jawY1, jawY2, jawPerimeter, jawArea;
        public double avgMU;
        public double leafGaps; // sum of leaf gaps within jaw opening
        public List<ControlPointAperture> apertures;
        public BeamControlPointAperture(int idx, double jawX1, double jawX2, double jawY1, double jawY2, double avgMU)
        {
            this.idx = idx;
            this.jawX1 = jawX1;
            this.jawX2 = jawX2;
            this.jawY1 = jawY1;
            this.jawY2 = jawY2;
            this.jawPerimeter = 2 * ((jawY2 - jawY1) + (jawX2 - jawX1));
            this.jawArea = (jawY2 - jawY1) * (jawX2 - jawX1);
            this.avgMU = avgMU;
            this.leafGaps = 0;
            this.apertures = new List<ControlPointAperture>();
        }
        public void AddAperture(double perimeter, double edgeLen, double area)
        {
            this.apertures.Add(new ControlPointAperture(perimeter, edgeLen, area));
        }
    }
    public class ControlPointAperture
    // Class for an aperture within a control point //
    {
        public double perimeter;
        public double edgeLen;
        public double area;
        public ControlPointAperture(double perimeter, double edgeLen, double area)
        {
            this.perimeter = perimeter;
            this.edgeLen = edgeLen;
            this.area = area;
        }
    }
    internal class LeafSpecs
    // Class for mlc specifications //
    {
        public double yPos;
        public double width;
        public LeafSpecs(double yPos, double width)
        {
            this.yPos = yPos;
            this.width = width;
        }
    }
}
