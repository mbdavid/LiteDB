# Syntax Highlighting for Windows Forms Rich Text Box

> https://github.com/sinairv/WinFormsSyntaxHighlighter

This repository will enable syntax highlighting based on the patterns the programmer defines.

Example:

```csharp
var syntaxHighlighter = new SyntaxHighlighter(theRichTextBox);

// That's it. Now tell me how you'd like to see what...

// multi-line comments; I'd like to see them in dark-sea-green and italic
syntaxHighlighter.AddPattern(
    new PatternDefinition(new Regex(@"/\*(.|[\r\n])*?\*/", 
        RegexOptions.Multiline | RegexOptions.Compiled)), 
    new SyntaxStyle(Color.DarkSeaGreen, bold: false, italic: true));

// singlie-line comments; I'd like to see them in Green and italic
syntaxHighlighter.AddPattern(
    new PatternDefinition(new Regex(@"//.*?$", 
        RegexOptions.Multiline | RegexOptions.Compiled)), 
    new SyntaxStyle(Color.Green, bold: false, italic: true));

// numbers; I'd like to see them in purple
syntaxHighlighter.AddPattern(
    new PatternDefinition(@"\d+\.\d+|\d+"), 
    new SyntaxStyle(Color.Purple));

// double quote strings; I'd like to see them in Red
syntaxHighlighter.AddPattern(
    new PatternDefinition(@"\""([^""]|\""\"")+\"""), 
    new SyntaxStyle(Color.Red));

// single quote strings; I'd like to see them in Salmon 
syntaxHighlighter.AddPattern(
    new PatternDefinition(@"\'([^']|\'\')+\'"), 
    new SyntaxStyle(Color.Salmon));
            
// 1st set of keywords; I'd like to see them in Blue
syntaxHighlighter.AddPattern(
    new PatternDefinition("for", "foreach", "int", "var"), 
    new SyntaxStyle(Color.Blue));
            
// 2nd set of keywords; I'd like to see them in bold Navy, and they must be case insensitive
syntaxHighlighter.AddPattern(
    new CaseInsensitivePatternDefinition("public", "partial", "class", "void"), 
    new SyntaxStyle(Color.Navy, true, false));
            
// operators; I'd like to see them in Brown
syntaxHighlighter.AddPattern(
    new PatternDefinition("+", "-", ">", "<", "&", "|"), 
    new SyntaxStyle(Color.Brown));
``` 


## License

MIT 
