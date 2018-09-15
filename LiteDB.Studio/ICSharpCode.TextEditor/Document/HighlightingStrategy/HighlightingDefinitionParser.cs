// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Schema;

namespace ICSharpCode.TextEditor.Document
{
	public static class HighlightingDefinitionParser
	{
		public static DefaultHighlightingStrategy Parse(SyntaxMode syntaxMode, XmlReader xmlReader)
		{
			return Parse(null, syntaxMode, xmlReader);
		}

		public static DefaultHighlightingStrategy Parse(DefaultHighlightingStrategy highlighter, SyntaxMode syntaxMode, XmlReader xmlReader)
		{
			if (syntaxMode == null)
				throw new ArgumentNullException("syntaxMode");
			if (xmlReader == null)
				throw new ArgumentNullException("xmlTextReader");
			try {
				List<ValidationEventArgs> errors = null;
				XmlReaderSettings settings = new XmlReaderSettings();
				Stream shemaStream = typeof(HighlightingDefinitionParser).Assembly.GetManifestResourceStream("LiteDB.Studio.ICSharpCode.TextEditor.Resources.Mode.xsd");
				settings.Schemas.Add(null, new XmlTextReader(shemaStream));
				settings.Schemas.ValidationEventHandler += delegate(object sender, ValidationEventArgs args) {
					if (errors == null) {
						errors = new List<ValidationEventArgs>();
					}
					errors.Add(args);
				};
				settings.ValidationType = ValidationType.Schema;
				XmlReader validatingReader = XmlReader.Create(xmlReader, settings);

				XmlDocument doc = new XmlDocument();
				doc.Load(validatingReader);
				
				if (highlighter == null)
					highlighter = new DefaultHighlightingStrategy(doc.DocumentElement.Attributes["name"].InnerText);
				
				if (doc.DocumentElement.HasAttribute("extends")) {
					KeyValuePair<SyntaxMode, ISyntaxModeFileProvider> entry = HighlightingManager.Manager.FindHighlighterEntry(doc.DocumentElement.GetAttribute("extends"));
					if (entry.Key == null) {
						throw new HighlightingDefinitionInvalidException("Cannot find referenced highlighting source " + doc.DocumentElement.GetAttribute("extends"));
					} else {
						highlighter = Parse(highlighter, entry.Key, entry.Value.GetSyntaxModeFile(entry.Key));
						if (highlighter == null) return null;
					}
				}
				if (doc.DocumentElement.HasAttribute("extensions")) {
					highlighter.Extensions = doc.DocumentElement.GetAttribute("extensions").Split(new char[] { ';', '|' });
				}
				
				XmlElement environment = doc.DocumentElement["Environment"];
				if (environment != null) {
					foreach (XmlNode node in environment.ChildNodes) {
						if (node is XmlElement) {
							XmlElement el = (XmlElement)node;
							if (el.Name == "Custom") {
								highlighter.SetColorFor(el.GetAttribute("name"), el.HasAttribute("bgcolor") ? new HighlightBackground(el) : new HighlightColor(el));
							} else {
								highlighter.SetColorFor(el.Name, el.HasAttribute("bgcolor") ? new HighlightBackground(el) : new HighlightColor(el));
							}
						}
					}
				}
				
				// parse properties
				if (doc.DocumentElement["Properties"]!= null) {
					foreach (XmlElement propertyElement in doc.DocumentElement["Properties"].ChildNodes) {
						highlighter.Properties[propertyElement.Attributes["name"].InnerText] =  propertyElement.Attributes["value"].InnerText;
					}
				}
				
				if (doc.DocumentElement["Digits"]!= null) {
					highlighter.DigitColor = new HighlightColor(doc.DocumentElement["Digits"]);
				}
				
				XmlNodeList nodes = doc.DocumentElement.GetElementsByTagName("RuleSet");
				foreach (XmlElement element in nodes) {
					highlighter.AddRuleSet(new HighlightRuleSet(element));
				}
				
				xmlReader.Close();
				
				if (errors != null) {
					StringBuilder msg = new StringBuilder();
					foreach (ValidationEventArgs args in errors) {
						msg.AppendLine(args.Message);
					}
					throw new HighlightingDefinitionInvalidException(msg.ToString());
				} else {
					return highlighter;
				}
			} catch (Exception e) {
				throw new HighlightingDefinitionInvalidException("Could not load mode definition file '" + syntaxMode.FileName + "'.\n", e);
			}
		}
	}
}
