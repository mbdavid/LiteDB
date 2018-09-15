// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Xml;

namespace ICSharpCode.TextEditor.Document
{
	public class FileSyntaxModeProvider : ISyntaxModeFileProvider
	{
		string    directory;
		List<SyntaxMode> syntaxModes = null;
		
		public ICollection<SyntaxMode> SyntaxModes {
			get {
				return syntaxModes;
			}
		}
		
		public FileSyntaxModeProvider(string directory)
		{
			this.directory = directory;
			UpdateSyntaxModeList();
		}
		
		public void UpdateSyntaxModeList()
		{
			string syntaxModeFile = Path.Combine(directory, "SyntaxModes.xml");
			if (File.Exists(syntaxModeFile)) {
				Stream s = File.OpenRead(syntaxModeFile);
				syntaxModes = SyntaxMode.GetSyntaxModes(s);
				s.Close();
			} else {
				syntaxModes = ScanDirectory(directory);
			}
		}
		
		public XmlTextReader GetSyntaxModeFile(SyntaxMode syntaxMode)
		{
			string syntaxModeFile = Path.Combine(directory, syntaxMode.FileName);
			if (!File.Exists(syntaxModeFile)) {
				throw new HighlightingDefinitionInvalidException("Can't load highlighting definition " + syntaxModeFile + " (file not found)!");
			}
			return new XmlTextReader(File.OpenRead(syntaxModeFile));
		}
		
		List<SyntaxMode> ScanDirectory(string directory)
		{
			string[] files = Directory.GetFiles(directory);
			List<SyntaxMode> modes = new List<SyntaxMode>();
			foreach (string file in files) {
				if (Path.GetExtension(file).Equals(".XSHD", StringComparison.OrdinalIgnoreCase)) {
					XmlTextReader reader = new XmlTextReader(file);
					while (reader.Read()) {
						if (reader.NodeType == XmlNodeType.Element) {
							switch (reader.Name) {
								case "SyntaxDefinition":
									string name       = reader.GetAttribute("name");
									string extensions = reader.GetAttribute("extensions");
									modes.Add(new SyntaxMode(Path.GetFileName(file),
									                         name,
									                         extensions));
									goto bailout;
								default:
									throw new HighlightingDefinitionInvalidException("Unknown root node in syntax highlighting file :" + reader.Name);
							}
						}
					}
				bailout:
					reader.Close();
					
				}
			}
			return modes;
		}
	}
}
