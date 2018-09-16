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
using System.Linq;
using System.Diagnostics;
using ICSharpCode.TextEditor.Document;

namespace LiteDB.Studio
{
    public class SqlCodeCompletion : ICompletionDataProvider
    {
        private readonly TextEditorControl _control;
        private readonly ImageList _imageList;

        private ICompletionDataProvider _completionDataProvider { get; set; }
        private CodeCompletionWindow _codeCompletionWindow = null;
        private List<ICompletionData> _codeCompletionData = new List<ICompletionData>();

        public SqlCodeCompletion(TextEditorControl control, ImageList imageList)
        {
            _control = control;
            _imageList = imageList;

            control.ActiveTextAreaControl.TextArea.KeyDown += (o, s) =>
            {
                // open via "ctrl+space"
                if (s.Control && s.KeyCode == Keys.Space)
                {
                    s.SuppressKeyPress = true;
                    this.ShowCodeCompleteWindow('\0');
                }
            };

            control.ActiveTextAreaControl.TextArea.KeyEventHandler += (key) =>
            {
                if (_codeCompletionWindow != null)
                {
                    if (_codeCompletionWindow.ProcessKeyEvent(key)) return true;
                }

                return false;
            };

            control.Disposed += this.CloseCodeCompletionWindow;  // When the editor is disposed, close the code completion window

            this.UpdateCodeCompletion(null);
        }

        public ImageList ImageList => _imageList;

        public string PreSelection => this.FindExpression();

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
            catch (Exception)
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
        
        private string FindExpression()
        {
            var textArea = _control.ActiveTextAreaControl.TextArea;

            try
            {
                var word = textArea.Document.GetLineSegment(textArea.Caret.Position.Line)
                    .GetWord(textArea.Caret.Position.Column - 1);

                return word?.Word ?? "";
            }
            catch (Exception)
            {
                return "";
            }
        }

        public void UpdateCodeCompletion(LiteDatabase db)
        {
            _codeCompletionData = new List<ICompletionData>();

            foreach (var m in BsonExpression.Methods)
            {
                _codeCompletionData.Add(new DefaultCompletionData(m.Name.ToUpper(),
                    m.Name.ToUpper() + "(" +
                    string.Join(", ", m.GetParameters().Select(x => x.Name)) +
                    ")",
                    0));
            }

            var words = new List<string>();

            using (var stream = typeof(SqlCodeCompletion).Assembly.GetManifestResourceStream("LiteDB.Studio.ICSharpCode.TextEditor.Resources.SQL-Mode.xshd"))
            {
                using (var reader = new StreamReader(stream))
                {
                    var content = reader.ReadToEnd();
                    var xml = new XmlDocument();
                    xml.LoadXml(content);

                    var nodes = xml.DocumentElement.SelectNodes("/SyntaxDefinition/RuleSets/RuleSet/KeyWords[@name=\"SqlKeywordsNormal\"]/Key");

                    words.AddRange(nodes.Cast<XmlNode>().Select(x => x.Attributes["word"].Value));
                }
            }

            _codeCompletionData.AddRange(words.Select(x => new DefaultCompletionData(x, x, 3)));

            if (db == null) return;

            // collections
            var cols = db.GetCollection("$cols").Query().ToArray();

            _codeCompletionData.AddRange(cols.Select(x => new DefaultCompletionData(x["name"].AsString, x["name"].AsString, 1)));

        }

        public ICompletionData[] GenerateCompletionData(string fileName, TextArea textArea, char charTyped)
        {
            return _codeCompletionData.ToArray();
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