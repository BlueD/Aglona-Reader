﻿using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;


[assembly: CLSCompliant(true)]
namespace AglonaReader
{
    
    public partial class MainForm : Form
    {

        bool newBook;

        AppSettings appSettings;

        byte opState;
        
        


        private void pTC_MouseWheel(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            if (e.Delta > 0)
                ProcessKeyUp();
            else if (e.Delta < 0)
                ProcessKeyDown();
        }


        public MainForm()
        {
            InitializeComponent();

            this.pTC.MouseWheel += new MouseEventHandler(this.pTC_MouseWheel);

            opState = 0;
            this.WindowState = FormWindowState.Maximized;

            
        }
        
        private void MainForm_Resize(object sender, EventArgs e)
        {
            pTC.ResizeBufferedGraphic();
            pTC.SetSplitterPositionByRatio();
            Recompute();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {

            appSettings = Properties.Settings.Default.AppSettings;

            if (appSettings == null)
                appSettings = new AppSettings();


            pTC.HighlightFragments = appSettings.HighlightFragments;
            pTC.HighlightFirstWords = appSettings.HighlightFirstWords;
            pTC.Brightness = appSettings.Brightness;

            if (appSettings.FileUsages.Count > 0)
            {
                LoadSettingsFromFileUsageInfo(appSettings.FileUsages[0], true);
                newBook = false;
            }
            else
            {
                pTC.SetSplitterPositionByRatio(0.5F);
                newBook = true;                
            }

            highlightFramgentsToolStripMenuItem.Checked = appSettings.HighlightFragments;
            highlightFirstWordsToolStripMenuItem.Checked = appSettings.HighlightFirstWords;
        }

        private void LoadSettingsFromFileUsageInfo(FileUsageInfo f, bool load)
        {
            
            pTC.Reversed = f.Reversed;
            reverseToolStripMenuItem.Checked = pTC.Reversed;
            pTC.SetSplitterPositionByRatio(f.SplitterRatio);

            if (load)
                pTC.PText.Load(f.FileName);
            pTC.Modified = false;

            if (pTC.Number > 0)
            {
                if (f.Pair >= pTC.Number)
                    pTC.HighlightedPair = pTC.Number - 1;
                else
                    pTC.HighlightedPair = f.Pair;

                if (f.TopPair >= pTC.Number)
                    pTC.CurrentPair = pTC.Number - 1;
                else
                    pTC.CurrentPair = f.TopPair;

                pTC.FindNaturalDividers(0);
                Recompute();
            }
        }

        private bool XonSplitter(int x)
        {
            return (x >= pTC.SplitterPosition && x < pTC.SplitterPosition + pTC.SplitterWidth);
        }




        private void parallelTextControl_MouseMove(object sender, MouseEventArgs e)
        {

            if (e.X == pTC.LastMouseX && e.Y == pTC.LastMouseY)
                return;

            pTC.LastMouseX = e.X;
            pTC.LastMouseY = e.Y;

            if (XonSplitter(pTC.LastMouseX) || opState == 1)
                Cursor = Cursors.VSplit;
            else
                Cursor = Cursors.Default;

            if (opState == 1)
            {
                // Move splitter

                int newSplitterPosition = pTC.LastMouseX - pTC.SplitterMoveOffset;

                if (newSplitterPosition != pTC.SplitterPosition)
                {
                    
                    pTC.SplitterPosition = newSplitterPosition;
                    pTC.SetSplitterRatioByPosition();
                    
                    Recompute();
                }
            }

            else if (opState == 0)
                pTC.ProcessMousePosition(false);

        }

        
        private void parallelTextControl_MouseLeave(object sender, EventArgs e)
        {
            Cursor = Cursors.Default;
            if (pTC.MouseCurrentWord != null)
            {
                pTC.MouseCurrentWord = null;
                pTC.Render();
            }
        }

        private void parallelTextControl_MouseDown(object sender, MouseEventArgs e)
        {
            pTC.MousePressed = true;


            if (opState == 0)
            {
                if (XonSplitter(e.X))
                {
                    opState = 1;
                    pTC.SplitterMoveOffset = e.X - pTC.SplitterPosition;
                }

            }
        }

        private void parallelTextControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (!pTC.MousePressed)
                return;

            pTC.MousePressed = false;


            if (opState == 1)
                opState = 0;
            else
            {
                if (pTC.MouseCurrentWord != null && pTC.MouseCurrentWord.Next != null)
                {

                    if (pTC.MouseCurrentWord.Side == 1)
                    {
                        pTC.NaturalDividerPosition1W = pTC.MouseCurrentWord.Next;
                        pTC.NaturalDividerPosition1 = pTC.NaturalDividerPosition1W.Pos;
                        pTC.SetNippingFrameByScreenWord(1, pTC.NaturalDividerPosition1W);
                        (pTC.NippingFrame.F1 as Frame).FramePen = pTC.CorrectedPen;
                        pTC.Side1Set = true;
                    }
                    else
                    {
                        pTC.NaturalDividerPosition2W = pTC.MouseCurrentWord.Next;
                        pTC.NaturalDividerPosition2 = pTC.NaturalDividerPosition2W.Pos;
                        pTC.SetNippingFrameByScreenWord(2, pTC.NaturalDividerPosition2W);
                        (pTC.NippingFrame.F2 as Frame).FramePen = pTC.CorrectedPen;
                        pTC.Side2Set = true;
                    }

                    if (pTC.Side1Set && pTC.Side2Set)
                        pTC.NipHighlightedPair();
                    else
                        pTC.Render();

                }
                else if (pTC.mouse_text_word != null)
                {
                    // Set focus to this pair
                    pTC.HighlightedPair = pTC.mouse_text_word.PairIndex;

                    TextPair p = pTC[pTC.HighlightedPair];

                    if ((p.RenderedInfo1.Line2 == -1
                        || p.RenderedInfo1.Line2 >= pTC.LastFullScreenLine
                        || p.RenderedInfo2.Line2 == -1
                        || p.RenderedInfo2.Line2 >= pTC.LastFullScreenLine)
                        && !pTC[pTC.HighlightedPair].IsBig()
                        || p.RenderedInfo1.Line1 == -1
                        || p.RenderedInfo2.Line1 == -1)
                    {
                        pTC.CurrentPair = pTC.HighlightedPair;
                        pTC.PrepareScreen();
                        pTC.RenderPairs();
                    }

                    pTC.FindNaturalDividers(0);
                    pTC.FindNaturalDividersScreen(0);
                    pTC.ProcessMousePosition(true);
                    pTC.Render();
                }


            }
        }

        private void reverseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pTC.Reversed = reverseToolStripMenuItem.Checked;
            pTC.SetSplitterPositionByRatio(1 - pTC.SplitterRatio);
            pTC.ComputeSideCoordinates();
            Recompute();
        }

        private void Recompute()
        {
            pTC.mouse_text_line = -1;
            pTC.MouseCurrentWord = null;
            pTC.ProcessLayoutChange();
        }

        private void pTC_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyData == Keys.Down)
                ProcessKeyDown();

            else if (e.KeyData == Keys.Up)
                ProcessKeyUp();

            else if (e.KeyData == (Keys.Control | Keys.Up))
            {
                if (pTC.HighlightedPair > 0)
                {
                    if (pTC.CurrentPair == pTC.HighlightedPair)
                        pTC.CurrentPair--;

                    pTC.MergePairs(pTC.HighlightedPair - 1);
                    pTC.HighlightedPair--;
                    pTC.PairChanged(pTC.HighlightedPair);
                }
            }

            else if (e.KeyData == (Keys.Control | Keys.Right))
                ChangeNatural(1, true);

            else if (e.KeyData == (Keys.Control | Keys.Left))
                ChangeNatural(1, false);

            else if (e.KeyData == (Keys.Alt| Keys.Right))
                ChangeNatural(2, true);

            else if (e.KeyData == (Keys.Alt| Keys.Left))
                ChangeNatural(2, false);

            else if (e.KeyData == Keys.Right)
                ChangeNatural(0, true);

            else if (e.KeyData == Keys.Left)
                ChangeNatural(0, false);

            else if (e.KeyData == (Keys.Control | Keys.End))
            {
                int n = pTC.Number;
                if (n == 0)
                    return;
                if (pTC.HighlightedPair != n - 1)
                    GotoPair(n - 1);

            }

            else if (e.KeyData == (Keys.Control | Keys.Home))
            {
                int n = pTC.Number;
                if (n == 0)
                    return;

                GotoPair(0);
            }

            else if (e.KeyData == (Keys.Control | Keys.OemOpenBrackets))
            {
                if (pTC.Brightness > 0.6)
                {
                    pTC.Brightness -= 0.005;
                    pTC.RenderPairs();
                    pTC.Render();
                }
            }

            else if (e.KeyData == (Keys.Control | Keys.OemCloseBrackets))
            {
                if (pTC.Brightness < 0.995)
                {
                    pTC.Brightness += 0.005;
                    pTC.RenderPairs();
                    pTC.Render();
                }
            }

            else if (e.KeyData == Keys.PageUp)
                ProcessPageUp();
            else if (e.KeyData == Keys.PageDown)
                ProcessPageDown();
            
        }

        private void ProcessPageDown()
        {
            if (pTC.LastRenderedPair != pTC.CurrentPair)
                GotoPair(pTC.CurrentPair);
        }

        private void ProcessPageUp()
        {

            int newCurrentPair = pTC.CurrentPair;
            int accLines = 0; // Accumulated lines

            TextPair processedPair;

            while (newCurrentPair > 0)
            {
                newCurrentPair--;
                processedPair = pTC[newCurrentPair];

                if (!(processedPair.AllLinesComputed1 && processedPair.AllLinesComputed2))
                    pTC.PrepareScreen(newCurrentPair, -1);

                accLines += processedPair.Height;

                if (accLines > pTC.LastFullScreenLine)
                {
                    newCurrentPair++;
                    break;
                }
            }

            if (newCurrentPair == pTC.CurrentPair
                && newCurrentPair > 0)
                newCurrentPair--;

            if (newCurrentPair != pTC.CurrentPair)
                GotoPair(newCurrentPair);

        }

        private void GotoPair(int newCurrentPair)
        {
            pTC.CurrentPair = newCurrentPair;
            pTC.HighlightedPair = newCurrentPair;

            pTC.UpdateScreen();
        }

        private void ProcessKeyUp()
        {
            if (pTC.HighlightedPair == 0)
                return;

            TextPair prev_p = pTC[pTC.HighlightedPair];

            pTC.HighlightedPair--;
            pTC.FindNaturalDividers(0);

            TextPair p = pTC[pTC.HighlightedPair];

            if (prev_p.RenderedInfo1.Line1 == 0 && (prev_p.RenderedInfo1.X1 == 0 || p.Height > 0)
                || prev_p.RenderedInfo2.Line1 == 0 && (prev_p.RenderedInfo2.X1 == 0 || p.Height > 0))
            {
                pTC.CurrentPair = pTC.HighlightedPair;
                pTC.PrepareScreen();
                pTC.RenderPairs();
            }

            pTC.FindNaturalDividersScreen(0);
            pTC.ProcessMousePosition(true);
            pTC.Render();
        }

        private void ProcessKeyDown()
        {
            if (pTC.HighlightedPair == pTC.Number - 1)
            {
                if (pTC.CurrentPair != pTC.HighlightedPair)
                    GotoPair(pTC.HighlightedPair);
                return;
            }

            pTC.HighlightedPair++;
            pTC.FindNaturalDividers(0);

            TextPair p = pTC[pTC.HighlightedPair];

            if ((p.RenderedInfo1.Line2 == -1
                || p.RenderedInfo1.Line2 >= pTC.LastFullScreenLine
                || p.RenderedInfo2.Line2 == -1
                || p.RenderedInfo2.Line2 >= pTC.LastFullScreenLine)
                && !pTC[pTC.HighlightedPair].IsBig())
            {
                pTC.CurrentPair = pTC.HighlightedPair;
                pTC.PrepareScreen();
                pTC.RenderPairs();
            }

            pTC.FindNaturalDividersScreen(0);

            pTC.ProcessMousePosition(true);

            pTC.Render();
        }

        private void ChangeNatural(byte screen_side, bool inc)
        {
            TextPair p = pTC[pTC.HighlightedPair];

            byte side;

            if (screen_side == 0)
                side = 0;
            else if (pTC.Reversed)
                side = (byte) (3 - screen_side);
            else
                side = screen_side;

            if (side == 0 || side == 1)
                if (inc || p.GetRecommendedNatural(1) > 0)
                {
                    if (inc)
                        p.IncRecommendedNatural(1);
                    else
                        p.DecRecommendedNatural(1);
                    pTC.FindNaturalDividers(1);
                    pTC.FindNaturalDividersScreen(1);
                }

            if (side == 0 || side == 2)
                if (inc || p.GetRecommendedNatural(2) > 0)
                {
                    if (inc)
                        p.IncRecommendedNatural(2);
                    else
                        p.DecRecommendedNatural(2);
                    pTC.FindNaturalDividers(2);
                    pTC.FindNaturalDividersScreen(2);
                }

            pTC.Render();

        }

        private void EditCurrentPair()
        {
            pTC.EditCurrentPair();
        }

        private void importToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ImportTextForm importTextForm = new ImportTextForm())
            {
                importTextForm.PText = pTC.PText;
                importTextForm.ShowDialog();
            }

            pTC.FindNaturalDividers(0);
            Recompute();

        }

        private void informationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (BookInfoForm f = new BookInfoForm())
            {
                f.ParallelTC = pTC;
                f.ShowDialog();
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveBook(false);
        }

        private void SaveBook(bool askForFileName)
        {
            string fileName;

            if (newBook || askForFileName)
            {
                // Ask for the file name
                using (SaveFileDialog d = new SaveFileDialog())
                {
                    d.Filter = "XML files (*.xml)|*.xml";
                    d.RestoreDirectory = true;
                    DialogResult dialogResult = d.ShowDialog();

                    if (dialogResult != DialogResult.OK)
                        return;

                    fileName = d.FileName;

                    RetrieveToTheTop(fileName);

                    newBook = false;
                }
            }
            else
                fileName = pTC.PText.FileName;

            pTC.PText.Save(fileName);
            pTC.Modified = false;
        }

        private bool RetrieveToTheTop(string fileName)
        {
            // Let's check whether there exists this file in the list

            for (int index = 0; index < appSettings.FileUsages.Count; index++)
            {
                if (appSettings.FileUsages[index].FileName == fileName)
                {
                    if (index != 0)
                    {
                        FileUsageInfo toMove = appSettings.FileUsages[index];
                        appSettings.FileUsages.Remove(toMove);
                        appSettings.FileUsages.Insert(0, toMove);
                    }
                    return true;
                }
            }

            FileUsageInfo fileUsageInfo = new FileUsageInfo();

            fileUsageInfo.FileName = fileName;

            appSettings.FileUsages.Insert(0, fileUsageInfo);

            return false;

        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {

            if (AskToSaveModified(sender) == System.Windows.Forms.DialogResult.Cancel)
                return;

            string fileName;

            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "XML files (*.xml)|*.xml";
                openFileDialog.RestoreDirectory = true;
                //openFileDialog.AutoUpgradeEnabled = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    fileName = openFileDialog.FileName;
                    pTC.PText = new ParallelText();
                    pTC.mouse_text_line = -1;
                    
                    pTC.PText.Load(fileName);

                    if (RetrieveToTheTop(fileName))
                        LoadSettingsFromFileUsageInfo(appSettings.FileUsages[0], false);
                    else
                    {
                        pTC.CurrentPair = 0;
                        pTC.HighlightedPair = 0;
                    }

                    newBook = false;

                    pTC.Modified = false;
                    
                    pTC.FindNaturalDividers(0);
                    
                    Recompute();

                }
            }
        }

        private void SaveAppSettings()
        {
            if (appSettings.FileUsages.Count > 0 && !newBook)
            {
                FileUsageInfo f = appSettings.FileUsages[0];

                f.Pair = pTC.HighlightedPair;
                f.TopPair = pTC.CurrentPair;
                f.Reversed = pTC.Reversed;
                f.SplitterRatio = pTC.SplitterRatio;
            }

            appSettings.HighlightFragments = pTC.HighlightFragments;
            appSettings.HighlightFirstWords = pTC.HighlightFirstWords;
            
            appSettings.Brightness = pTC.Brightness;

            Properties.Settings.Default.AppSettings = appSettings;
            Properties.Settings.Default.Save();
        }

        private void pTC_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == ' ')
            {
                if (pTC.NipHighlightedPair())
                    pTC.MouseCurrentWord = null;
                else
                    ProcessKeyDown();

            }
        }

        private void editToolStripMenuItem_Click(object sender, EventArgs e)
        {
            EditCurrentPair();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveBook(true);
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (ColorTableForm f = new ColorTableForm())
                f.ShowDialog();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (pTC.Modified)
            {
                if (AskToSaveModified(sender) == System.Windows.Forms.DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }
            }

            SaveAppSettings();
        }

        DialogResult AskToSaveModified(Object sender)
        {
            SaveAppSettings();

            if (!pTC.Modified)
                return DialogResult.No;

            DialogResult r = MessageBox.Show(
                    "Save modified book?", "The book was modified", MessageBoxButtons.YesNoCancel);

            if (r == System.Windows.Forms.DialogResult.Yes)
            {
                saveToolStripMenuItem_Click(sender, EventArgs.Empty);
                
            }

            return r;

        }


        private void highlightFramgentsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pTC.HighlightFragments = highlightFramgentsToolStripMenuItem.Checked;
            pTC.RenderPairs();
            pTC.Render();
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (pTC.Modified)
                if (AskToSaveModified(sender) == System.Windows.Forms.DialogResult.Cancel)
                    return;

            pTC.CreateNewParallelBook();

            pTC.HighlightedFrame.SetInvisible();
            pTC.NippingFrame.SetInvisible();

            pTC.ProcessLayoutChange();

            newBook = true;
        }

        private void highlightFirstWordsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            pTC.HighlightFirstWords= highlightFirstWordsToolStripMenuItem.Checked;
            pTC.RenderPairs();
            pTC.Render();
        }

        private void structureleftToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BrowseBookStructure(1);
        }

        private void BrowseBookStructure(byte screenSide)
        {
            using (BookStructureForm f = new BookStructureForm())
            {
                f.parallelTextControl = pTC;
                f.screenSide = screenSide;
                f.ShowDialog();

                if (f.pairIndex != -1)
                    GotoPair(f.pairIndex);
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void structurerightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            BrowseBookStructure(2);
        }

    }

    public class CommonWordInfo : WordInfo
    {
        public TextPair TextPair { get; set; }

        public CommonWordInfo(TextPair textPair, string word, int line, int wordX, int wordX2, int pos)
            : base(word, line, wordX, wordX2, pos)
        {
            this.TextPair = textPair;

        }
            
    }

    
    public class FileUsageInfo
    {
        public string FileName { get; set; }
        public int Pair { get; set; }
        public int TopPair { get; set; }
        public bool Reversed { get; set; }
        public float SplitterRatio { get; set; }
        public bool EditMode { get; set; }
    }

    public class AppSettings
    {
        public bool HighlightFragments { get; set; }
        public bool HighlightFirstWords { get; set; }
        public double Brightness { get; set; }

        public Collection<FileUsageInfo> FileUsages { get; set; }

        public AppSettings()
        {
            HighlightFragments = true;
            Brightness = 0.96;
            FileUsages = new Collection<FileUsageInfo>();
        }

    }

    


}
