﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
//using MoreLinq;
using Autodesk.Revit.UI;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Plumbing;
using fi = Shared.Filter;
using ut = Shared.Util;
using op = Shared.Output;
using mp = Shared.MyMepUtils;
//using mySettings = GeneralStability.Properties.Settings;

namespace MEPUtils
{
    public class ConnectConnectors
    {
        public static void ConnectTheConnectors(ExternalCommandData commandData)
        {
            bool ctrl = false;
            if ((int)Keyboard.Modifiers == 2) ctrl = true;

            var app = commandData.Application;
            var uiDoc = app.ActiveUIDocument;
            var doc = uiDoc.Document;
            var selection = uiDoc.Selection.GetElementIds();

            if (selection.Count == 0) //If no elements selected, connect ALL connectors to ALL connectors
            {
                //Argh! It seems Revit2019 doesn't break when connecting pipes at angle!!!
                ////To filter out PCF_ELEM_EXCL set to true
                ////Collecting pipes, fittings, accessories
                ////Filtering out those with "true" value
                ////The Guid below is for PCF_ELEM_EXCL
                //var exclFilter = fi.ParameterValueGenericFilter(doc, 0, new Guid("CC8EC292-226C-4677-A32D-10B9736BFC1A"));

                //FilteredElementCollector col1 = new FilteredElementCollector(doc);
                //col1.WherePasses(
                //        new LogicalOrFilter(
                //            new List<ElementFilter>
                //            {
                //                new ElementCategoryFilter(BuiltInCategory.OST_PipeFitting),
                //                new ElementCategoryFilter(BuiltInCategory.OST_PipeAccessory),
                //                new ElementClassFilter(typeof (Pipe))
                //            }));//.WherePasses(exclFilter);
                //var col2 = mp.GetElementsOfBuiltInCategory(doc, BuiltInCategory.OST_MechanicalEquipment);

                //HashSet<Element> elements = new HashSet<Element>();
                //elements.UnionWith(col1);
                //elements.UnionWith(col2);

                var allConnectors = mp.GetALLConnectorsInDocument(doc).Where(c => !c.IsConnected).ToList();

                //Employ reverse iteration to be able to modify the collection while iterating over it
                for (int i = allConnectors.Count - 1; i > 0; i--)
                {
                    if (allConnectors.Count < 2) break;
                    Connector c1 = allConnectors[i];
                    allConnectors.RemoveAt(i);
                    if (c1.IsConnected) continue; //Need: connectors connected in this loop are still in collection
                    Connector c2 = (from Connector c in allConnectors where ut.IsEqual(c.Origin, c1.Origin) select c).FirstOrDefault();
                    c2?.ConnectTo(c1);
                }
            }

            else if (selection.Count == 1 && !ctrl) //If one and no CTRL key, connect the element
            {
                var elements = new HashSet<Element>(from ElementId id in selection select doc.GetElement(id));
                var elementConnectors = mp.GetALLConnectorsFromElements(elements);
                var allConnectors = mp.GetALLConnectorsInDocument(doc).Where(c => !c.IsConnected).ToList();

                IList<Connector> list1 = new List<Connector>();
                IList<Connector> list2 = new List<Connector>();

                foreach (var c1 in elementConnectors)
                {
                    foreach (var c2 in allConnectors)
                    {
                        if (c1.Id != c2.Id && !c1.IsConnected && ut.IsEqual(c1.Origin, c2.Origin))
                        {
                            list1.Add(c1);
                            list2.Add(c2);
                        }
                    }
                }

                if (list1.Count == 0 && list2.Count == 0) throw new Exception("No matches found! Check alignment!");

                foreach (var (c1, c2) in list1.Zip(list2, (x, y) => (c1: x, c2: y)))
                {
                    c1.ConnectTo(c2);
                }
            }

            else if ((selection.Count == 1 || selection.Count > 2) && ctrl) //If one and CTRL key is pressed, disconnect the element
            {
                var elements = new HashSet<Element>(from ElementId id in selection select doc.GetElement(id));
                var elementConnectors = mp.GetALLConnectorsFromElements(elements);

                foreach (Connector c1 in elementConnectors)
                {
                    if (c1.IsConnected)
                    {
                        var set = c1.AllRefs;
                        foreach (Connector c2 in set)
                        {
                            if (c1.IsConnectedTo(c2)) c1.DisconnectFrom(c2);
                        }
                    }
                }
            }

            //Connect or disconnect the connectors of selection
            //Only works on selection of two adjacent elements
            //That means only two connectors get connected to or disconnected from each other
            else if (selection.Count == 2)
            {
                var elements = new HashSet<Element>(from ElementId id in selection select doc.GetElement(id));
                var connectors = mp.GetALLConnectorsFromElements(elements).ToList();

                for (int i = connectors.Count - 1; i > 0; i--)
                {
                    if (connectors.Count < 2) throw new Exception("No eligible connectors found! Check alignment.");
                    Connector c1 = connectors[i];
                    connectors.RemoveAt(i);
                    Connector c2 = (from Connector c in connectors where ut.IsEqual(c.Origin, c1.Origin) select c).FirstOrDefault();
                    if (c2 != null)
                    {
                        if (c1.IsConnected) c2.DisconnectFrom(c1);
                        else c2.ConnectTo(c1);
                    }
                }

            }
            else throw new Exception("Not correct amount of elements selected for the command! Choose none, one or two!");
        }
    }
}

