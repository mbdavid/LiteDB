// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ICSharpCode.TextEditor.Gui.CompletionWindow
{
    /// <summary>
    /// Description of CodeCompletionListView.
    /// </summary>
    public class CodeCompletionListView : System.Windows.Forms.UserControl
    {
        ICompletionData[] _fullItems;
        ICompletionData[] _filteredItems;

        int _firstItem = 0;
        int _selectedItem = -1;
        ImageList _imageList;

        public ImageList ImageList
        {
            get => _imageList;
            set => _imageList = value;
        }

        public int FirstItem
        {
            get => _firstItem;
            set
            {
                if (_firstItem != value)
                {
                    _firstItem = value;
                    OnFirstItemChanged(EventArgs.Empty);
                }
            }
        }

        public ICompletionData SelectedCompletionData
        {
            get
            {
                if (_selectedItem < 0 || _filteredItems.Length == 0)
                {
                    return null;
                }

                return _filteredItems[_selectedItem];
            }
        }

        public int ItemHeight => Math.Max(_imageList.ImageSize.Height, (int)(this.Font.Height * 1.25));

        public int MaxVisibleItem => this.Height / this.ItemHeight;

        public CodeCompletionListView(ICompletionData[] completionData)
        {
            Array.Sort(completionData, DefaultCompletionData.Compare);

            _fullItems = completionData;
            _filteredItems = new List<ICompletionData>(completionData).ToArray();
        }

        public void Close()
        {
            if (_fullItems != null)
            {
                Array.Clear(_fullItems, 0, _fullItems.Length);
            }
            if (_filteredItems != null)
            {
                Array.Clear(_filteredItems, 0, _filteredItems.Length);
            }
            base.Dispose();
        }

        public void SelectIndex(int index)
        {
            var oldSelectedItem = _selectedItem;
            var oldFirstItem = _firstItem;

            index = Math.Max(0, index);
            _selectedItem = Math.Max(0, Math.Min(_filteredItems.Length - 1, index));

            if (_selectedItem < _firstItem)
            {
                this.FirstItem = _selectedItem;
            }
            if (_firstItem + this.MaxVisibleItem <= _selectedItem)
            {
                this.FirstItem = _selectedItem - this.MaxVisibleItem + 1;
            }
            if (oldSelectedItem != _selectedItem)
            {
                if (_firstItem != oldFirstItem)
                {
                    this.Invalidate();
                }
                else
                {
                    var min = Math.Min(_selectedItem, oldSelectedItem) - _firstItem;
                    var max = Math.Max(_selectedItem, oldSelectedItem) - _firstItem;
                    this.Invalidate(new Rectangle(0, 1 + min * ItemHeight, Width, (max - min + 1) * ItemHeight));
                }

                this.OnSelectedItemChanged(EventArgs.Empty);
            }
        }

        public void ClearSelection()
        {
            if (_selectedItem < 0) return;

            var itemNum = _selectedItem - _firstItem;

            _selectedItem = -1;

            this.Invalidate(new Rectangle(0, itemNum * this.ItemHeight, this.Width, (itemNum + 1) * this.ItemHeight + 1));
            this.Update();
            this.OnSelectedItemChanged(EventArgs.Empty);
        }

        public void PageDown()
        {
            this.SelectIndex(_selectedItem + this.MaxVisibleItem);
        }

        public void PageUp()
        {
            this.SelectIndex(_selectedItem - this.MaxVisibleItem);
        }

        public void SelectNextItem()
        {
            this.SelectIndex(_selectedItem + 1);
        }

        public void SelectPrevItem()
        {
            this.SelectIndex(_selectedItem - 1);
        }

        public void SelectItemWithStart(string startText)
        {
            if (startText == null || startText.Length == 0) return;

            _filteredItems = _fullItems
                .Where(x => x.Text.StartsWith(startText, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (_filteredItems.Length == 0)
            {
                (this.Parent as Form).Close();
            }
            else
            {
                var height = this.ItemHeight * Math.Min(10, _filteredItems.Length);
                var scroll = (VScrollBar)this.Parent.Controls.Find("scroll", true).First();

                scroll.Visible = _filteredItems.Length > 10;
                scroll.Maximum = _filteredItems.Length - 1;

                this.Parent.Height = height;
                this.Invalidate();
                this.SelectIndex(0);
            }
        }

        protected override void OnPaint(PaintEventArgs pe)
        {
            float yPos = 1;
            float itemHeight = ItemHeight;

            // Maintain aspect ratio
            int imageWidth = (int)(itemHeight * _imageList.ImageSize.Width / _imageList.ImageSize.Height);

            int curItem = _firstItem;
            Graphics g = pe.Graphics;
            while (curItem < _filteredItems.Length && yPos < Height)
            {
                var drawingBackground = new RectangleF(1, yPos, Width - 2, itemHeight);

                if (drawingBackground.IntersectsWith(pe.ClipRectangle))
                {
                    // draw Background
                    if (curItem == _selectedItem)
                    {
                        g.FillRectangle(SystemBrushes.Highlight, drawingBackground);
                    }
                    else
                    {
                        g.FillRectangle(SystemBrushes.Window, drawingBackground);
                    }

                    // draw Icon
                    int xPos = 0;
                    if (_imageList != null && _filteredItems[curItem].ImageIndex < _imageList.Images.Count)
                    {
                        g.DrawImage(_imageList.Images[_filteredItems[curItem].ImageIndex], new RectangleF(1, yPos, imageWidth, itemHeight));
                        xPos = imageWidth;
                    }

                    // draw text
                    if (curItem == _selectedItem)
                    {
                        g.DrawString(_filteredItems[curItem].Text, Font, SystemBrushes.HighlightText, xPos + 3, yPos + 3);
                    }
                    else
                    {
                        g.DrawString(_filteredItems[curItem].Text, Font, SystemBrushes.WindowText, xPos + 3, yPos + 3);
                    }
                }

                yPos += itemHeight;
                ++curItem;
            }

            g.DrawRectangle(SystemPens.Control, new Rectangle(0, 0, Width - 1, Height - 1));
        }

        protected override void OnMouseDown(System.Windows.Forms.MouseEventArgs e)
        {
            float yPos = 1;
            int curItem = _firstItem;
            float itemHeight = ItemHeight;

            while (curItem < _filteredItems.Length && yPos < Height)
            {
                var drawingBackground = new RectangleF(1, yPos, Width - 2, itemHeight);

                if (drawingBackground.Contains(e.X, e.Y))
                {
                    this.SelectIndex(curItem);
                    break;
                }
                yPos += itemHeight;
                ++curItem;
            }
        }

        protected override void OnPaintBackground(PaintEventArgs pe)
        {
        }

        protected virtual void OnSelectedItemChanged(EventArgs e)
        {
            SelectedItemChanged?.Invoke(this, e);
        }

        protected virtual void OnFirstItemChanged(EventArgs e)
        {
            FirstItemChanged?.Invoke(this, e);
        }

        public event EventHandler SelectedItemChanged;
        public event EventHandler FirstItemChanged;
    }
}
