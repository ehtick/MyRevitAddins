﻿#region Namespaces
using System;
using System.Reflection;
using System.IO;
using System.Windows.Media.Imaging;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using Autodesk.Revit.UI;
using PlaceSupport;
using cn = ConnectConnectors.ConnectConnectors;
using tl = TotalLineLength.TotalLineLength;
using piv = PipeInsulationVisibility.PipeInsulationVisibility;
using ped = PED.InitPED;
using Shared;
//using Document = Autodesk.Revit.Creation.Document;

#endregion

namespace MyRibbonPanel
{
    [Transaction(TransactionMode.Manual)]
    class App : IExternalApplication
    {
        public const string myRibbonPanelToolTip = "My Own Ribbon Panel";

        //Method to get the button image
        BitmapImage NewBitmapImage(Assembly a, string imageName)
        {
            Stream s = a.GetManifestResourceStream(imageName);

            BitmapImage img = new BitmapImage();

            img.BeginInit();
            img.StreamSource = s;
            img.EndInit();

            return img;
        }

        // get the absolute path of this assembly
        static string ExecutingAssemblyPath = Assembly.GetExecutingAssembly().Location;
        // get ref to assembly
        Assembly exe = Assembly.GetExecutingAssembly();

        public Result OnStartup(UIControlledApplication application)
        {
            AddMenu(application);
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        private void AddMenu(UIControlledApplication application)
        {
            RibbonPanel rvtRibbonPanel = application.CreateRibbonPanel("MyRevitAddins");

            //ConnectConnectors
            PushButtonData data = new PushButtonData("ConnectConnectors", "Connect Connectors", ExecutingAssemblyPath,
                "MyRibbonPanel.ConnectConnectors");
            data.ToolTip = myRibbonPanelToolTip;
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgConnectConnectors16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgConnectConnectors32.png");
            PushButton connectCons = rvtRibbonPanel.AddItem(data) as PushButton;

            //TotalLineLengths
            data = new PushButtonData("TotalLineLengths", "Total length of lines", ExecutingAssemblyPath, "MyRibbonPanel.TotalLineLengths");
            data.ToolTip = myRibbonPanelToolTip;
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgTotalLineLength16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgTotalLineLength32.png");
            PushButton totLentgths = rvtRibbonPanel.AddItem(data) as PushButton;

            //PipeInsulationVisibility
            data = new PushButtonData("PipeInsulationVisibility", "Toggle Pipe Insulation visibility", ExecutingAssemblyPath, "MyRibbonPanel.PipeInsulationVisibility");
            data.ToolTip = myRibbonPanelToolTip;
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPipeInsulationVisibility16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPipeInsulationVisibility32.png");
            PushButton pipeInsulationVisibility = rvtRibbonPanel.AddItem(data) as PushButton;

            //PlaceSupports
            data = new PushButtonData("PlaceSupports", "Place supports", ExecutingAssemblyPath, "MyRibbonPanel.PlaceSupports");
            data.ToolTip = myRibbonPanelToolTip;
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPlaceSupport16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPlaceSupport32.png");
            PushButton placeSupports = rvtRibbonPanel.AddItem(data) as PushButton;

            //PED
            data = new PushButtonData("PED", "PED", ExecutingAssemblyPath, "MyRibbonPanel.PEDclass");
            data.ToolTip = myRibbonPanelToolTip;
            data.Image = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPED16.png");
            data.LargeImage = NewBitmapImage(exe, "MyRibbonPanel.Resources.ImgPED32.png");
            PushButton PED = rvtRibbonPanel.AddItem(data) as PushButton;
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class ConnectConnectors : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                using (Transaction trans = new Transaction(commandData.Application.ActiveUIDocument.Document))
                {
                    trans.Start("Connect the Connectors!");
                    cn.ConnectTheConnectors(commandData);
                    trans.Commit();
                }
                return Result.Succeeded;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class TotalLineLengths : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                using (Transaction trans = new Transaction(commandData.Application.ActiveUIDocument.Document))
                {
                    trans.Start("Calculate total length of selected lines!");
                    tl.TotalLineLengths(commandData);
                    trans.Commit();
                }
                return Result.Succeeded;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class PipeInsulationVisibility : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                using (Transaction trans = new Transaction(commandData.Application.ActiveUIDocument.Document))
                {
                    trans.Start("Toggle Pipe Insulation visibility!");
                    piv.TogglePipeInsulationVisibility(commandData);
                    trans.Commit();
                }
                return Result.Succeeded;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class PlaceSupports : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            try
            {
                using (TransactionGroup txGp = new TransactionGroup(doc))
                {
                    txGp.Start("Place support");

                    Pipe pipe;
                    Element support;
                    Pipe dummyPipe;
                    Connector supportConnector;

                    using (Transaction trans1 = new Transaction(doc))
                    {
                        trans1.Start("Place supports!");
                        SupportChooser sc = new SupportChooser(commandData);
                        sc.ShowDialog();
                        sc.Close();

                        (pipe, support) = PlaceSupport.PlaceSupport.PlaceSupports(commandData, "Support Symbolic: " + sc.supportName);

                        trans1.Commit();
                    }

                    using (Transaction trans2 = new Transaction(doc))
                    {
                        trans2.Start("Define correct system type for support.");
                        (dummyPipe, supportConnector) = PlaceSupport.PlaceSupport.SetSystemType(commandData, pipe, support);
                        trans2.Commit();
                    }

                    using (Transaction trans3 = new Transaction(doc))
                    {
                        trans3.Start("Disconnect pipe.");
                        Connector connectorToDisconnect = (from Connector c in dummyPipe.ConnectorManager.Connectors
                                                           where c.IsConnectedTo(supportConnector)
                                                           select c).FirstOrDefault();
                        connectorToDisconnect.DisconnectFrom(supportConnector);
                        trans3.Commit();
                    }

                    using (Transaction trans4 = new Transaction(doc))
                    {
                        trans4.Start("Divide the MEPSystem.");
                        dummyPipe.MEPSystem.DivideSystem(doc);
                        trans4.Commit();
                    }

                    using (Transaction trans5 = new Transaction(doc))
                    {
                        trans5.Start("Delete the dummy pipe.");
                        doc.Delete(dummyPipe.Id);
                        trans5.Commit();
                    }

                    txGp.Assimilate();
                }

                return Result.Succeeded;
            }

            catch (Autodesk.Revit.Exceptions.OperationCanceledException) { return Result.Cancelled; }

            catch (Exception ex)
            {
                message = ex.Message;
                return Result.Failed;
            }
        }
    }

    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    class PEDclass : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {

            UIApplication uiApp = commandData.Application;
            Document doc = commandData.Application.ActiveUIDocument.Document;
            UIDocument uidoc = uiApp.ActiveUIDocument;

            using (TransactionGroup txGp = new TransactionGroup(doc))
            {
                txGp.Start("Initialize PED data");

                using (Transaction trans1 = new Transaction(doc))
                {
                    trans1.Start("Create parameters");
                    ped ped = new ped();
                    ped.CreateElementBindings(commandData);
                    trans1.Commit();
                }

                using (Transaction trans2 = new Transaction(doc))
                {
                    trans2.Start("Populate parameters");
                    ped ped = new ped();
                    ped.PopulateParameters(commandData);
                    trans2.Commit();
                }

                txGp.Assimilate();
            }

            return Result.Succeeded;
        }
    }
}

