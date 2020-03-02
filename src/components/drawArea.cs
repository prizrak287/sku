﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Microsoft.CSharp;
using sku_to_smv.Properties;
using sku_to_smv.src;

namespace sku_to_smv
{
    public class DrawArea : PictureBox
    {

        //States
        public static readonly Pen stateDefaultPen = new Pen(Settings.Default.ColorDefaultState, 2);
        public static readonly Pen stateSelectedPen = new Pen(Settings.Default.ColorSelectedState, 3);
        public static readonly Pen stateActivePen = new Pen(Settings.Default.ColorActiveState, 4);
        public static readonly Pen stateActiveAndSelectedPen = new Pen(Settings.Default.ColorActiveState, 6);
        public static readonly Brush stateActiveBrush = new SolidBrush(Settings.Default.ColorFillActiveState);
        public static readonly Font defaultTextFont = Settings.Default.DefaultText;
        public static readonly Brush defaultTextBrush = new SolidBrush(Settings.Default.ColorDefaultTextBrush);
        //Links
        public static readonly Pen linkDefaultPen = new Pen(Settings.Default.ColorDefaultLink, 2);
        public static readonly Pen linkSelectedPen = new Pen(Settings.Default.ColorSelectedLink, 4);
        //Signals
        public static readonly Font signalDefaultFont = Settings.Default.SignalDefaultFont;
        public static readonly Font signalSelectedFont = Settings.Default.SignalSelectedFont;
        public static readonly Brush signalDefaultBrush = new SolidBrush(Settings.Default.SignalDefaultColor);
        public static readonly Brush signalActiveBrush = new SolidBrush(Settings.Default.SignalActiveColor);
        //TimeMarks
        public TimeTextBox timeTb;
        private static DrawArea _instnce;


        public static DrawArea getInstance()
        {
            if (_instnce == null)
            {
                _instnce = new DrawArea();
            }
            return _instnce;
        }

        HScrollBar hScroll;
        VScrollBar vScroll;
        ContextMenuStrip contextMenu;
        System.Windows.Forms.Timer ClickToolPanelTimer;
        
        ToolPanel tools;
        Graphics g;
        public State[] States;
        public Link[] Links;
        public Signal[] signals;
        public Time[] timeMarks;
        public Rule[] rules;
        NamedPipeServerStream pipe;
        StreamWriter sw;
        SignalTable table;
        BufferedGraphicsContext drawContext;
        BufferedGraphics drawBuffer;
        LogWriter log;

        int xT, yT/*, dx, dy*/;
        float dragOffsetX, dragOffsetY;
        Point paintDotSelectedState;
        int InputsLeight;
        bool TableCreated;
        bool b_SavingImage;
        int stepNumber;
        bool b_EnableLogging;

        public bool SimulStarted { get; set; }
        public String LogFileName { get; set; }
        private System.Windows.Forms.Timer timer1;
        private System.ComponentModel.IContainer components;
        FormClosedEventHandler handler;

        public delegate void drawAreaEventHandler(object sender, EventArgs a);
        public event drawAreaEventHandler SimulationStarted;
        public event drawAreaEventHandler SimulationStoped;

        private float scaleT; 
        
        public float ScaleT
        {
            get { return scaleT; }
            set
            {
                scaleT = value;              
            }
        }

        private DrawArea()
        {
            InitializeArea();
        }
        ~DrawArea() 
        {
            hScroll.Dispose();
            vScroll.Dispose();
            g.Dispose();
            Array.Clear(States, 0, States.Length);
            Array.Clear(Links, 0, Links.Length);
        }
        /// <summary>
        /// Функция инициализации области рисования
        /// </summary>
        private void InitializeArea()
        {
            timeTb = new TimeTextBox(this);
            timeTb.Size = new Size(50, 20);
            timeTb.TabIndex = 5;
            timeTb.Dock = DockStyle.None;
            timeTb.Visible = false;
            Controls.Add(timeTb);
            //Отображение отладочной информации на графе
            ScaleT = 1F;
            xT = 0;
            yT = 0;
            stepNumber = 1;
            InputsLeight = 0;

            dragOffsetX = 0;
            dragOffsetY = 0;
            paintDotSelectedState = Point.Empty;
            SimulStarted = false;
            TableCreated = false;
            b_SavingImage = false;
            b_EnableLogging = true;
            LogFileName = "";

            log = new LogWriter();
            drawContext = new BufferedGraphicsContext();
            handler = new System.Windows.Forms.FormClosedEventHandler(this.TableClosed);
            this.components = new System.ComponentModel.Container();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this)).BeginInit();
            this.SuspendLayout();

            Links = new Link[0];
            States = new State[0];
            signals = new Signal[0];
            timeMarks = new Time[0];
            rules = new Rule[0];

            this.DoubleBuffered = true;

            tools = ToolPanel.getInstance();
            tools.BackColor = Color.Pink;
            tools.ButtonClicked += this.ToolPanelButtonClicked;
            tools.PanelOrientation = Orientation.Vertical;
            tools.PanelAlignment = Alignment.RIGHT;

            

            tools.Buttons[1].Enabled = false;
            tools.Buttons[2].Enabled = false;
            //tools.Buttons[3].Enabled = false;
            //tools.Buttons[4].Enabled = false;

            hScroll = new HScrollBar();
            vScroll = new VScrollBar();
            contextMenu = new ContextMenuStrip(components);
            contextMenu.Visible = false;
            contextMenu.Items.Add("Установить 1");
            contextMenu.Items.Add("Всегда 1");
            contextMenu.Items.Add("Установить 0");
            contextMenu.ItemClicked += new ToolStripItemClickedEventHandler(this.contextMenuClick);

            this.Controls.Add(hScroll);
            this.Controls.Add(vScroll);
            Controls.Add(tools);

            hScroll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            hScroll.LargeChange = 50;
            hScroll.Location = new System.Drawing.Point(0, this.Height - 20);
            hScroll.Maximum = 1000;
            hScroll.Name = "hScroll";
            hScroll.Size = new System.Drawing.Size(this.Width - 20, 20);
            hScroll.TabIndex = 2;
            hScroll.Scroll += new System.Windows.Forms.ScrollEventHandler(this.hScroll_Scroll);

            vScroll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            vScroll.LargeChange = 50;
            vScroll.Location = new System.Drawing.Point(this.Width - 20, 0);
            vScroll.Maximum = 1000;
            vScroll.Name = "vScroll";
            vScroll.Size = new System.Drawing.Size(20, this.Height);
            vScroll.TabIndex = 1;
            vScroll.Scroll += new System.Windows.Forms.ScrollEventHandler(this.vScroll_Scroll);



            //графический буфер
            drawBuffer = drawContext.Allocate(this.CreateGraphics(), this.ClientRectangle);
            g = drawBuffer.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;//Включаем сглаживание графических объектов
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//Включаем сглаживание шрифтов
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;//Включаем интерполяцию

            //ApplySettings();

            this.MouseClick += new System.Windows.Forms.MouseEventHandler(this.mouseClick);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.mouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.mouseMove);
            //this.SizeChanged += new EventHandler(this.AreaResized);
                  
            this.ClickToolPanelTimer = new System.Windows.Forms.Timer();
            this.ClickToolPanelTimer.Interval = 200;
            this.ClickToolPanelTimer.Tick += new System.EventHandler(this.ClickToolPanelTimer_Tick);

            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            ((System.ComponentModel.ISupportInitialize)(this)).EndInit();
            this.ResumeLayout(false);
        }
        public void ApplySettings()
        {
            b_EnableLogging = Settings.Default.LogSimulation;
            /*if (b_EnableLogging)
                tools.Buttons[6].Visible = true;
            else tools.Buttons[6].Visible = false;*/

            if (timer1.Enabled)
            {
                timer1.Stop();
                timer1.Interval = int.Parse(Settings.Default.SimulationPeriod);
                timer1.Start();
            }
            else timer1.Interval = int.Parse(Settings.Default.SimulationPeriod);

            Refresh();
        }
        /// <summary>
        /// Закрывает именованный канал
        /// </summary>
        public void ClosePipe()
        {
            if (pipe != null)
            {
                if (pipe.IsConnected)
                {
                    WritePipe(0, 0, 'e');
                    if (pipe.IsConnected) pipe.Disconnect();
                }
                sw = null;
                pipe = null;
                GC.Collect();
            }
        }
        public override void Refresh()
        {
            base.Refresh();         
            RefreshArea(g);
            drawBuffer.Render();
            this.vScroll.Maximum = (int)(1000.0 * ScaleT);
            this.hScroll.Maximum = (int)(1000.0 * ScaleT);
        }

        private void RefreshArea(Graphics g)
        {
            g.Clear(System.Drawing.Color.White);
            drawStates(g);
            drawLinks(g);
            drawSignals(g);
            drawTimeMarks(g);
            //PaintToolPanel(g);
        }

        public void createStates()
        {
            int stateDefaultCentreX = Settings.Default.StateCentre;
            int stateDefaultCentreY = Settings.Default.StateCentre;
            int offsetStateX = Settings.Default.OffsetStateX;
            int offsetStateY = Settings.Default.OffsetStateY;
            foreach (Rule rule in this.rules)
            {
                State startState = rule.startState;
                State endState = rule.endState;
                State[] startAndEndStates = { startState, endState };
                foreach (State state in startAndEndStates)
                {
                    State s = getStateByName(state.Name);
                    if (s == null)
                    {
                        state.paintDot.X = States.Length % 2 == 0 ? stateDefaultCentreX : stateDefaultCentreX + offsetStateX;
                        state.paintDot.Y = stateDefaultCentreY + States.Length / 2 * offsetStateY;
                        addState(state);
                    }    
                }
            }
        }
        
        private void drawStates(Graphics g)
        {
            int stateDiametr = Settings.Default.StateDiametr;
            foreach (State state in States)
            {
                state.calculateLocation();
                Pen statePen; 
                if (!state.Active && !state.Selected) statePen = stateDefaultPen;
                else if (!state.Active && state.Selected) statePen = stateSelectedPen;
                else if (state.Active && !state.Selected) statePen = stateActivePen;
                else statePen = stateActiveAndSelectedPen;
                g.DrawEllipse(statePen, state.paintDot.X, state.paintDot.Y, stateDiametr, stateDiametr);
                if (state.Active) g.FillEllipse(stateActiveBrush, state.paintDot.X, state.paintDot.Y, stateDiametr, stateDiametr);             
                g.DrawString(state.Name, defaultTextFont, defaultTextBrush, state.nameDot.X, state.nameDot.Y);             
            }
        }

        private void canselStates()
        {
            States = new State[0];
        }

        public void createLinks()
        {
            foreach (Rule rule in rules)
            {
                Link link = getLinkByName(rule.startState.Name + rule.endState.Name);
                if (link == null)
                {
                    Array.Resize(ref Links, Links.Length + 1);
                    link = new Link(rule.startState, rule.endState);
                    link.setName();
                    link.Arc = link.startState.Equals(link.endState);
                    link.startState.links.Add(link);
                    link.endState.links.Add(link);
                    Links[Links.Length - 1] = link;
                }
                Array.Resize(ref rule.signal.links, rule.signal.links.Length + 1);
                rule.signal.links[rule.signal.links.Length - 1] = link;
                Array.Resize(ref link.signals, link.signals.Length + 1);
                link.signals[link.signals.Length - 1] = rule.signal;               
            }
        }

        private void drawLinks(Graphics g)
        {
            int arcRadius = Settings.Default.ArcCyrcleRadius;
            int arcStartAngle = Settings.Default.ArcStartAngle;
            int arcSweepAngle = Settings.Default.ArcSweepAngle;
            foreach (Link link in Links)
            {       
                Pen linkPen = link.Selected ? linkSelectedPen : linkDefaultPen;
                link.calculateLocation();
                if (link.Arc)
                {  
                    g.DrawArc(linkPen, new RectangleF(link.arcDot.X, link.arcDot.Y, arcRadius * 2, arcRadius * 2), arcStartAngle, arcSweepAngle);
                }
                else
                {
                    g.DrawLine(linkPen, link.startDot.X, link.startDot.Y, link.endDot.X, link.endDot.Y);
                    g.DrawLine(linkPen, link.endDot.X, link.endDot.Y, link.arrowDots[0].X, link.arrowDots[0].Y);
                    g.DrawLine(linkPen, link.endDot.X, link.endDot.Y, link.arrowDots[1].X, link.arrowDots[1].Y);
                }
            }          
        }
        private void canselLinks()
        {
            Links = new Link[0];
        }

        public void createSignals()
        {
            foreach (Rule rule in rules)
            {
                Signal s = getSignalByName(rule.signal.name);
                if (s == null)
                {
                    s = rule.signal;
                    Array.Resize(ref signals, signals.Length + 1);
                    signals[signals.Length - 1] = s;
                }
            }
        }

        public void drawSignals(Graphics g)
        {
            foreach (Signal s in signals)
            {
                s.calculateLocation();
                foreach (Point d in s.paintDots)
                {
                    Font font = (s.Selected) ? signalSelectedFont : signalDefaultFont;
                    Brush brush = (s.Active) ? signalActiveBrush: signalDefaultBrush;                                                      
                    g.DrawString(s.name, font, brush, d.X, d.Y);
                }
            }
        }

        private void canselSignals()
        {
            signals = new Signal[0];
        }

        public void createTimeMarks()
        {
            foreach (Rule r in rules)
            {
                if (r.timeMark == null) continue;
                Time timeMark = getTimeMarkByName(r.timeMark.name);
                if (timeMark == null)
                {
                    timeMark = r.timeMark;
                    Array.Resize(ref timeMarks, timeMarks.Length + 1);
                    timeMarks[timeMarks.Length - 1] = timeMark;
                }
                Link l = getLinkByName(r.startState.Name + r.endState.Name);
                if (l == null) continue;
                Array.Resize(ref timeMark.links, timeMark.links.Length + 1);
                timeMark.links[timeMark.links.Length - 1] = l;
            }
        }

        public void drawTimeMarks(Graphics g)
        {
            foreach (Time t in timeMarks)
            {
                t.calculateLocation();
                foreach (Point d in t.paintDots)
                {
                    Font font = (t.selected) ? signalSelectedFont : signalDefaultFont;
                    Brush brush = signalDefaultBrush;
                    g.DrawString(t.name + "<" + t.value + ">", font, brush, d.X, d.Y);
                }   
            }
        }
        private void canselTimeMarks()
        {
            timeMarks = new Time[0];
        }

        public void canselDrawElements()
        {
            canselStates();
            canselLinks();
            canselSignals();
            canselTimeMarks();     
        }

        public Link getLinkByName(String name)
        {
            foreach (Link link in this.Links)
            {
                if (link.name.Equals(name)) return link;
            }
            return null;
        }

        private Signal getSignalByName(String name)
        {
            foreach (Signal signal in signals)
            {
                if (signal.name.Equals(name)) return signal;
            }
            return null;
        }

        private State getStateByName(String name)
        {
            foreach (State state in States)
            {
                if (state.Name.Equals(name)) return state;
            }
            return null;
        }

        private Time getTimeMarkByName(String name)
        {
            foreach(Time t in timeMarks)
            {
                if (t.name.Equals(name)) return t;
            }
            return null;
        }

        public State getActiveState()
        {
            foreach (State s in States)
            {
                if (s.Active) return s;
            }
            return null;
        }

        public List<Rule> getRulesWithActiveState()
        {
            List<Rule> activeRules = new List<Rule>();
            State activeState = getActiveState();
            foreach(Rule r in rules)
            {
                if (r.startState.Equals(activeState)) 
                    activeRules.Add(r);
            }
            return activeRules;
        }

        private void addState(State state)
        {
            State currentState = getStateByName(state.Name);
            if (currentState == null)
            {
                Array.Resize(ref States, States.Length + 1);
                States[States.Length - 1] = state;
            }
        }

        private bool isDotOnLink(Point dot, Link link)
        {
            float x = dot.X;
            float y = dot.Y;
            if (link.Arc)
            {
                int arcRadius = Settings.Default.ArcCyrcleRadius;
                Point centreArc = new Point(link.arcDot.X + arcRadius, link.arcDot.Y + arcRadius);
                //Уравнение окружности вида (x-x.centre)^2 + (y-y.centre)^2 = r^2, представляющей часть арки
                int result = (int)(Math.Pow(x - centreArc.X, 2) + Math.Pow(y - centreArc.Y, 2));
                int squareRadius = (int)Math.Pow(arcRadius, 2);
                bool dotOnPaintedArc = !(x >= centreArc.X && y >= centreArc.Y); //Переменная отвечающая, что точка лежит на отрисованной части окружности - арке
                return result >= (squareRadius - 100) && result <= (squareRadius + 100) && dotOnPaintedArc;
            }
            else
            {
                //Каноническое уравнение прямой на плоскости типа (x-x1)/(x2-x1) = (y-y1)/(y2-y1)
                float result = (link.startDot.Y - link.endDot.Y) * x + (link.endDot.X - link.startDot.X) * y + (link.startDot.X * link.endDot.Y - link.endDot.X * link.startDot.Y);
                int offset = 10;
                float maxX = Math.Max(link.startDot.X, link.endDot.X) + offset;
                float minX = Math.Min(link.startDot.X, link.endDot.X) - offset;
                float maxY = Math.Max(link.startDot.Y, link.endDot.Y) + offset;
                float minY = Math.Min(link.startDot.Y, link.endDot.Y) - offset;
                return (result >= -2500
                    && result <= 2500
                    && x >= minX && x <= maxX
                    && y >= minY && y <= maxY);
            }       
        }

        public bool isDotOnState(Point dot, State state)
        {
            int diametr = Settings.Default.StateDiametr;
            return dot.X >= state.paintDot.X
                && dot.X <= state.paintDot.X + diametr
                && dot.Y >= state.paintDot.Y
                && dot.Y <= state.paintDot.Y + diametr;
        }

        public bool isDotOnSignal(Point dot, Signal signal)
        {
            float offset = 20;
            bool isOnSignal = false;
            foreach (Point pDot in signal.paintDots)
            {
                isOnSignal = isOnSignal || (dot.X >= pDot.X
                     && dot.X <= pDot.X + offset
                     && dot.Y >= pDot.Y
                     && dot.Y <= pDot.Y + offset);
            }
            return isOnSignal;
        }

        private bool isDotOnTimeMark(Point dot, Time tm)
        {
            int offset = 10 * tm.name.Length;
            bool isOnMark = false;
            foreach (Point pd in tm.paintDots)
            {
                isOnMark = isOnMark 
                    || dot.X >= pd.X
                    && dot.X <= pd.X + offset
                    && dot.Y >= pd.Y
                    && dot.Y <= pd.Y + offset;
            }
            return isOnMark;
        }

        private bool setSelectedLink(Point dot)
        {
            bool isChanged = false;
            foreach (Link link in Links)
            {
                bool isLink = isDotOnLink(dot, link);
                isChanged = isChanged || link.Selected ^ isLink;
                link.Selected = isLink;          
            }
            return isChanged;
        }

        private bool setSelectedState(Point dot)
        {
            bool isChanged = false;
            foreach (State state in States)
            {
                bool isState = isDotOnState(dot, state);
                isChanged = isChanged || state.Selected ^ isState;          
                state.Selected = isState;               
            }
            return isChanged;
        }

        private bool setActiveState()
        {
            bool isChanged = false;
            foreach (State s in States)
            {
                bool isNeedChangeActivity = s.Selected && paintDotSelectedState.Equals(s.paintDot);
                isChanged = isChanged || isNeedChangeActivity;
                if (isNeedChangeActivity)
                {
                    s.Active = !s.Active;
                    canselStates(s);
                }
            }
            return isChanged;
        }

        private void canselStates(State state)
        {
            foreach (State s in States)
            {
                s.Active = s.Equals(state) && s.Active;
            }
        }

        private bool setSelectedTimeMarks(Point dot)
        {
            bool isChanged = false;
            foreach (Time tm in timeMarks)
            {
                bool isOnMark = isDotOnTimeMark(dot, tm);
                isChanged = isChanged || tm.selected ^ isOnMark;
                tm.selected = isOnMark;
            }
            return isChanged;
        }

        private bool setActiveSignal()
        {
            bool isChanged = false;
            foreach (Signal s in signals)
            {
                bool newActivityStatus = s.Selected && !s.Active || !s.Selected && s.Active;
                isChanged = isChanged || s.Active ^ newActivityStatus;
                s.Active = newActivityStatus;
            }
            return isChanged;
        }

        private bool setSelectedSignal(Point dot)
        {
            bool isSignalsChanged = false;
            foreach (Signal signal in signals)
            {
                bool isOnSignal = isDotOnSignal(dot, signal);
                isSignalsChanged = isSignalsChanged || signal.Selected ^ isOnSignal;
                signal.Selected = isOnSignal;
            }
            return isSignalsChanged;
        }

        private bool setValueTimeMark(Point dot)
        {
            bool tbVisible = false;
            Point tbLocation = Point.Empty;
            Time selectedTm = null;
            foreach (Time tm in timeMarks)
            {
                tbVisible = tbVisible || tm.selected;
                if (tm.selected)
                {
                    tbLocation = dot;
                    selectedTm = tm;
                }
            }
            timeTb.Visible = tbVisible;
            timeTb.Location = tbLocation;
            timeTb.timeMark = selectedTm;
            return true;
        }

        private bool isElementsChanged(Point dot)
        {
            bool isLinkChanged = setSelectedLink(dot);
            bool isStateChanged = setSelectedState(dot);
            bool isSignalChanged = setSelectedSignal(dot);
            bool isTimeMarkChanged = setSelectedTimeMarks(dot);
            return isLinkChanged
                || isStateChanged
                || isSignalChanged
                || isTimeMarkChanged;
        }    

        private void relocationStates(Point dot)
        {
            foreach (State state in States)
            {
                if (state.Selected)
                {
                    state.paintDot.X = dot.X - (int)dragOffsetX;
                    state.paintDot.Y = dot.Y - (int)dragOffsetY;
                    foreach (Link l in state.links)
                    {
                        l.calculateLocation();
                        if (l.lengthLink < 0 && !l.Arc)
                        {
                            int direction = state.Equals(l.startState) ? -1 : 1;
                            state.paintDot.X += (int)(l.cosx * Math.Abs(l.lengthLink) * direction);
                            state.paintDot.Y += (int)(l.sinx * Math.Abs(l.lengthLink) * direction);
                        }
                    }
                }
            }
        }

        private void mouseMove(object sender, MouseEventArgs e)
        {
            Point dot = e.Location;
            if (e.Button.Equals(MouseButtons.Left))
            {
                relocationStates(dot);
            }
            if (isElementsChanged(dot) || e.Button.Equals(MouseButtons.Left))
            {
                Refresh();
            }
        }

        private void mouseDown(object sender, MouseEventArgs e)
        {
            foreach (State state in States)
            {
                if (state.Selected)
                {
                    dragOffsetX = e.X - state.paintDot.X;
                    dragOffsetY = e.Y - state.paintDot.Y;
                    paintDotSelectedState = new Point(state.paintDot.X, state.paintDot.Y);
                }
            }
        }

        private void mouseClick(object sender, MouseEventArgs e)
        {
            dragOffsetX = 0;
            dragOffsetY = 0;
            bool isStateChangeActivity = !SimulStarted ? setActiveState() : false;
            bool isSignalChangeActivity = setActiveSignal();
            bool s = setValueTimeMark(e.Location);
            if (isStateChangeActivity || isSignalChangeActivity || s) Refresh(); 
        }
       
        private void PaintToolPanel(Graphics g)
        {

            //Отрисовка панели инструментов
            if (!b_SavingImage)
            {
                if (tools.PanelOrientation == Orientation.Vertical)
                    if (tools.PanelAlignment == Alignment.LEFT)
                    {
                        tools.Size = new Size(40, this.Size.Height);
                        tools.Location = new Point(0, 0);
                    }
                    else
                    {
                        tools.Size = new Size(40, this.Size.Height);
                        tools.Location = new Point(this.Size.Width - 40 - vScroll.Size.Width, 0);
                    }
                else
                {
                    if (tools.PanelAlignment == Alignment.TOP)
                    {
                        tools.Size = new Size(this.Size.Width, 40);
                        tools.Location = new Point(0, 0);
                    }
                    else
                    {
                        tools.Size = new Size(this.Size.Width, 40);
                        tools.Location = new Point(0, this.Size.Height - 40 - hScroll.Size.Height);
                    }
                }
                //tools.UpdateControlsLocation();
                //tools.Draw(g);
            }
        }

        private void AreaResized(object sender, EventArgs e)
        {
            g.Dispose();
            drawBuffer.Dispose();
            drawBuffer = drawContext.Allocate(this.CreateGraphics(), this.ClientRectangle);
            g = drawBuffer.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;//Включаем сглаживание графических объектов
            g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//Включаем сглаживание шрифтов
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;//Включаем интерполяцию
            Refresh();
        }
        /// <summary>
        /// Обработчик горизонтального скролинга
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void hScroll_Scroll(object sender, ScrollEventArgs e)
        {
            xT = -hScroll.Value;
            Refresh();
        }
        /// <summary>
        /// Обработчик вертикального скролинга
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void vScroll_Scroll(object sender, ScrollEventArgs e)
        {
            yT = -vScroll.Value;
            Refresh();
        }

        public void ToolPanelButtonClicked(object sender, ToolButtonEventArgs e)
        {
            this.ClickToolPanelTimer.Start();
            switch (e.Name)
            {
                case "start":
                    //CreateSimul((this.Parent.Parent.Parent as Form1).parser.Rules, (this.Parent.Parent.Parent as Form1).parser.Outputs);
                    break;
                case "run":
                    SimulStart();
                    break;
                case "step":
                    SimulStep(true);
                    break;
                case "stop":
                    SimulStop();
                    break;
                case "table":
                    CreateTable();
                    break;
                case "reset":
                    ResetAllsignals();
                    break;
                case "showlog":
                    ShowLog();
                    break;
            }

        }
        private void ClickToolPanelTimer_Tick(object sender, EventArgs e)
        {
            this.OnMouseClick(null);
            this.ClickToolPanelTimer.Stop();
            this.Refresh();
        }

        private void OnSimulStarted()
        {
            drawAreaEventHandler handler = SimulationStarted;
            if (handler != null)
                handler(this, new EventArgs());
        }

        private void OnSimiulStoped()
        {
            drawAreaEventHandler handler = SimulationStoped;
            if (handler != null)
                handler(this, new EventArgs());
        }
        
        private void WritePipe(int num, int b, char ch, int step = 0)
        {
            try
            {
                if (pipe != null && pipe.IsConnected)
                {
                    if (ch == 's')
                    {
                        sw.WriteLine("set " + num.ToString() + " " + b.ToString());
                    }
                    if (ch == 't')
                    {
                        sw.WriteLine("step " + step);
                    }
                    if (ch == 'e')
                    {
                        sw.WriteLine("exit");
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine("ERROR: {0}", e.Message);
            }

        }
       
        public void SimulStart()
        {
            OnSimulStarted();
            if (b_EnableLogging)
            {
                log.LogFormat = Settings.Default.LogFormat;
                if (LogFileName.Length > 0)
                {
                    log.FileName = LogFileName;
                }
                else log.FileName = "log_" + States.GetHashCode().ToString();
                log.StartLog(true, false, null);
            }

            tools.Buttons[1].Enabled = false;
            tools.Buttons[2].Enabled = false;
            tools.Buttons[3].Enabled = true;

            if (table != null)
            {
                table.UpdateSimControls(new bool[] {true, false, true});
                table.ResetSteps();
                if (TableCreated)
                {
                    for (int i = 0; i < InputsLeight; i++)
                    {
                        switch (table.GetElementByNumber(i))
                        {
                           /* case returnResults.rFALSE: States[i].Signaled = States[i].Signaled || false;
                                break;
                            case returnResults.rTRUE: States[i].Signaled = States[i].Signaled || true;
                                break;*/
                            case returnResults.rUNDEF: break;
                        }
                    }
                }
                //Refresh();
            }
            for (int i = 0; i < States.Length; i++ )
            {
                log.StartLog(false, false, States[i].Name);
            }
            log.StartLog(false, true, null);
            this.timer1.Start();
        }
        /// <summary>
        /// Останов автоматической симуляции
        /// </summary>
        public void SimulStop()
        {
            if (b_EnableLogging && this.timer1.Enabled)
                log.EndLog();
            if (table != null)
                table.UpdateSimControls(new bool[] { true, true, false });
            this.timer1.Stop();
            OnSimiulStoped();
            tools.Buttons[1].Enabled = true;
            tools.Buttons[2].Enabled = true;
            tools.Buttons[3].Enabled = false;
        }
        /// <summary>
        /// Шаг симуляции
        /// </summary>
        public void SimulStep(bool b_Manual = false)
        {
            if (b_Manual)
            {
                if (TableCreated)
                {
                    for (int i = 0; i < InputsLeight; i++)
                    {
                        switch (table.GetElementByNumber(i))
                        {
                            /*case returnResults.rFALSE: States[i].Signaled = States[i].Signaled || false;
                                break;
                            case returnResults.rTRUE: States[i].Signaled = States[i].Signaled || true;
                                break;*/
                            case returnResults.rUNDEF: break;
                        }
                    }
                }
                Refresh();
                System.Threading.Thread.Sleep(500);
            }
            bool StepStart = true;

            for (int i = 0; i < States.Length; i++)
            {
                if (b_EnableLogging /*&& States[i].InputSignal == true*/)
                {
                    //log.AddToLog(States[i].Name, States[i].Signaled || States[i].AlSignaled, States[i].Type == STATE_TYPE.INPUT, States[i].Type == STATE_TYPE.OUTPUT, StepStart);
                    StepStart = false;
                }
               // WritePipe(i, Convert.ToInt16((States[i].Signaled || States[i].AlSignaled)), 's');
            }
            WritePipe(0, 0, 't', stepNumber);
            stepNumber++;
            //StepStart = true;
            for (int i = 0; i < States.Length; i++)
            {
                    //States[i].Signaled = ReadPipe(i);
//                 if (b_EnableLogging && States[i].InputSignal != true)
//                 {
//                     log.AddToLog(States[i].Name, States[i].Signaled || States[i].AlSignaled, false, StepStart);
//                     StepStart = false;
//                 }
            }
            if (TableCreated)
            {
                table.NextStep();
            }
            if (TableCreated)
            {
                for (int i = 0; i < InputsLeight; i++)
                {
                    switch (table.GetElementByNumber(i))
                    {
                       /* case returnResults.rFALSE: States[i].Signaled = States[i].Signaled || false;
                            break;
                        case returnResults.rTRUE: States[i].Signaled = States[i].Signaled || true;
                            break;*/
                        case returnResults.rUNDEF: break;
                    }
                }
            }
            Refresh();
        }
        /// <summary>
        /// Обработчик такта таймера
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void timer1_Tick(object sender, EventArgs e)
        {
            SimulStep();
        }
        /// <summary>
        /// Обработчик клика по выпадающему меню
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void contextMenuClick(object sender, ToolStripItemClickedEventArgs  e)
        {
            switch (e.ClickedItem.Text)
            {
                /*case "Установить 1": States[SelectedState].Signaled = true; 
                    break;
                case "Всегда 1": States[SelectedState].AlSignaled = true;
                    break;
                case "Установить 0": States[SelectedState].Signaled = false;
                    States[SelectedState].AlSignaled = false; 
                    break;*/
            }
            Refresh();
        }
        /// <summary>
        /// Создает таблицу сигналов и отображает ее
        /// </summary>
        public void CreateTable()
        {
            if (InputsLeight != 0 && !TableCreated)
            {
                if (table != null)
                {
                    this.table.FormClosed -= handler;
                }
                table = null;
                GC.Collect();
                table = new SignalTable(InputsLeight, this);
                this.table.FormClosed += handler;
                for (int i = 0; i < InputsLeight; i++)
                {
                   // table.AddElement(i, States[i].Name, States[i].Signaled, States[i].InputSignal);
                }
                table.ShowT();
                TableCreated = true;
            }
        }
        /// <summary>
        /// Обработчик закрытия таблицы сигналов
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TableClosed(object sender, FormClosedEventArgs e)
        {
            TableCreated = false;
        }
        public void SaveImage(String Path)
        {
            int maxX = 0;
            int maxY = 0;
            int tempX, tempY;
            float tempScale;
            for (int i = 0; i < States.Length; i++ )
            {
                if (States[i].paintDot.X > maxX) maxX = (int) States[i].paintDot.X;
                if (States[i].paintDot.Y > maxY) maxY = (int) States[i].paintDot.Y;
            }
            Bitmap imageB = new Bitmap(maxX + 70, maxY + 70);
            Graphics graf = Graphics.FromImage(imageB);
            graf.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;//Включаем сглаживание графических объектов
            graf.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//Включаем сглаживание шрифтов
            graf.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;//Включаем интерполяцию
            tempX = xT;
            tempY = yT;
            tempScale = ScaleT;
            xT = 0;
            yT = 0;
            ScaleT = 1F;
            b_SavingImage = true;
            //Refresh(graf);
            b_SavingImage = false;
            xT = tempX;
            yT = tempY;
            ScaleT = tempScale;
            imageB.Save(Path);
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;//Включаем сглаживание графических объектов
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;//Включаем сглаживание шрифтов
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;//Включаем интерполяцию
            RefreshArea(e.Graphics);
        }
        private void ResetAllsignals()
        {
            for (int i = 0; i < States.Length; i++ )
            {
              //  States[i].Signaled = false;
                //States[i].AlSignaled = false;
            }
        }

        private void ShowLog()
        {
            Process pr;
            ProcessStartInfo prInf = new ProcessStartInfo();
            prInf.FileName = log.SavedFileName;
            if(log.FileName != null && log.FileName.Length > 0)
                pr = Process.Start(prInf);
            //prInf.Arguments = log.FileName;
            /*if ((prInf.Arguments = log.FileName) == null)
            {
                if (LogFileName.Length > 0)
                {
                    prInf.Arguments = LogFileName + ".log";
                }
                else prInf.Arguments = "log" + States.GetHashCode().ToString() + ".log";
            }
            prInf.FileName = "notepad.exe";
            Process pr = Process.Start(prInf);*/
        }
        public void ClearArea()
        {
            if (SimulStarted)
            {
                //CreateSimul(null, null);
            }
            Array.Resize(ref Links, 0);
            Array.Resize(ref States, 0);
            GC.Collect();
        }
    } 
}
