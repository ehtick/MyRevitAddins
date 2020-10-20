﻿using System;
using System.Collections.Generic;
using System.Linq;
using MoreLinq;
using System.Data;

namespace MEPUtils.DrawingListManager
{
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
                MetadataName = "DWGSCALE"; ColumnName = "Scale"; ExcelColumnIdx = 3;
            }
            public Scale(string value) : this() { Value = value; }
        }
        public class Date : Field
        {
            public Date()
            {
                MetadataName = "DWGDATE"; ColumnName = "Date"; ExcelColumnIdx = 4;
            }
            public Date(string value) : this() { Value = value; }
        }
        public class RevisionDate : Field
        {
            public RevisionDate()
            {
                MetadataName = "DWGREVDATE"; ColumnName = "Rev. date"; ExcelColumnIdx = 6;
            }
            public RevisionDate(string value) : this() { Value = value; }
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
            public Source_FileName(string number, string title, string fileNameFormat, string revision)
            {
                Number = new Field.Number(number);
                Title = new Field.Title(title);
                Revision = new Field.Revision(revision);
                FileNameFormat = new Field.FileNameFormat(fileNameFormat);
            }
        }

        public class Source_Excel : DrwgProps
        {
            public Source_Excel(
                string number, string title, string revision, string scale, string date, string revisionDate)
            {
                Number = new Field.Number(number);
                Title = new Field.Title(title);
                Scale = new Field.Scale(scale);
                Date = new Field.Date(date);
                Revision = new Field.Revision(revision);
                RevisionDate = new Field.RevisionDate(revisionDate);
            }
        }

        public class Source_Meta : DrwgProps
        {
            public Source_Meta() { }
            public Source_Meta(
                string number, string title, string revision, string scale, string date, string revDate)
            {
                Number = new Field.Number(number);
                Title = new Field.Title(title);
                Scale = new Field.Scale(scale);
                Date = new Field.Date(date);
                Revision = new Field.Revision(revision);
                RevisionDate = new Field.RevisionDate(revDate);
            }
        }
    }
}