﻿using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.DB.Structure;
using Shared;
using fi = Shared.Filter;
using ut = Shared.Util;
using tr = Shared.Transformation;
using mp = Shared.MyMepUtils;
using lad = MEPUtils.CreateInstrumentation.ListsAndDicts;
using dbg = Shared.Dbg;

namespace MEPUtils.CreateInstrumentation
{
    public class StartCreatingInstrumentation
    {
        public static Result StartCreating(ExternalCommandData commandData)
        {
            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Create Instrumentation!");

                    Pipe selectedPipe;
                    XYZ iP;

                    using (Transaction trans1 = new Transaction(doc))
                    {
                        trans1.Start("SelectPipePoint");
                        (selectedPipe, iP) = SelectPipePoint(doc, uidoc);
                        trans1.Commit();
                    }

                    string direction;

                    //Select the direction to create in
                    BaseFormTableLayoutPanel_Basic ds = new BaseFormTableLayoutPanel_Basic(lad.Directions());
                    ds.ShowDialog();
                    direction = ds.strTR;
                    //ut.InfoMsg(ds.strTR);

                    string PipeTypeName;

                    //Select type of Olet
                    BaseFormTableLayoutPanel_Basic oletSelector = new BaseFormTableLayoutPanel_Basic(lad.PipeTypeByOlet());
                    oletSelector.ShowDialog();
                    PipeTypeName = oletSelector.strTR;
                    //ut.InfoMsg(PipeTypeName);

                    PipeType pipeType = fi.GetElements<PipeType>(doc, PipeTypeName, BuiltInParameter.SYMBOL_NAME_PARAM).First();

                    FamilyInstance olet;

                    XYZ dirPoint = null;

                    using (Transaction trans2 = new Transaction(doc))
                    {
                        trans2.Start("Create Olet");

                        switch (direction)
                        {
                            case "Top":
                                dirPoint = new XYZ(iP.X, iP.Y, iP.Z + 5);
                                break;
                            case "Bottom":
                                dirPoint = new XYZ(iP.X, iP.Y, iP.Z - 5);
                                break;
                            case "Front":
                                dirPoint = new XYZ(iP.X, iP.Y - 5, iP.Z);
                                break;
                            case "Back":
                                dirPoint = new XYZ(iP.X, iP.Y + 5, iP.Z);
                                break;
                            case "Left":
                                dirPoint = new XYZ(iP.X - 5, iP.Y, iP.Z);
                                break;
                            case "Right":
                                dirPoint = new XYZ(iP.X + 5, iP.Y, iP.Z);
                                break;
                            default:
                                break;
                        }

                        dbg.PlaceAdaptiveFamilyInstance(doc, "Marker Line: Red", iP, dirPoint);

                        trans2.Commit();
                    }

                    txGp.Assimilate();
                }

                return Result.Succeeded;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                throw new Exception(ex.Message);
                return Result.Failed;
            }
        }

        private static (Pipe pipe, XYZ point) SelectPipePoint(Document doc, UIDocument uidoc)
        {
            //Select the pipe to operate on
            var selectedPipe = ut.SelectSingleElementOfType(uidoc, typeof(Pipe),
                "Select a pipe where to place a support!", false);
            //Get end connectors
            var conQuery = (from Connector c in mp.GetALLConnectorsFromElements(selectedPipe)
                            where (int)c.ConnectorType == 1
                            select c).ToList();

            Connector c1 = conQuery.First();
            Connector c2 = conQuery.Last();

            //Define a plane by three points
            //Detect if the pipe concides with X-axis
            //If true use another axis to define point
            Plane plane;

            if (ut.Compare(c1.Origin.Y, c2.Origin.Y) == 0 && ut.Compare(c1.Origin.Z, c2.Origin.Z) == 0)
                plane = Plane.CreateByThreePoints(c1.Origin, c2.Origin, new XYZ(c1.Origin.X, c1.Origin.Y + 5, c1.Origin.Z));
            else
                plane = Plane.CreateByThreePoints(c1.Origin, c2.Origin, new XYZ(c1.Origin.X + 5, c1.Origin.Y, c1.Origin.Z));

            //Set view sketch plane to the be the created plane
            var sp = SketchPlane.Create(doc, plane);
            uidoc.ActiveView.SketchPlane = sp;
            //Get a 3d point by picking a point
            XYZ point_in_3d = null;
            try { point_in_3d = uidoc.Selection.PickPoint("Please pick a point on the plane defined by the selected face"); }
            catch (OperationCanceledException) { }


            return ((Pipe)selectedPipe, point_in_3d);
        }

        private static (Pipe pipe, Element element) PlaceSupports(ExternalCommandData commandData, string name)
        {
            var app = commandData.Application;
            var uiDoc = app.ActiveUIDocument;
            var doc = uiDoc.Document;

            try
            {
                //Select a pipe
                var selectedPipe = ut.SelectSingleElementOfType(uiDoc, typeof(Pipe),
                    "Select a pipe where to place a support!", false);
                //Get end connectors
                var conQuery = (from Connector c in mp.GetALLConnectorsFromElements(selectedPipe)
                                where (int)c.ConnectorType == 1
                                select c).ToList();

                Connector c1 = conQuery.First();
                Connector c2 = conQuery.Last();

                //Define a plane by three points
                //Detect if the pipe concides with X-axis
                //If true use another axis to define point
                Plane plane;

                if (ut.Compare(c1.Origin.Y, c2.Origin.Y) == 0 && ut.Compare(c1.Origin.Z, c2.Origin.Z) == 0)
                {
                    plane = Plane.CreateByThreePoints(c1.Origin, c2.Origin, new XYZ(c1.Origin.X, c1.Origin.Y + 5, c1.Origin.Z));
                }
                else
                {
                    plane = Plane.CreateByThreePoints(c1.Origin, c2.Origin, new XYZ(c1.Origin.X + 5, c1.Origin.Y, c1.Origin.Z));
                }

                //Set view sketch plane to the be the created plane
                var sp = SketchPlane.Create(doc, plane);
                uiDoc.ActiveView.SketchPlane = sp;
                //Get a 3d point by picking a point
                XYZ point_in_3d = null;
                try
                {
                    point_in_3d = uiDoc.Selection.PickPoint(
                      "Please pick a point on the plane"
                      + " defined by the selected face");
                }
                catch (OperationCanceledException)
                {
                }

                //Get family symbol
                FilteredElementCollector collector = new FilteredElementCollector(doc);
                ElementParameterFilter filter = fi.ParameterValueFilter(name, BuiltInParameter.SYMBOL_FAMILY_AND_TYPE_NAMES_PARAM);
                LogicalOrFilter classFilter = fi.FamSymbolsAndPipeTypes();
                FamilySymbol familySymbol = (FamilySymbol)collector.WherePasses(classFilter).WherePasses(filter).FirstOrDefault();
                if (familySymbol == null) throw new Exception("No SUPPORT FamilySymbol loaded in project!");

                //The strange symbol activation thingie...
                //See: http://thebuildingcoder.typepad.com/blog/2014/08/activate-your-family-symbol-before-using-it.html
                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                    doc.Regenerate();
                }

                //Get the host pipe level
                Level level = (Level)doc.GetElement(selectedPipe.LevelId);

                //Create the support instance
                Element support = doc.Create.NewFamilyInstance(point_in_3d, familySymbol, level, StructuralType.NonStructural);

                //Get the connector from the support
                ConnectorSet connectorSetToAdd = mp.GetConnectorSet(support);
                if (connectorSetToAdd.IsEmpty)
                    throw new Exception("The support family lacks a connector. Please read the documentation for correct procedure of setting up a support element.");
                Connector connectorToConnect = (from Connector c in connectorSetToAdd select c).FirstOrDefault();

                //Rotate into place
                tr.RotateElementInPosition(point_in_3d, connectorToConnect, c1, support);

                //Set diameter
                Parameter nominalDiameter = support.LookupParameter("Nominal Diameter");
                nominalDiameter.Set(conQuery.First().Radius * 2);

                return ((Pipe)selectedPipe, support);
            }
            catch (Exception e)
            {
                throw new Exception(e.Message);
            }
        }

        /// <summary>
        /// Set the correct PipingSystemType for the placed support family.
        /// </summary>
        /// <param name="commandData">The usual ExternalCommandData.</param>
        /// <param name="pipe">The pipe on which the support was placed.</param>
        /// <param name="support">The support that was placed.</param>
        private static (Pipe dummyPipe, Connector supportConnector) SetSystemType(ExternalCommandData commandData, Pipe pipe, Element support)
        {
            var app = commandData.Application;
            var uiDoc = app.ActiveUIDocument;
            var doc = uiDoc.Document;

            //Get the pipe type from pipe
            ElementId pipeTypeId = pipe.PipeType.Id;

            //Get system type from pipe
            ConnectorSet pipeConnectors = pipe.ConnectorManager.Connectors;
            Connector pipeConnector = (from Connector c in pipeConnectors where (int)c.ConnectorType == 1 select c).FirstOrDefault();
            ElementId pipeSystemType = pipeConnector.MEPSystem.GetTypeId();

            //Get the connector from the support
            Connector connectorToConnect = (from Connector c in ((FamilyInstance)support).MEPModel.ConnectorManager.Connectors select c).FirstOrDefault();

            //Create a point in space to connect the pipe
            XYZ direction = connectorToConnect.CoordinateSystem.BasisZ.Multiply(2);
            XYZ origin = connectorToConnect.Origin;
            XYZ pointInSpace = origin.Add(direction);

            //Create the pipe
            Pipe dummyPipe = Pipe.Create(doc, pipeTypeId, pipe.ReferenceLevel.Id, connectorToConnect, pointInSpace);

            //Change the pipe system type to match the picked pipe (it is not always matching)
            dummyPipe.SetSystemType(pipeSystemType);

            return (dummyPipe, connectorToConnect);
        }
    }
}
