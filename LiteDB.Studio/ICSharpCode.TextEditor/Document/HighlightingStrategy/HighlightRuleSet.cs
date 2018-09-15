// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version>$Revision$</version>
// </file>

using System;
using System.Collections;
using System.Xml;

using ICSharpCode.TextEditor.Util;

namespace ICSharpCode.TextEditor.Document
{
	public class HighlightRuleSet
	{
		LookupTable keyWords;
		ArrayList   spans = new ArrayList();
		LookupTable prevMarkers;
		LookupTable nextMarkers;
		char escapeCharacter;
		
		bool ignoreCase = false;
		string name     = null;
		
		bool[] delimiters = new bool[256];
		
		string      reference  = null;
		
		public ArrayList Spans {
			get {
				return spans;
			}
		}
		
		internal IHighlightingStrategyUsingRuleSets Highlighter;
		
		public LookupTable KeyWords {
			get {
				return keyWords;
			}
		}
		
		public LookupTable PrevMarkers {
			get {
				return prevMarkers;
			}
		}
		
		public LookupTable NextMarkers {
			get {
				return nextMarkers;
			}
		}
		
		public bool[] Delimiters {
			get {
				return delimiters;
			}
		}
		
		public char EscapeCharacter {
			get {
				return escapeCharacter;
			}
		}
		
		public bool IgnoreCase {
			get {
				return ignoreCase;
			}
		}
		
		public string Name {
			get {
				return name;
			}
			set {
				name = value;
			}
		}
		
		public string Reference {
			get {
				return reference;
			}
		}
		
		public HighlightRuleSet()
		{
			keyWords    = new LookupTable(false);
			prevMarkers = new LookupTable(false);
			nextMarkers = new LookupTable(false);
		}
		
		public HighlightRuleSet(XmlElement el)
		{
			XmlNodeList nodes;
			
			if (el.Attributes["name"] != null) {
				Name = el.Attributes["name"].InnerText;
			}
			
			if (el.HasAttribute("escapecharacter")) {
				escapeCharacter = el.GetAttribute("escapecharacter")[0];
			}
			
			if (el.Attributes["reference"] != null) {
				reference = el.Attributes["reference"].InnerText;
			}
			
			if (el.Attributes["ignorecase"] != null) {
				ignoreCase  = Boolean.Parse(el.Attributes["ignorecase"].InnerText);
			}
			
			for (int i  = 0; i < Delimiters.Length; ++i) {
				delimiters[i] = false;
			}
			
			if (el["Delimiters"] != null) {
				string delimiterString = el["Delimiters"].InnerText;
				foreach (char ch in delimiterString) {
					delimiters[(int)ch] = true;
				}
			}
			
//			Spans       = new LookupTable(!IgnoreCase);

			keyWords    = new LookupTable(!IgnoreCase);
			prevMarkers = new LookupTable(!IgnoreCase);
			nextMarkers = new LookupTable(!IgnoreCase);
			
			nodes = el.GetElementsByTagName("KeyWords");
			foreach (XmlElement el2 in nodes) {
				HighlightColor color = new HighlightColor(el2);
				
				XmlNodeList keys = el2.GetElementsByTagName("Key");
				foreach (XmlElement node in keys) {
					keyWords[node.Attributes["word"].InnerText] = color;
				}
			}
			
			nodes = el.GetElementsByTagName("Span");
			foreach (XmlElement el2 in nodes) {
				Spans.Add(new Span(el2));
				/*
				Span span = new Span(el2);
				Spans[span.Begin] = span;*/
			}
			
			nodes = el.GetElementsByTagName("MarkPrevious");
			foreach (XmlElement el2 in nodes) {
				PrevMarker prev = new PrevMarker(el2);
				prevMarkers[prev.What] = prev;
			}
			
			nodes = el.GetElementsByTagName("MarkFollowing");
			foreach (XmlElement el2 in nodes) {
				NextMarker next = new NextMarker(el2);
				nextMarkers[next.What] = next;
			}
		}
		
		/// <summary>
		/// Merges spans etc. from the other rule set into this rule set.
		/// </summary>
		public void MergeFrom(HighlightRuleSet ruleSet)
		{
			for (int i = 0; i < delimiters.Length; i++) {
				delimiters[i] |= ruleSet.delimiters[i];
			}
			// insert merged spans in front of old spans
			ArrayList oldSpans = spans;
			spans = (ArrayList)ruleSet.spans.Clone();
			spans.AddRange(oldSpans);
			//keyWords.MergeFrom(ruleSet.keyWords);
			//prevMarkers.MergeFrom(ruleSet.prevMarkers);
			//nextMarkers.MergeFrom(ruleSet.nextMarkers);
		}
	}
}
