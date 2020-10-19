﻿using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.IO;
using System.Text.RegularExpressions;
using System.Data;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows.Forms;
using MsgBox = System.Windows.Forms.MessageBox;
using Microsoft.Office.Interop.Excel;
using DataTable = System.Data.DataTable;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace MEPUtils.DrawingListManager
{
    static class DrawingListManager
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);
            System.Windows.Forms.Application.Run(new DrawingListManagerForm());
        }
    }

    public class DrwgLstMan
    {
        //Fields for filename analysis
        internal List<string> drwgFileNameList;
        internal List<Drwg> drwgList = new List<Drwg>();
        public DataTable FileNameData;
        internal Field.Fields fs = new Field.Fields();

        //Fields for Excel data analysis
        private DataSet ExcelDataSet;

        //Fields for Metadata data
        private DataTable MetadataData;

        internal void EnumeratePdfFiles(string path)
        {
            drwgFileNameList = Directory.EnumerateFiles(path, "*.pdf", SearchOption.TopDirectoryOnly).ToList();
        }
        internal void ScanRescanFilesAndList(string path)
        {
            if (drwgFileNameList.Count < 1 || drwgFileNameList == null)
            {
                drwgFileNameList = Directory.EnumerateFiles(path, "*.pdf", SearchOption.TopDirectoryOnly).ToList();
                if (drwgFileNameList.Count < 1 || drwgFileNameList == null)
                {
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show("No valid files found at specified location!", "Error!", buttons);
                }
            }
        }
        /// <summary>
        /// Builds the DataTable to store the data about the pdf files
        /// </summary>
        internal void PopulateDrwgDataFromFileName()
        {
            foreach (string fileNameWithPath in drwgFileNameList)
            {
                Drwg drwg = new Drwg(fileNameWithPath);
                drwgList.Add(drwg);
                drwg.ReadDrwgDataFromFileName();
            }
        }
        internal void BuildFileNameDataTable()
        {
            FileNameData = new DataTable("DrwgFileNameData");

            #region DataTable Definition
            DataColumn column;

            column = new DataColumn();
            column.DataType = typeof(bool);
            column.ColumnName = fs._Select.ColumnName;
            FileNameData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Number.ColumnName;
            FileNameData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Title.ColumnName;
            FileNameData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._FileNameFormat.ColumnName;
            FileNameData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Scale.ColumnName;
            FileNameData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Date.ColumnName;
            FileNameData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Revision.ColumnName;
            FileNameData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._RevisionDate.ColumnName;
            FileNameData.Columns.Add(column);

            //Debug column showing the state of drwg
            column = new DataColumn("State");
            column.DataType = typeof(int);
            FileNameData.Columns.Add(column);
            #endregion
        }
        internal void PopulateFileNameDataTable()
        {

            foreach (Drwg drwg in drwgList)
            {
                DataRow row = FileNameData.NewRow();

                row[fs._Number.ColumnName] = drwg.DataFromFileName.Number.Value;
                row[fs._Title.ColumnName] = drwg.DataFromFileName.Title.Value;
                row[fs._FileNameFormat.ColumnName] = drwg.DataFromFileName.FileNameFormat.Value;
                row[fs._Revision.ColumnName] = drwg.DataFromFileName.Revision.Value;

                FileNameData.Rows.Add(row);
            }
            FileNameData.AcceptChanges();
        }

        internal void ScanExcelFile(string pathToDwgList)
        {
            //Fields for Excel Interop
            Microsoft.Office.Interop.Excel.Workbook wb;
            Microsoft.Office.Interop.Excel.Sheets wss;
            Microsoft.Office.Interop.Excel.Worksheet ws;
            Microsoft.Office.Interop.Excel.Application oXL;
            object misVal = System.Reflection.Missing.Value;
            oXL = new Microsoft.Office.Interop.Excel.Application();
            oXL.Visible = true;
            oXL.DisplayAlerts = false;
            wb = oXL.Workbooks.Open(pathToDwgList, 0, false, 5, "", "", false,
                Microsoft.Office.Interop.Excel.XlPlatform.xlWindows, "", true, false, 0, false, false,
                Microsoft.Office.Interop.Excel.XlCorruptLoad.xlNormalLoad);

            wss = wb.Worksheets;
            ws = (Microsoft.Office.Interop.Excel.Worksheet)wss.Item[1];

            Range usedRange = ws.UsedRange;
            int usedRows = usedRange.Rows.Count;
            int usedCols = usedRange.Columns.Count;
            int rowStartIdx = 0;
            //Detect first row of the drawingslist
            //Assumes that Drawing data starts with a field containing string "Tegningsnr." -- subject to change
            string firstColumnValue = "Tegningsnr."; //<-- Here be the string that triggers the start of data.
            for (int row = 1; row < usedRows + 1; row++)
            {
                var cellValue = (string)(ws.Cells[row, 1] as Range).Value;
                if (cellValue == firstColumnValue)
                { rowStartIdx = row; break; }
            }

            if (rowStartIdx == 0)
            {
                MsgBox.Show($"Excel file did not find a cell in the first column\n" +
                            $"containing the first column keyword: {firstColumnValue}");
                return;
            }

            //int lastColIdx = fs.GetAllFields().MaxBy(x => x.ExcelColumnIdx).Select(x => x.ExcelColumnIdx).FirstOrDefault();
            //Range drwgUsedRange = oXL.Range[ws.Cells[rowStartIdx, 1], ws.Cells[usedRows, lastColIdx]];

            //myRange.Select();
            //MsgBox.Show(myRange.Address);

            #region BuildDataTableFromExcel
            ExcelDataSet = new DataSet("ExcelDrwgData");
            DataTable table = null;

            //Main loop creating DataTables for DataSet
            for (int i = rowStartIdx; i < usedRows + 1; i++)
            {
                //Detect start of the table
                var cellValue = (string)(ws.Cells[i, 1] as Range).Value;
                if (cellValue == firstColumnValue) //Header row detected
                {
                    //Add previously made dataTable to dataSet except at the first iteration
                    if (i != rowStartIdx) ExcelDataSet.Tables.Add(table);

                    //Get the name of DataTable
                    string nameOfDt = (string)(ws.Cells[i, 2] as Range).Value;
                    table = new DataTable(nameOfDt);

                    //Add the header values to the column names
                    DataColumn column;
                    for (int j = 1; j < usedCols + 1; j++)
                    {
                        cellValue = (string)(ws.Cells[i, j] as Range).Value;
                        column = new DataColumn();
                        column.DataType = typeof(string);
                        column.ColumnName = fs.GetExcelColumnField(j).ColumnName;
                        table.Columns.Add(column);
                        //MsgBox.Show(cellValue);
                    }
                }
                else
                {
                    DataRow row = table.NewRow();

                    for (int j = 1; j < usedCols + 1; j++)
                    {
                        string value;
                        var cellValueRaw = (ws.Cells[i, j] as Range).Value;
                        if (cellValueRaw == null) value = "";
                        else if (cellValueRaw is string) value = (string)cellValueRaw;
                        else { value = cellValueRaw.ToString(); }
                        row[j - 1] = value;
                    }
                    table.Rows.Add(row);
                }
            }
            //Add last made data table to data set.
            ExcelDataSet.Tables.Add(table);
            ExcelDataSet.AcceptChanges();
            #endregion

            wb.Close(true, misVal, misVal);
            oXL.Quit();
        }

        internal void PopulateDrwgDataFromExcel()
        {
            foreach (Drwg drwg in drwgList)
            {
                var foundRows = GetRowsBySelectQuery(drwg, ExcelDataSet);

                if (foundRows.Count > 1) throw new Exception($"Drwg {drwg.FileName} matched multiple" +
                                                             $"items in excel! This is unhandled yet, clear up before" +
                                                             $"restarting.");
                if (foundRows.Count < 1) continue;
                foreach (DataRow row in foundRows)
                {
                    drwg.DrwgNumberFromExcel = row.Field<string>(fs._Number.ColumnName);
                    drwg.DrwgTitleFromExcel = row.Field<string>(fs._Title.ColumnName);
                    drwg.DrwgRevFromExcel = row.Field<string>(fs._Revision.ColumnName);
                    drwg.DrwgScaleFromExcel = row.Field<string>(fs._Scale.ColumnName);
                    drwg.DrwgDateFromExcel = row.Field<string>(fs._Date.ColumnName);
                    drwg.DrwgRevDateFromExcel = row.Field<string>(fs._RevisionDate.ColumnName);
                }
            }
        }
        private HashSet<DataRow> GetRowsBySelectQuery<T>(Drwg drwg, T Data)
        {
            HashSet<DataRow> foundRows = new HashSet<DataRow>();

            //Build expression to use Table.Select
            string number = drwg.DrwgNumberFromFileName;
            string title = drwg.DrwgTitleFromFileName.Replace("'", "''");
            string rev = drwg.DrwgRevFromFileName;
            List<string> exprlist = new List<string>();
            exprlist.Add($"[{fs._Number.ColumnName}] = '{number}'");
            //exprlist.Add($"[{fs._Title.ColumnName}] = '{title}'"); <-- Too many problems with non-matching titles
            if (!rev.IsNullOrEmpty()) exprlist.Add($"[{fs._Revision.ColumnName}] = '{rev}'");
            string expr = string.Join(" AND ", exprlist);

            if (Data is DataSet dataSet)
            {
                foreach (DataTable table in dataSet.Tables)
                {
                    DataRow[] result = table.Select(expr);
                    HashSet<DataRow> newSet = new HashSet<DataRow>(result);
                    foundRows.UnionWith(newSet);
                }
            }
            else if (Data is DataTable table)
            {
                DataRow[] result = table.Select(expr);
                HashSet<DataRow> newSet = new HashSet<DataRow>(result);
                foundRows.UnionWith(newSet);
            }
            return foundRows;
        }
        internal void ReadMetadataData(string path)
        {
            if (drwgFileNameList.Count < 1 || drwgFileNameList == null)
            {
                drwgFileNameList = Directory.EnumerateFiles(path, "*.pdf", SearchOption.TopDirectoryOnly).ToList();
                if (drwgFileNameList.Count < 1 || drwgFileNameList == null)
                {
                    MessageBoxButtons buttons = MessageBoxButtons.OK;
                    MessageBox.Show("No valid files found at specified location!", "Error!", buttons);
                }
            }

            MetadataData = new DataTable("MetadataData");

            #region DataTable Definition
            DataColumn column;

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Number.ColumnName;
            MetadataData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Title.ColumnName;
            MetadataData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Scale.ColumnName;
            MetadataData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Date.ColumnName;
            MetadataData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._Revision.ColumnName;
            MetadataData.Columns.Add(column);

            column = new DataColumn();
            column.DataType = typeof(string);
            column.ColumnName = fs._RevisionDate.ColumnName;
            MetadataData.Columns.Add(column);
            #endregion

            HashSet<Field> fields = new HashSet<Field>(fs.GetAllFields().Where(x => x.IsMetaDataField));

            foreach (string filePath in drwgFileNameList)
            {
                PdfDocument document = null;
                try { document = PdfReader.Open(filePath); }
                catch (Exception) { continue; }

                var props = document.Info.Elements;

                if (fields.Any(x => props.ContainsKey("/" + x.MetadataName)))
                {
                    DataRow row = MetadataData.NewRow();
                    foreach (Field field in fields)
                    {
                        if (props.ContainsKey("/" + field.MetadataName))
                        {
                            string s = props["/" + field.MetadataName].ToString();
                            //Substring removes leading and closing parantheses
                            row[field.ColumnName] = s.Substring(1, s.Length - 2);
                        }
                    }
                    MetadataData.Rows.Add(row);
                }
                document.Close();
            }
            MetadataData.AcceptChanges();
        }
        internal void PopulateDrwgDataFromMetadata()
        {
            foreach (Drwg drwg in drwgList)
            {
                var foundRows = GetRowsBySelectQuery(drwg, MetadataData);

                if (foundRows.Count > 1) throw new Exception($"Drwg {drwg.FileName} matched multiple" +
                                                             $"items in excel! This is unhandled yet, clear up before" +
                                                             $"restarting.");
                if (foundRows.Count < 1) continue;
                foreach (DataRow row in foundRows)
                {
                    drwg.DrwgNumberFromMeta = row.Field<string>(fs._Number.ColumnName);
                    drwg.DrwgTitleFromMeta = row.Field<string>(fs._Title.ColumnName);
                    drwg.DrwgRevFromMeta = row.Field<string>(fs._Revision.ColumnName);
                    drwg.DrwgScaleFromMeta = row.Field<string>(fs._Scale.ColumnName);
                    drwg.DrwgDateFromMeta = row.Field<string>(fs._Date.ColumnName);
                    drwg.DrwgRevDateFromMeta = row.Field<string>(fs._RevisionDate.ColumnName);
                }
            }
        }
        internal void AddStateToGridView(Drwg drwg, DataTable data)
        {
            HashSet<DataRow> foundRows = new HashSet<DataRow>();

            //Build expression to use Table.Select
            string number = drwg.DrwgNumberFromFileName;
            string title = drwg.DrwgTitleFromFileName.Replace("'", "''");
            string rev = drwg.DrwgRevFromFileName;
            List<string> exprlist = new List<string>();
            exprlist.Add($"[{fs._Number.ColumnName}] = '{number}'");
            exprlist.Add($"[{fs._Title.ColumnName}] = '{title}'");
            if (!rev.IsNullOrEmpty()) exprlist.Add($"[{fs._Revision.ColumnName}] = '{rev}'");
            string expr = string.Join(" AND ", exprlist);
            DataRow[] result = data.Select(expr);
            HashSet<DataRow> newSet = new HashSet<DataRow>(result);
            foundRows.UnionWith(newSet);

            foreach (DataRow row in foundRows) row["State"] = (int)drwg.State;
        }

    }

    public class Drwg
    {
        #region Fields
        string FileNameWithPath;
        internal string FileName;

        FileNameFormat Format;
        DrwgNamingFormat Dnf;
        Field.Fields Fields;

        internal DrwgProps.Source_FileName DataFromFileName;

        //internal string DrwgNumberFromFileName = string.Empty;
        internal string DrwgNumberFromExcel = string.Empty;
        internal string DrwgNumberFromMeta = string.Empty;

        //internal string DrwgTitleFromFileName = string.Empty;
        internal string DrwgTitleFromExcel = string.Empty;
        internal string DrwgTitleFromMeta = string.Empty;

        //internal string DrwgRevFromFileName = string.Empty;
        internal string DrwgRevFromExcel = string.Empty;
        internal string DrwgRevFromMeta = string.Empty;

        internal string DrwgScaleFromExcel = string.Empty;
        internal string DrwgScaleFromMeta = string.Empty;

        internal string DrwgDateFromExcel = string.Empty;
        internal string DrwgDateFromMeta = string.Empty;

        internal string DrwgRevDateFromExcel = string.Empty;
        internal string DrwgRevDateFromMeta = string.Empty;

        internal StateFlags State;

        //public string DrwgFileNameFormat = string.Empty;
        #endregion

        internal List<DrwgNamingFormat> NamingFormats;

        public Drwg(string fileNameWithPath)
        {
            Fields = new Field.Fields();
            NamingFormats = new DrwgNamingFormat().GetDrwgNamingFormatListExceptOther();

            FileNameWithPath = fileNameWithPath;
            FileName = Path.GetFileName(FileNameWithPath);

            //Test to see if there are multiple matches for filenames -> meaning multiple regex matches -> should be only one match
            if (TestFormatsForMultipleMatches(FileName) > 1) throw
                      new Exception($"Filename {FileName} matched multiple Regex patterns! Must only match one!");

            //Analyze file name and find the format
            Format = DetermineFormat(FileName);

            //Find the correct analyzing format
            if (Format == FileNameFormat.Other) Dnf = new DrwgNamingFormat.Other();
            else Dnf = NamingFormats.Where(x => x.Format == Format).FirstOrDefault();
        }
        private int TestFormatsForMultipleMatches(string fileName)
        {
            int count = 0;
            foreach (DrwgNamingFormat dnf in NamingFormats)
            {
                if (dnf.TestFormat(fileName)) count++;
            }
            return count;
        }
        private FileNameFormat DetermineFormat(string fileName)
            => NamingFormats.Where(x => x.TestFormat(fileName)).Select(x => x.Format).FirstOrDefault();
        internal void ReadDrwgDataFromFileName()
        {
            if (Dnf.Format == FileNameFormat.Other)
            {
                DrwgTitleFromFileName = Path.GetFileNameWithoutExtension(FileNameWithPath); //this one is not passed as argument
            }
            else
            {
                Match match = Dnf.Regex.Match(FileName);
                string number = match.Groups[Fields._Number.RegexName].Value ?? "";
                string title = match.Groups[Fields._Title.RegexName].Value ?? "";
                string revision = match.Groups[Fields._Revision.RegexName].Value ?? "";
                DataFromFileName = new DrwgProps.Source_FileName(
                    number, title, Dnf.DrwgFileNameFormatDescription, revision);
            }
        }
        internal void CalculateState()
        {
            StateFlags Calc(string testValue, StateFlags flag)
            {
                if (!testValue.IsNullOrEmpty()) return flag;
                else return StateFlags.None;
            }
            StateFlags temp = 0;
            temp |= Calc(DrwgNumberFromFileName, StateFlags.NumberFromFileName);
            temp |= Calc(DrwgNumberFromExcel, StateFlags.NumberFromExcel);
            temp |= Calc(DrwgNumberFromMeta, StateFlags.NumberFromMeta);
            temp |= Calc(DrwgTitleFromFileName, StateFlags.TitleFromFileName);
            temp |= Calc(DrwgTitleFromExcel, StateFlags.TitleFromExcel);
            temp |= Calc(DrwgTitleFromMeta, StateFlags.TitleFromMeta);
            temp |= Calc(DrwgRevFromFileName, StateFlags.RevFromFileName);
            temp |= Calc(DrwgRevFromExcel, StateFlags.RevFromExcel);
            temp |= Calc(DrwgRevFromMeta, StateFlags.RevFromMeta);
            temp |= Calc(DrwgScaleFromExcel, StateFlags.ScaleFromExcel);
            temp |= Calc(DrwgScaleFromMeta, StateFlags.ScaleFromMeta);
            temp |= Calc(DrwgDateFromExcel, StateFlags.DateFromExcel);
            temp |= Calc(DrwgDateFromMeta, StateFlags.DateFromMeta);
            temp |= Calc(DrwgRevDateFromExcel, StateFlags.RevDateFromExcel);
            temp |= Calc(DrwgRevDateFromMeta, StateFlags.RevDateFromMeta);
            State = temp;
        }

        internal void ActOnState()
        {
            throw new NotImplementedException();
        }

        [Flags]
        internal enum StateFlags
        {
            None = 0,
            NumberFromFileName = 1,
            NumberFromExcel = 2,
            NumberFromMeta = 4,
            TitleFromFileName = 8,
            TitleFromExcel = 16,
            TitleFromMeta = 32,
            RevFromFileName = 64,
            RevFromExcel = 128,
            RevFromMeta = 256,
            ScaleFromExcel = 512,
            ScaleFromMeta = 1024,
            DateFromExcel = 2048,
            DateFromMeta = 4096,
            RevDateFromExcel = 8192,
            RevDateFromMeta = 16384
        }

        internal interface IAction
        {
            void Act();
        }

        internal class StateActions
        {
            internal StateFlags AcceptedStates { get; private set; } = StateFlags.None;

            internal class AllDataPresent : StateActions, IAction
            {
                AllDataPresent()
                {
                    AcceptedStates |= (StateFlags)32767;
                }

                public void Act()
                {
                    throw new NotImplementedException();
                }
            }
        }
    }


    public class DrwgNamingFormat
    {
        public FileNameFormat Format { get; private set; }
        public Regex Regex { get; private set; }
        public string DrwgFileNameFormatDescription { get; private set; }
        public bool TestFormat(string fileName) => this.Regex.IsMatch(fileName);

        /// <summary>
        /// "Other" is an exception and should be handled separately.
        /// </summary>
        public class Other : DrwgNamingFormat
        {
            public Other()
            {
                Format = FileNameFormat.Other;
                Regex = null;
                DrwgFileNameFormatDescription = "Andet";
            }
        }

        public class VeksNoRevision : DrwgNamingFormat
        {
            public VeksNoRevision()
            {
                Format = FileNameFormat.VeksNoRevision;
                Regex = new Regex(@"(?<number>\d{3}-\d{2}-\p{L}{3}\d-\d{3})\s-\s(?<title>[\p{L}0-9 -]*)(?<extension>.[\p{L}0-9 -]*)");
                DrwgFileNameFormatDescription = "VEKS U. REV";
            }
        }

        public class VeksWithRevision : DrwgNamingFormat
        {
            public VeksWithRevision()
            {
                Format = FileNameFormat.VeksWithRevision;
                Regex = new Regex(@"(?<number>\d{3}-\d{2}-\p{L}{3}\d-\d{3})(?:-)(?<revision>[\p{L}0-9]+)\s-\s(?<title>[\p{L}0-9 -]*)(?<extension>.[\p{L}0-9 -]*)");
                DrwgFileNameFormatDescription = "VEKS M. REV";
            }
        }

        public class DRI_BygNoRevision : DrwgNamingFormat
        {
            public DRI_BygNoRevision()
            {
                Format = FileNameFormat.DRI_BygNoRevision;
                Regex = new Regex(@"(?<number>\d{3}-\d{4}-BYG\d{2})\s-\s(?<title>[\p{L}0-9 -]*)(?<extension>.[\p{L}0-9 -]*)");
                DrwgFileNameFormatDescription = "DRI BYG U. REV";
            }
        }

        public class DRI_BygWithRevision : DrwgNamingFormat
        {
            public DRI_BygWithRevision()
            {
                Format = FileNameFormat.DRI_BygWithRevision;
                Regex = new Regex(@"(?<number>\d{3}-\d{4}-BYG\d{2})(?:-)(?<revision>[\p{L}0-9]+)\s-\s(?<title>[\p{L}0-9 -]*)(?<extension>.[\p{L}0-9 -]*)");
                DrwgFileNameFormatDescription = "DRI BYG M. REV";
            }
        }

        public class STD_NoRevision : DrwgNamingFormat
        {
            public STD_NoRevision()
            {
                Format = FileNameFormat.STD_NoRevision;
                Regex = new Regex(@"(?<number>STD-\d{3}-\d{3})\s-\s(?<title>[\p{L}0-9 ,'-]*)(?<extension>.[\p{L}0-9 -]*)");
                DrwgFileNameFormatDescription = "DRI STD U. REV";
            }
        }

        public class STD_WithRevision : DrwgNamingFormat
        {
            public STD_WithRevision()
            {
                Format = FileNameFormat.STD_WithRevision;
                Regex = new Regex(@"(?<number>STD-\d{3}-\d{3})(?:-)(?<revision>[\p{L}0-9]+)\s-\s(?<title>[\p{L}0-9 ,'-]*)(?<extension>.[\p{L}0-9 -]*)");
                DrwgFileNameFormatDescription = "DRI STD M. REV";
            }
        }

        public List<DrwgNamingFormat> GetDrwgNamingFormatListExceptOther()
        {
            List<DrwgNamingFormat> list = new List<DrwgNamingFormat>();

            //Field is the base class from which are subclasses are derived
            var dnfType = typeof(DrwgNamingFormat);
            //We also need the "Fields" type because it is also a subclas of Field, but should not be in the list
            var otherType = typeof(DrwgNamingFormat.Other);

            var subFieldTypes = dnfType.Assembly.DefinedTypes
                .Where(x => dnfType.IsAssignableFrom(x) && x != dnfType && x != otherType)
                .ToList();

            foreach (var field in subFieldTypes)
                list.Add((DrwgNamingFormat)Activator.CreateInstance(field));

            return list;
        }
    }
    public enum FileNameFormat
    {
        Other,
        VeksNoRevision,
        VeksWithRevision,
        DRI_BygNoRevision,
        DRI_BygWithRevision,
        STD_NoRevision,
        STD_WithRevision
    }
    public class Field
    {
        public string RegexName { get; private set; } = "";
        public string MetadataName { get; private set; } = "";
        public string ColumnName { get; private set; } = "";
        public bool IsExcelField { get { return ExcelColumnIdx > 0; } }
        public int ExcelColumnIdx { get; private set; } = 0;
        public bool IsMetaDataField { get { return !MetadataName.IsNullOrEmpty(); } }
        public string Value { get; private set; }
        public class Number : Field
        {
            public Number()
            {
                RegexName = "number"; MetadataName = "DWGNUMBER";
                ColumnName = "Drwg Nr."; ExcelColumnIdx = 1;
            }
            public Number(string value) : this() { Value = value; }
        }
        public class Title : Field
        {
            public Title()
            {
                RegexName = "title"; MetadataName = "DWGTITLE";
                ColumnName = "Drwg Title"; ExcelColumnIdx = 2;
            }
            public Title(string value) : this() { Value = value; }
        }
        public class Revision : Field
        {
            public Revision()
            {
                RegexName = "revision"; MetadataName = "DWGREVINDEX";
                ColumnName = "Rev. idx"; ExcelColumnIdx = 5;
            }
            public Revision(string value) : this() { Value = value; }
        }
        public class Extension : Field
        {
            public Extension() { RegexName = "extension"; }
        }
        public class Scale : Field
        {
            public Scale()
            {
                MetadataName = "DWGSCALE";
                ColumnName = "Scale"; ExcelColumnIdx = 3;
            }
        }
        public class Date : Field
        {
            public Date()
            {
                MetadataName = "DWGDATE";
                ColumnName = "Date"; ExcelColumnIdx = 4;
            }
        }
        public class RevisionDate : Field
        {
            public RevisionDate()
            {
                MetadataName = "DWGREVDATE";
                ColumnName = "Rev. date"; ExcelColumnIdx = 6;
            }
        }
        public class Selected : Field
        {
            public Selected()
            {
                ColumnName = "Select";
            }
        }
        public class FileNameFormat : Field
        {
            public FileNameFormat()
            {
                ColumnName = "File name format";
            }
            public FileNameFormat(string value) : this() { Value = value; }
        }
        public class Fields
        {
            //Remember to add new "Field"s here!
            public Field _Number = new Number();
            public Field _Title = new Title();
            public Field _Revision = new Revision();
            public Field _Extension = new Extension();
            public Field _Scale = new Scale();
            public Field _Date = new Date();
            public Field _RevisionDate = new RevisionDate();
            public Field _FileNameFormat = new FileNameFormat();
            public Field _Select = new Selected();

            public HashSet<Field> GetAllFields()
            {
                HashSet<Field> FieldsCollection = new HashSet<Field>();

                //Field is the base class from which are subclasses are derived
                var fieldType = typeof(Field);
                //We also need the "Fields" type because it is also a subclas of Field, but should not be in the list
                var fieldsType = typeof(Fields);

                var subFieldTypes = fieldType.Assembly.DefinedTypes
                    .Where(x => fieldType.IsAssignableFrom(x) && x != fieldType && x != fieldsType)
                    .ToList();

                foreach (var field in subFieldTypes)
                    FieldsCollection.Add((Field)Activator.CreateInstance(field));

                return FieldsCollection;
            }
            /// <summary>
            /// Returns the correct Field for the specified columnindex in the excel.
            /// </summary>
            /// <param name="colIdx">Index of column.</param>
            public Field GetExcelColumnField(int colIdx) =>
                new Fields().GetAllFields().Where(x => x.ExcelColumnIdx == colIdx).FirstOrDefault();

        }
    }

    public class DrwgProps
    {
        public Field.Number Number { get; private set; }
        public Field.Title Title { get; private set; }
        public Field.Revision Revision { get; private set; }
        public Field.Scale Scale { get; private set; }
        public Field.Date Date { get; private set; }
        public Field.RevisionDate RevisionDate { get; private set; }
        public Field.FileNameFormat FileNameFormat { get; private set; }
        public class Source_FileName : DrwgProps
        {
            public Source_FileName(string NumberValue, string TitleValue, string FileNameFormatValue, string RevisionValue)
            {
                Number = new Field.Number(NumberValue);
                Title = new Field.Title(TitleValue);
                Revision = new Field.Revision(RevisionValue);
                FileNameFormat = new Field.FileNameFormat(FileNameFormatValue);
            }
        }

        public class Source_Excel : DrwgProps
        {

        }

        public class Source_Meta : DrwgProps
        {

        }
    }
}