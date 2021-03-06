﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using sku_to_smv.Properties;

namespace sku_to_smv
{
    public partial class options : Form
    {
        Color PrewievColor;
        Bitmap bm;
        Graphics g;
        object[] Page1;
        object[] Page2;
        object[] Page3;
        int SimulPeriod, AutosavePeriod;
        public options()
        {
            InitializeComponent();

            Page1 = new object[] { "Основной текст", "Комментарий", "Начало секции", "Сигналы"};
            Page2 = new object[] { "Текст", "Входные сигналы", "Локальные состояния", "Выходные сигналы", "Выделение"};
            Page3 = new object[] { };

            this.panel2.Enabled = false;
            this.panel2.Visible = false;
            this.panel3.Enabled = false;
            this.panel3.Visible = false;
            CancelButton = this.button2;
            bm = new Bitmap(150, 74);
            g = Graphics.FromImage(bm);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;//Включаем сглаживание графических объектов
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//Включаем сглаживание шрифтов
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;//Включаем интерполяцию
            PrewievColor = Color.Red;

            this.checkBox1.DataBindings.Add(new Binding("Checked", Settings.Default, "Autosave", false, DataSourceUpdateMode.OnPropertyChanged));
            this.textBox1.DataBindings.Add(new Binding("Text", Settings.Default, "AutosavePeriod", false, DataSourceUpdateMode.OnPropertyChanged));
            this.comboBox2.DataBindings.Add(new Binding("SelectedIndex", Settings.Default, "ToolsPanelLocation", false, DataSourceUpdateMode.OnPropertyChanged));

            this.textBox2.DataBindings.Add(new Binding("Text", Settings.Default, "SimulationPeriod", false, DataSourceUpdateMode.OnPropertyChanged));
            this.checkBox2.DataBindings.Add(new Binding("Checked", Settings.Default, "LogSimulation", false, DataSourceUpdateMode.OnPropertyChanged));
            this.LogFormatBox.DataBindings.Add(new Binding("SelectedIndex", Settings.Default, "LogFormat", false, DataSourceUpdateMode.OnPropertyChanged));
            this.TexthighLightEnable.DataBindings.Add(new Binding("Checked", Settings.Default, "TextFieldEnableHighLight", false, DataSourceUpdateMode.OnPropertyChanged));
            this.comboBox1.SelectedIndex = 0;
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            switch (e.Node.Index)
            {
                case 0:
                    this.panel1.Enabled = true;
                    this.panel1.Visible = true;
                    this.panel2.Enabled = false;
                    this.panel2.Visible = false;
                    this.panel3.Enabled = false;
                    this.panel3.Visible = false;
                    break;
                case 1:
                    this.panel2.Enabled = true;
                    this.panel2.Visible = true;
                    this.panel1.Enabled = false;
                    this.panel1.Visible = false;
                    this.panel3.Enabled = false;
                    this.panel3.Visible = false;
                    break;
                case 2:
                    this.panel3.Enabled = true;
                    this.panel3.Visible = true;
                    this.panel2.Enabled = false;
                    this.panel2.Visible = false;
                    this.panel1.Enabled = false;
                    this.panel1.Visible = false;
                    switch (comboBox1.SelectedItem as String)
                    {
                        case "Текстовый редактор":
                            break;
                        case "Область графа":
                            break;
                        case "Таблица сигналов":
                            break;
                    }
                    break;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.OK;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DialogResult = DialogResult.Cancel;
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ColorDialog colorDlg = new ColorDialog();
            switch (comboBox1.SelectedItem as String)
            {
                case "Текстовый редактор":
                    switch (listBox1.SelectedItem as String)
                    {
                        case "Основной текст":
                            colorDlg.Color = Settings.Default.TextFieldTextColor;
                            break;
                        case "Комментарий":
                            colorDlg.Color = Settings.Default.TextFieldCommentColor;
                            break;
                        case "Начало секции":
                            colorDlg.Color = Settings.Default.TextFieldOptionColor;
                            break;
                        case "Сигналы":
                            colorDlg.Color = Settings.Default.TextFieldSignalColor;
                            break;
                    }
                    break;
                case "Область графа":
                    switch (listBox1.SelectedItem as String)
                    {
                        case "Текст":
                            colorDlg.Color = Settings.Default.GrafFieldTextColor;
                            break;
                        case "Входные сигналы":
                            colorDlg.Color = System.Drawing.Color.Red;
                            break;
                        case "Локальные состояния":
                            colorDlg.Color = Settings.Default.GrafFieldLocalSignalColor;
                            break;
                        case "Выходные сигналы":
                            colorDlg.Color = Settings.Default.GrafFieldOutputSignalColor;
                            break;
                        case "Выделение":
                            colorDlg.Color = System.Drawing.Color.LightPink;
                            break;
                    }
                    break;
                case "Таблица сигналов":
                    break;
            }
            if (colorDlg.ShowDialog() == DialogResult.OK)
            {
                switch (comboBox1.SelectedItem as String)
                {
                    case "Текстовый редактор":
                        switch (listBox1.SelectedItem as String)
                        {
                            case "Основной текст":
                                 Settings.Default.TextFieldTextColor = colorDlg.Color;
                                break;
                            case "Комментарий":
                                Settings.Default.TextFieldCommentColor = colorDlg.Color;
                                break;
                            case "Начало секции":
                                Settings.Default.TextFieldOptionColor = colorDlg.Color;
                                break;
                            case "Сигналы":
                                Settings.Default.TextFieldSignalColor = colorDlg.Color;
                                break;
                        }
                        break;
                    case "Область графа":
                        switch (listBox1.SelectedItem as String)
                        {
                            case "Текст":
                                Settings.Default.GrafFieldTextColor = colorDlg.Color;
                                break;
                            case "Входные сигналы":
                                Settings.Default.GrafFieldInputSignalColor = colorDlg.Color;
                                break;
                            case "Локальные состояния":
                                Settings.Default.GrafFieldLocalSignalColor = colorDlg.Color;
                                break;
                            case "Выходные сигналы":
                                Settings.Default.GrafFieldOutputSignalColor = colorDlg.Color;
                                break;
                            case "Выделение":
                                Settings.Default.GrafFieldSygnalSelectionColor = colorDlg.Color;
                                break;
                        }
                        break;
                    case "Таблица сигналов":
                        break;
                }
            }
            PrintImage();
        }
        
        private void PrintImage()
        {
            g.Clear(Color.White);
            switch (comboBox1.SelectedItem as String)
            {
                case "Текстовый редактор":
                    switch (listBox1.SelectedItem as String)
                    {
                        case "Основной текст":
                            g.DrawString("Sample", Settings.Default.TextFieldTextFont, new SolidBrush(Settings.Default.TextFieldTextColor), new PointF(5F, 5F));
                            break;
                        case "Комментарий":
                            g.DrawString("Sample", Settings.Default.TextFieldTextFont, new SolidBrush(Settings.Default.TextFieldCommentColor), new PointF(5F, 5F));
                            break;
                        case "Начало секции":
                            g.DrawString("Sample", Settings.Default.TextFieldTextFont, new SolidBrush(Settings.Default.TextFieldOptionColor), new PointF(5F, 5F));
                            break;
                        case "Сигналы":
                            g.DrawString("Sample", Settings.Default.TextFieldTextFont, new SolidBrush(Settings.Default.TextFieldSignalColor), new PointF(5F, 5F));
                            break;
                    }
                    break;
                case "Область графа":
                    switch (listBox1.SelectedItem as String)
                    {
                        case "Текст":
                            g.DrawString("Sample", Settings.Default.GrafFieldTextFont, new SolidBrush(Settings.Default.GrafFieldTextColor), new PointF(5F, 5F));
                            break;
                        case "Входные сигналы":
                            g.DrawRectangle(new Pen(Settings.Default.GrafFieldInputSignalColor, 2), 5, 5, 60, 60);
                            break;
                        case "Локальные состояния":
                            g.DrawEllipse(new Pen(Settings.Default.GrafFieldLocalSignalColor, 2), 5, 5, 60, 60);
                            break;
                        case "Выходные сигналы":
                            g.DrawRectangle(new Pen(Settings.Default.GrafFieldOutputSignalColor, 2), 5, 5, 60, 60);
                            break;
                        case "Выделение":
                            g.DrawEllipse(new Pen(Settings.Default.GrafFieldSygnalSelectionColor, 3), 5, 5, 60, 60);
                            break;
                    }
                    break;
                case "Таблица сигналов":
                    break;
            }
            g.Flush();
            this.pictureBox1.Image = bm;
        }

        private void options_Shown(object sender, EventArgs e)
        {
            PrintImage();
            //AutosavePeriod = int.Parse(this.textBox1.Text);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (this.comboBox1.SelectedIndex)
            {
                case 0: this.listBox1.Items.Clear();
                    this.listBox1.Items.AddRange(Page1);
                    break;
                case 1: this.listBox1.Items.Clear();
                    this.listBox1.Items.AddRange(Page2);
                    break;
                case 2: this.listBox1.Items.Clear();
                    this.listBox1.Items.AddRange(Page3);
                    break;
            }
            PrintImage();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            FontDialog fontDlg = new FontDialog();
            switch (comboBox1.SelectedItem as String)
            {
                case "Текстовый редактор":
                    switch (listBox1.SelectedItem as String)
                    {
                        case "Основной текст":
                            fontDlg.Font = Settings.Default.TextFieldTextFont;
                            break;
                        case "Комментарий":

                            break;
                        case "Начало секции":
                            
                            break;
                        case "Сигналы":

                            break;
                    }
                    break;
                case "Область графа":
                    switch (listBox1.SelectedItem as String)
                    {
                        case "Текст":
                            fontDlg.Font = Settings.Default.GrafFieldTextFont;
                            break;
                        case "Входные сигналы":

                            break;
                        case "Локальные состояния":

                            break;
                        case "Выходные сигналы":
                            
                            break;
                        case "Выделение":

                            break;
                    }
                    break;
                case "Таблица сигналов":
                    break;
            }
            if (fontDlg.ShowDialog() == DialogResult.OK)
            {
                switch (comboBox1.SelectedItem as String)
                {
                    case "Текстовый редактор":
                        switch (listBox1.SelectedItem as String)
                        {
                            case "Основной текст":
                                Settings.Default.TextFieldTextFont = fontDlg.Font;
                                break;
                            case "Комментарий":

                                break;
                            case "Начало секции":
                                
                                break;
                            case "Сигналы":

                                break;
                        }
                        break;
                    case "Область графа":
                        switch (listBox1.SelectedItem as String)
                        {
                            case "Текст":
                                Settings.Default.GrafFieldTextFont = fontDlg.Font;
                                break;
                            case "Входные сигналы":

                                break;
                            case "Локальные состояния":

                                break;
                            case "Выходные сигналы":
                                
                                break;
                            case "Выделение":

                                break;
                        }
                        break;
                    case "Таблица сигналов":
                        break;
                }
            }
            PrintImage();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (listBox1.SelectedItem as String)
            {
                case "Основной текст":
                case "Текст": button3.Enabled = true;
                    break;
                default: button3.Enabled = false;
                    break;
            }
            PrintImage();
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox2.Text.Length > 0)
                {
                    if (Int32.Parse(this.textBox2.Text) < 0)
                    {
                        this.label11.Text = "Значение не может быть отрицательным";
                        this.label11.Visible = true;
                        this.button1.Enabled = false;
                    }
                    else
                    {
                        this.label11.Visible = false;
                        this.button1.Enabled = true;
                    }
                }
            }
            catch (FormatException)
            {
                this.label11.Text = "Введен недопустимый символ";
                this.label11.Visible = true;
                this.button1.Enabled = false;
            }
            catch (OverflowException)
            {
                this.label11.Text = "Введено слишком большое число";
                this.label11.Visible = true;
                this.button1.Enabled = false;
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            try
            {
                if (textBox1.Text.Length > 0)
                {
                    if (Int32.Parse(this.textBox1.Text) < 0)
                    {
                        this.label10.Text = "Значение не может быть отрицательным";
                        this.label10.Visible = true;
                        this.button1.Enabled = false;
                    }
                    else
                    {
                        this.label10.Visible = false;
                        this.button1.Enabled = true;
                    }
                }
            }
            catch (FormatException)
            {
                this.label10.Text = "Введен недопустимый символ";
                this.label10.Visible = true;
                this.button1.Enabled = false;
            }
            catch (OverflowException)
            {
                this.label10.Text = "Введено слишком большое число";
                this.label10.Visible = true;
                this.button1.Enabled = false;
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            Settings.Default.Reset();
        }
    }
}
