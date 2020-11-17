﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NLog;

namespace MEPUtils.ModelessForms.SearchAndSelect
{
    public partial class EditGroupingForm : Form
    {
        //Log
        private static readonly NLog.Logger log = NLog.LogManager.GetCurrentClassLogger();

        BindingList<ParameterTypeGroup> ListToBindParametersType = new BindingList<ParameterTypeGroup>();
        public Grouping Grouping;

        public EditGroupingForm(HashSet<ParameterImpression> allParametersList)
        {
            InitializeComponent();

            //Log
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration(
                "G:\\Github\\shtirlitsDva\\MyRevitAddins\\MyRevitAddins\\SetTagsModeless\\NLog.config");

            BindingList<ParameterImpression> BuiltInParameters = new BindingList<ParameterImpression>
                (allParametersList.Where(x => x.IsBuiltIn == true).OrderBy(x => x.Name).ToList());
            BindingList<ParameterImpression> SharedParameters = new BindingList<ParameterImpression>
                (allParametersList.Where(x => x.IsShared).OrderBy(x => x.Name).ToList());
            ListToBindParametersType.Add(new ParameterTypeGroup("Built In Parameter", BuiltInParameters));
            ListToBindParametersType.Add(new ParameterTypeGroup("Shared Parameter", SharedParameters));

            comboBox1.DisplayMember = "Name";
            comboBox1.ValueMember = "ParameterList";
            comboBox1.DataSource = new BindingSource { DataSource = ListToBindParametersType };

            comboBox2.DisplayMember = "Name";
            comboBox2.ValueMember = null;
            comboBox2.DataSource = new BindingSource { DataSource = (BindingList<ParameterImpression>)comboBox1.SelectedValue };
            
            comboBox1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;
        }

        private void addRowMethod(object sender, EventArgs e)
        {
            int rowIndex = tableLayoutPanel1.GetRow((Button)sender);

            //Take the last row and create same one at end of collection
            RowStyle temp = tableLayoutPanel1.RowStyles[tableLayoutPanel1.RowCount - 1];
            tableLayoutPanel1.RowCount++;
            tableLayoutPanel1.RowStyles.Add(new RowStyle(temp.SizeType, temp.Height));

            //Take the previous row and change style
            temp = tableLayoutPanel1.RowStyles[0];
            tableLayoutPanel1.RowStyles[tableLayoutPanel1.RowCount - 2] = new RowStyle(temp.SizeType, temp.Height);

            if (tableLayoutPanel1.RowCount - rowIndex != 3) //= 3 means the control is the last and need not moved
            {
                //Move controls down
                for (int i = tableLayoutPanel1.RowCount - 3; i > rowIndex; i--)
                {
                    for (int j = 0; j < tableLayoutPanel1.ColumnCount; j++)
                    {
                        var control = tableLayoutPanel1.GetControlFromPosition(j, i);
                        if (control != null)
                        {
                            tableLayoutPanel1.SetRow(control, i + 1);
                        }
                    }
                }
            }

            #region Add controls
            //Add controls
            ComboBox cb1 = new ComboBox();
            cb1.Dock = DockStyle.Fill;
            cb1.Anchor = (AnchorStyles)15;
            cb1.DropDownStyle = ComboBoxStyle.DropDownList;
            cb1.DisplayMember = "Name";
            cb1.ValueMember = "ParameterList";
            cb1.DataSource = new BindingSource { DataSource = ListToBindParametersType };
            cb1.SelectedValue = ListToBindParametersType[0];
            cb1.SelectedIndexChanged += comboBox1_SelectedIndexChanged;
            tableLayoutPanel1.Controls.Add(cb1, 0, rowIndex + 1);

            ComboBox cb2 = new ComboBox();
            cb2.Dock = DockStyle.Fill;
            cb2.Anchor = (AnchorStyles)15;
            cb2.DropDownStyle = ComboBoxStyle.DropDownList;
            tableLayoutPanel1.Controls.Add(cb2, 1, rowIndex + 1);

            //Initialize data for second combo box by firing the associated method of the event
            cb2.DisplayMember = "Name";
            cb2.ValueMember = null;
            comboBox1_SelectedIndexChanged(cb1, new EventArgs());
            cb2.SelectedIndexChanged += comboBox2_SelectedIndexChanged;

            //Add buttons
            Button button = new Button() { Dock = DockStyle.Fill, Text = "+" };
            button.Anchor = (AnchorStyles)15;
            button.Click += addRowMethod;

            tableLayoutPanel1.Controls.Add(button, 2, rowIndex + 1);

            button = new Button() { Dock = DockStyle.Fill, Text = "-" };
            button.Anchor = (AnchorStyles)15;
            button.Click += removeRowMethod;

            tableLayoutPanel1.Controls.Add(button, 3, rowIndex + 1);
            #endregion
        }

        private void removeRowMethod(object sender, EventArgs e)
        {
            int rowIndex = tableLayoutPanel1.GetRow((Button)sender);

            if (rowIndex >= tableLayoutPanel1.RowCount)
            {
                return;
            }

            // delete all controls of row that we want to delete
            for (int i = 0; i < tableLayoutPanel1.ColumnCount; i++)
            {
                var control = tableLayoutPanel1.GetControlFromPosition(i, rowIndex);
                tableLayoutPanel1.Controls.Remove(control);
            }

            // move up row controls that comes after row we want to remove
            for (int i = rowIndex + 1; i < tableLayoutPanel1.RowCount; i++)
            {
                for (int j = 0; j < tableLayoutPanel1.ColumnCount; j++)
                {
                    var control = tableLayoutPanel1.GetControlFromPosition(j, i);
                    if (control != null)
                    {
                        tableLayoutPanel1.SetRow(control, i - 1);
                    }
                }
            }

            var removeStyle = tableLayoutPanel1.RowCount - 1;

            if (tableLayoutPanel1.RowStyles.Count > removeStyle)
                tableLayoutPanel1.RowStyles.RemoveAt(removeStyle);

            tableLayoutPanel1.RowCount--;
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            BindingList<ParameterImpression> item =
                (BindingList<ParameterImpression>)(sender as ComboBox).SelectedValue;
            int rowIndex = tableLayoutPanel1.GetRow((ComboBox)sender);
            ComboBox cb2 = (ComboBox)tableLayoutPanel1.GetControlFromPosition(1, rowIndex);

            if (item != null)
            {
                cb2.DataSource = null;
                cb2.DisplayMember = "Name";
                cb2.DataSource = new BindingSource { DataSource = item };
            }
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            List<ParameterImpression> list = new List<ParameterImpression>(tableLayoutPanel1.RowCount);
            for (int i = 0; i < tableLayoutPanel1.RowCount - 1; i++)
            {
                int col = 1;
                ComboBox cb = (ComboBox)tableLayoutPanel1.GetControlFromPosition(col, i);
                list.Add(cb.SelectedValue as ParameterImpression);
            }
            Grouping = new Grouping(list);
        }

        private void EditGroupingForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            comboBox2_SelectedIndexChanged(new ComboBox(), new EventArgs());
            GroupingSettings gs = new GroupingSettings();
            gs.GroupingSetting = Grouping;
            gs.GroupingSetting.Test = "Test String, does it get saved?";
            gs.Save();
        }
    }
}
