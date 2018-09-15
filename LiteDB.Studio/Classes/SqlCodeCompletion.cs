// This code was based on the CSharp Editor Example with Code Completion created by Daniel Grunwald
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using ICSharpCode.TextEditor;
using ICSharpCode.TextEditor.Gui.CompletionWindow;

namespace LiteDB.Studio
{
    public class SqlCodeCompletion : ICompletionDataProvider
    {
        private readonly TextEditorControl _control;
        private readonly ImageList _imageList;

        private bool _open = false;
        private ICompletionDataProvider _completionDataProvider { get; set; }
        private CodeCompletionWindow _codeCompletionWindow = null;


        public SqlCodeCompletion(TextEditorControl control, ImageList imageList)
        {
            _control = control;
            _imageList = imageList;

            control.ActiveTextAreaControl.TextArea.KeyDown += (o, s) =>
            {
                if (s.Control && s.KeyCode == Keys.Space)
                {
                    _open = true;
                }
                else
                {
                    _open = false;
                }
            };

            control.ActiveTextAreaControl.TextArea.KeyEventHandler += (key) =>
            {
                if (_codeCompletionWindow != null)
                {
                    if (_codeCompletionWindow.ProcessKeyEvent(key)) return true;
                }

                if (_open)
                {
                    this.ShowCodeCompleteWindow(key);
                }

                return false;

            };
            control.Disposed += CloseCodeCompletionWindow;  // When the editor is disposed, close the code completion window

            //set up the ToolTipRequest event
            // control.ActiveTextAreaControl.TextArea.ToolTipRequest += OnToolTipRequest;

        }

        public ImageList ImageList => _imageList;

        public string PreSelection => null;

        public int DefaultIndex => -1;

        private void ShowCodeCompleteWindow(char key)
        {
            try
            {
                _completionDataProvider = this;

                _codeCompletionWindow = CodeCompletionWindow.ShowCompletionWindow(
                    _control.ParentForm,
                    _control,
                    "file.sql",
                    _completionDataProvider,
                    key
                );

                if (_codeCompletionWindow != null)
                {
                    _codeCompletionWindow.Closed += CloseCodeCompletionWindow;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void CloseCodeCompletionWindow(object sender, EventArgs e)
        {
            if (_codeCompletionWindow != null)
            {
                _codeCompletionWindow.Closed -= CloseCodeCompletionWindow;
                _codeCompletionWindow.Dispose();
                _codeCompletionWindow = null;
            }
        }

        public ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
        {
            return new ICompletionData[] {
                new DefaultCompletionData("SELECT", "Description", 1),
                new DefaultCompletionData("FROM", "Description", 1),
                new DefaultCompletionData("WHERE", "Description", 1),
                new DefaultCompletionData("ORDER", "Description", 1),
                new DefaultCompletionData("BY", "Description", 1),
                new DefaultCompletionData("TOP", "Description", 1),
                new DefaultCompletionData("Text", "Description", 1)

            };

        }

        public bool InsertAction(ICompletionData data, TextArea textArea, int insertionOffset, char key)
        {
            textArea.Caret.Position = textArea.Document.OffsetToPosition(insertionOffset);

            return data.InsertAction(textArea, key);
        }

        public CompletionDataProviderKeyResult ProcessKey(char key)
        {
            if (char.IsLetterOrDigit(key) || key == '_')
            {
                return CompletionDataProviderKeyResult.NormalKey;
            }

            // key triggers insertion of selected items
            return CompletionDataProviderKeyResult.InsertionKey;
        }
    }
}