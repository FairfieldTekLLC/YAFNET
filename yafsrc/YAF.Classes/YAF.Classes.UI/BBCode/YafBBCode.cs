/* Yet Another Forum.net
 * Copyright (C) 2003-2005 Bj�rnar Henden
 * Copyright (C) 2006-2009 Jaben Cargman
 * http://www.yetanotherforum.net/
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
 */

using System;
using System.Data;
using System.Web;
using System.Text.RegularExpressions;
using System.Web.UI;
using System.Xml;
using YAF.Classes;
using YAF.Classes.Core;
using YAF.Classes.Utils;
using YAF.Classes.Data;

namespace YAF.Classes.UI
{
	/// <summary>
	/// Summary description for YafBBCode.
	/// </summary>
	public class YafBBCode
	{
		/* Ederon : 6/16/2007 - conventions */

		private YafBBCode() { }

		// regular regex...
		static private readonly RegexOptions _options = RegexOptions.IgnoreCase | RegexOptions.Singleline;
		static private readonly string _rgxSize = @"\[size=(?<size>([1-9]))\](?<inner>(.*?))\[/size\]";
		static private readonly string _rgxBold = @"\[B\](?<inner>(.*?))\[/B\]";
		static private readonly string _rgxStrike = @"\[S\](?<inner>(.*?))\[/S\]";
		static private readonly string _rgxItalic = @"\[I\](?<inner>(.*?))\[/I\]";
		static private readonly string _rgxUnderline = @"\[U\](?<inner>(.*?))\[/U\]";
		static private readonly string _rgxFont = @"\[font=(?<font>([-a-z0-9, ]*))\](?<inner>(.*?))\[/font\]";
		static private readonly string _rgxColor = @"\[color=(?<color>(\#?[-a-z0-9]*))\](?<inner>(.*?))\[/color\]";
		static private readonly string _rgxBullet = @"\[\*\]";
		static private readonly string _rgxList4 = @"\[list=i\](?<inner>(.*?))\[/list\]";
		static private readonly string _rgxList3 = @"\[list=a\](?<inner>(.*?))\[/list\]";
		static private readonly string _rgxList2 = @"\[list=1\](?<inner>(.*?))\[/list\]";
		static private readonly string _rgxList1 = @"\[list\](?<inner>(.*?))\[/list\]";
		static private readonly string _rgxCenter = @"\[center\](?<inner>(.*?))\[/center\]";
		static private readonly string _rgxLeft = @"\[left\](?<inner>(.*?))\[/left\]";
		static private readonly string _rgxRight = @"\[right\](?<inner>(.*?))\[/right\]";
		static private readonly string _rgxHr = "^[-][-][-][-][-]*[\r]?[\n]";
		static private readonly string _rgxBr = "[\r]?\n";
		static private readonly string _rgxPost = @"\[post=(?<post>[^\]]*)\](?<inner>(.*?))\[/post\]";
		static private readonly string _rgxTopic = @"\[topic=(?<topic>[^\]]*)\](?<inner>(.*?))\[/topic\]";
		static private readonly string _rgxBBCodeLocalizationTag = @"\[localization=(?<tag>[^\]]*)\](?<inner>(.*?))\[/localization\]";
		// precompiled regex...
		static private readonly Regex _rgxEmail2 = new Regex( @"\[email=(?<email>[^\]]*)\](?<inner>(.*?))\[/email\]", _options | RegexOptions.Compiled );
		static private readonly Regex _rgxEmail1 = new Regex( @"\[email[^\]]*\](?<inner>(.*?))\[/email\]", _options | RegexOptions.Compiled );
		static private readonly Regex _rgxUrl1 = new Regex( @"\[url\](?<http>(skype:)|(http://)|(https://)| (ftp://)|(ftps://))?(?<inner>(.*?))\[/url\]", _options | RegexOptions.Compiled );
		static private readonly Regex _rgxUrl2 = new Regex( @"\[url\=(?<http>(skype:)|(http://)|(https://)|(ftp://)|(ftps://))?(?<url>([^\]]*?))\](?<inner>(.*?))\[/url\]", _options | RegexOptions.Compiled );
		static private readonly Regex _rgxCode2 = new Regex( @"\[code=(?<language>[^\]]*)\](?<inner>(.*?))\[/code\]", _options | RegexOptions.Compiled );
		static private readonly Regex _rgxCode1 = new Regex( @"\[code\](?<inner>(.*?))\[/code\]", _options | RegexOptions.Compiled );
		static private readonly Regex _rgxQuote2 = new Regex( @"\[quote=(?<quote>[^\]]*)\](?<inner>(.*?))\[/quote\]", _options | RegexOptions.Compiled );
		static private readonly Regex _rgxQuote1 = new Regex( @"\[quote\](?<inner>(.*?))\[/quote\]", _options | RegexOptions.Compiled );
		static private readonly Regex _rgxImg = new Regex( @"\[img\](?<http>(http://)|(https://)|(ftp://)|(ftps://))?(?<inner>(.*?))\[/img\]", _options | RegexOptions.Compiled );

		/// <summary>
		/// Converts a string containing YafBBCode to the equivalent HTML string.
		/// </summary>
		/// <param name="inputString">Input string containing YafBBCode to convert to HTML</param>
		/// <param name="doFormatting"></param>
		/// <param name="targetBlankOverride"></param>
		/// <returns></returns>
		static public string MakeHtml( string inputString, bool doFormatting, bool targetBlankOverride )
		{
			// get the rules engine from the creator...
			ReplaceRules ruleEngine = ReplaceRulesCreator.GetInstance( new bool [] { doFormatting, targetBlankOverride, YafContext.Current.BoardSettings.UseNoFollowLinks } );

			if ( !ruleEngine.HasRules )
			{
				CreateBBCodeRules( ref ruleEngine, doFormatting, targetBlankOverride, YafContext.Current.BoardSettings.UseNoFollowLinks );
			}
			
			ruleEngine.Process( ref inputString );
			
			return inputString;
		}

		/// <summary>
		/// Converts a message containing YafBBCode to HTML appropriate for editing in a rich text editor.
		/// </summary>
		/// <remarks>
		/// YafBBCode quotes are not converted to HTML.  "[quote]...[/quote]" will remain in the string 
		/// returned, as to appear in plaintext in rich text editors.
		/// </remarks>
		/// <param name="message">String containing the body of the message to convert</param>
		/// <returns>The converted text</returns>
		static public string ConvertBBCodeToHtmlForEdit( string message )
		{
			bool doFormatting = true;
			bool targetBlankOverride = false;
			bool forHtmlEditing = true;

			// get the rules engine from the creator...
			ReplaceRules ruleEngine = ReplaceRulesCreator.GetInstance(
				new bool[]
					{
						doFormatting,
						targetBlankOverride,
						YafContext.Current.BoardSettings.UseNoFollowLinks,
						forHtmlEditing
					});

			if ( !ruleEngine.HasRules )
			{
				// Do not convert BBQuotes to HTML when editing -- "[quote]...[/quote]" will remain in plaintext in the rich text editor
				CreateBBCodeRules(ref ruleEngine, doFormatting, targetBlankOverride,
				                  YafContext.Current.BoardSettings.UseNoFollowLinks, false /*convertBBQuotes*/);
			}

			ruleEngine.Process(ref message);

			return message;
		}

		/// <summary>
		/// Creates the rules that convert YafBBCode to HTML
		/// </summary>
		static public void CreateBBCodeRules(ref ReplaceRules ruleEngine, bool doFormatting, bool targetBlankOverride, bool useNoFollow)
		{
			CreateBBCodeRules(ref ruleEngine, doFormatting, targetBlankOverride, useNoFollow, true);
		}

		/// <summary>
		/// Creates the rules that convert YafBBCode to HTML
		/// </summary>
		static public void CreateBBCodeRules( ref ReplaceRules ruleEngine, bool doFormatting, bool targetBlankOverride, bool useNoFollow, bool convertBBQuotes )
		{
			string target = ( YafContext.Current.BoardSettings.BlankLinks || targetBlankOverride ) ? "target=\"_blank\"" : "";
			string nofollow = ( useNoFollow ) ? "rel=\"nofollow\"" : "";

			// pull localized strings
			string localQuoteStr = YafContext.Current.Localization.GetText( "COMMON", "BBCODE_QUOTE" );
			string localQuoteWroteStr = YafContext.Current.Localization.GetText( "COMMON", "BBCODE_QUOTEWROTE" );
			string localCodeStr = YafContext.Current.Localization.GetText( "COMMON", "BBCODE_CODE" );

			// add rule for code block type with syntax highlighting			
			ruleEngine.AddRule( new SyntaxHighlightedCodeRegexReplaceRule( _rgxCode2, @"<div class=""code""><b>{0}</b><div class=""innercode"">${inner}</div></div>".Replace( "{0}", localCodeStr ) ) );

			// add rule for code block type with no syntax highlighting
			ruleEngine.AddRule( new CodeRegexReplaceRule( _rgxCode1, @"<div class=""code""><b>{0}</b><div class=""innercode"">${inner}</div></div>".Replace( "{0}", localCodeStr ) ) );

			// handle font sizes -- this rule class internally handles the "size" variable
			ruleEngine.AddRule( new FontSizeRegexReplaceRule( _rgxSize, @"<span style=""font-size:${size}"">${inner}</span>", _options ) );

			if ( doFormatting )
			{
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxBold, "<b>${inner}</b>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxStrike, "<s>${inner}</s>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxItalic, "<i>${inner}</i>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxUnderline, "<u>${inner}</u>", _options ) );

				// e-mails
				ruleEngine.AddRule( new VariableRegexReplaceRule( _rgxEmail2, "<a href=\"mailto:${email}\">${inner}</a>", new string [] { "email" } ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxEmail1, "<a href=\"mailto:${inner}\">${inner}</a>" ) );

				// urls
				ruleEngine.AddRule(
					new VariableRegexReplaceRule(
						_rgxUrl2,
						"<a {0} {1} href=\"${http}${url}\" title=\"${http}${url}\">${inner}</a>".Replace( "{0}", target ).Replace( "{1}", nofollow ),
						new string [] { "url", "http" },
						new string [] { "", "http://" }
						)
				);
				ruleEngine.AddRule(
					new VariableRegexReplaceRule(
						_rgxUrl1,
						"<a {0} {1} href=\"${http}${inner}\" title=\"${http}${inner}\">${http}${innertrunc}</a>".Replace( "{0}", target ).Replace( "{1}", nofollow ),
						new string [] { "http" },
						new string [] { "", "http://" },
						50
						)
				);

				// font
				ruleEngine.AddRule(
					new VariableRegexReplaceRule(
						_rgxFont,
						"<span style=\"font-family:${font}\">${inner}</span>",
						_options,
						new string [] { "font" }
						)
				);

				// color
				ruleEngine.AddRule(
					new VariableRegexReplaceRule(
						_rgxColor,
						"<span style=\"color:${color}\">${inner}</span>",
						_options,
						new string [] { "color" }
						)
				);

				// bullets
				ruleEngine.AddRule( new SingleRegexReplaceRule( _rgxBullet, "<li>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxList4, "<ol type=\"i\">${inner}</ol>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxList3, "<ol type=\"a\">${inner}</ol>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxList2, "<ol>${inner}</ol>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxList1, "<ul>${inner}</ul>", _options ) );

				// alignment
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxCenter, "<div align=\"center\">${inner}</div>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxLeft, "<div align=\"left\">${inner}</div>", _options ) );
				ruleEngine.AddRule( new SimpleRegexReplaceRule( _rgxRight, "<div align=\"right\">${inner}</div>", _options ) );

				// image
				ruleEngine.AddRule(
					new VariableRegexReplaceRule(
						_rgxImg,
						"<img src=\"${http}${inner}\" alt=\"\"/>",
						new string [] { "http" },
						new string [] { "http://" }
						)
				);

				// handle custom YafBBCode
				AddCustomBBCodeRules( ref ruleEngine );

				// basic hr and br rules
				SingleRegexReplaceRule hrRule = new SingleRegexReplaceRule( _rgxHr, "<hr/>", _options | RegexOptions.Multiline );	// Multiline, since ^ must match beginning of line
				SingleRegexReplaceRule brRule = new SingleRegexReplaceRule( _rgxBr, "<br/>", _options );
				brRule.RuleRank = hrRule.RuleRank + 1;	// Ensure the newline rule is processed after the HR rule, otherwise the newline characters in the HR regex will never match
				ruleEngine.AddRule( hrRule );
				ruleEngine.AddRule( brRule );
			}

			// add smilies
			FormatMsg.AddSmiles( ref ruleEngine );

			if (convertBBQuotes)
			{
				// "quote" handling...
				string tmpReplaceStr;

				tmpReplaceStr =
					string.Format(@"<div class=""quote""><b>{0}</b><div class=""innerquote"">{1}</div></div>",
					              localQuoteWroteStr.Replace("{0}", "${quote}"), "${inner}");
				ruleEngine.AddRule(new VariableRegexReplaceRule(_rgxQuote2, tmpReplaceStr, new string[] {"quote"}));

				tmpReplaceStr =
					string.Format(@"<div class=""quote""><b>{0}</b><div class=""innerquote"">{1}</div></div>", localQuoteStr,
					              "${inner}");
				ruleEngine.AddRule(new SimpleRegexReplaceRule(_rgxQuote1, tmpReplaceStr ));
			}

			// post and topic rules...
			ruleEngine.AddRule(
				new PostTopicRegexReplaceRule(
					_rgxPost,
					@"<a {0} href=""${post}"">${inner}</a>".Replace( "{0}", target ),
					_options
					)
			);
			ruleEngine.AddRule(
				new PostTopicRegexReplaceRule(
					_rgxTopic,
					@"<a {0} href=""${topic}"">${inner}</a>".Replace( "{0}", target ),
					_options
					)
			);
		}

		/// <summary>
		/// Applies Custom YafBBCode Rules from the YafBBCode table
		/// </summary>
		static protected void AddCustomBBCodeRules( ref ReplaceRules rulesEngine )
		{
			DataTable bbcodeTable = GetCustomBBCode();

			// handle custom bbcodes row by row...
			foreach ( DataRow codeRow in bbcodeTable.Rows )
			{
				if ( codeRow ["UseModule"] != DBNull.Value &&
						 Convert.ToBoolean( codeRow ["UseModule"] ) &&
						 codeRow ["ModuleClass"] != DBNull.Value &&
						 codeRow ["SearchRegEx"] != DBNull.Value )
				{
					// code module!
					string searchRegEx = codeRow ["SearchRegEx"].ToString();
					string moduleClass = codeRow ["ModuleClass"].ToString();
					string rawVariables = codeRow ["Variables"].ToString();

					// create Module Invocation XML Document
					XmlDocument moduleInfoDoc = new XmlDocument();
					XmlElement mainNode = moduleInfoDoc.CreateElement( "YafModuleFactoryInvocation" );
					mainNode.SetAttribute( "ClassName", moduleClass );
					moduleInfoDoc.AppendChild( mainNode );
					XmlElement paramsNode = moduleInfoDoc.CreateElement( "Parameters" );
					mainNode.AppendChild( paramsNode );

					// add "inner" param as all have inner...
					XmlElement innerParam = moduleInfoDoc.CreateElement( "Param" );
					innerParam.SetAttribute( "Name", "inner" );
					XmlText innerText = moduleInfoDoc.CreateTextNode( "${inner}" );
					innerParam.AppendChild( innerText );
					paramsNode.AppendChild( innerParam );

					if ( !String.IsNullOrEmpty( rawVariables ) )
					{
						// handle variables...
						string [] variables = rawVariables.Split( new char [] { ';' } );

						// add variables to the XML
						foreach ( string var in variables )
						{
							innerParam = moduleInfoDoc.CreateElement( "Param" );
							innerParam.SetAttribute( "Name", var );
							innerText = moduleInfoDoc.CreateTextNode( "${" + var + "}" );
							innerParam.AppendChild( innerText );
							paramsNode.AppendChild( innerParam );
						}

						VariableRegexReplaceRule rule = new VariableRegexReplaceRule( searchRegEx, moduleInfoDoc.OuterXml, _options, variables );
						rule.RuleRank = 50;
						rulesEngine.AddRule( rule );
					}
					else
					{
						// just standard replace...
						SimpleRegexReplaceRule rule = new SimpleRegexReplaceRule( searchRegEx, moduleInfoDoc.OuterXml, _options );
						rule.RuleRank = 50;
						rulesEngine.AddRule( rule );
					}
				}
				else if (	codeRow ["SearchRegEx"] != DBNull.Value &&
									codeRow ["ReplaceRegEx"] != DBNull.Value &&
									!String.IsNullOrEmpty(codeRow ["SearchRegEx"].ToString().Trim()) )
				{
					string searchRegEx = codeRow ["SearchRegEx"].ToString();
					string replaceRegEx = codeRow ["ReplaceRegEx"].ToString();
					string rawVariables = codeRow ["Variables"].ToString();

					if ( !String.IsNullOrEmpty( rawVariables ) )
					{
						// handle variables...
						string [] variables = rawVariables.Split( new char [] { ';' } );

						VariableRegexReplaceRule rule = new VariableRegexReplaceRule( searchRegEx, replaceRegEx, _options, variables );
						rule.RuleRank = 50;
						rulesEngine.AddRule( rule );
					}
					else
					{
						// just standard replace...
						SimpleRegexReplaceRule rule = new SimpleRegexReplaceRule( searchRegEx, replaceRegEx, _options );
						rule.RuleRank = 50;
						rulesEngine.AddRule( rule );
					}
				}
			}
		}

		static public DataTable GetCustomBBCode()
		{
			string cacheKey = YafCache.GetBoardCacheKey( Constants.Cache.CustomBBCode );
			DataTable bbCodeTable;

			// check if there is value cached
			if ( YafContext.Current.Cache [cacheKey] == null )
			{
				// get the bbcode table from the db...
				bbCodeTable = YAF.Classes.Data.DB.bbcode_list( YafContext.Current.PageBoardID, null );
				// cache it indefinately (or until it gets updated)
				YafContext.Current.Cache [cacheKey] = bbCodeTable;
			}
			else
			{
				// retrieve bbcode Table from the cache
				bbCodeTable = ( DataTable )YafContext.Current.Cache [cacheKey];
			}

			return bbCodeTable;
		}

		/// <summary>
		/// Helper function that dandles registering "custom bbcode" javascript (if there is any)
		/// for all the custom YafBBCode.
		/// </summary>
		static public void RegisterCustomBBCodePageElements( System.Web.UI.Page currentPage, Type currentType )
		{
			RegisterCustomBBCodePageElements( currentPage, currentType, null );
		}

		/// <summary>
		/// Helper function that dandles registering "custom bbcode" javascript (if there is any)
		/// for all the custom YafBBCode. Defining editorID make the system also show "editor js" (if any).
		/// </summary>
		static public void RegisterCustomBBCodePageElements( System.Web.UI.Page currentPage, Type currentType, string editorID )
		{
			DataTable bbCodeTable = YafBBCode.GetCustomBBCode();
			string scriptID = "custombbcode";
			var jsScriptBuilder = new System.Text.StringBuilder();
			var cssBuilder = new System.Text.StringBuilder();

			jsScriptBuilder.Append( "\r\n" );
			cssBuilder.Append( "\r\n" );

			foreach ( DataRow row in bbCodeTable.Rows )
			{
				string displayScript = null;
				string editScript = null;

				if ( row ["DisplayJS"] != DBNull.Value )
				{
					displayScript = LocalizeCustomBBCodeElement(row ["DisplayJS"].ToString().Trim());
				}

				if ( !String.IsNullOrEmpty( editorID ) && row ["EditJS"] != DBNull.Value )
				{
					editScript = LocalizeCustomBBCodeElement( row ["EditJS"].ToString().Trim() );
					// replace any instances of editor ID in the javascript in case the ID is needed
					editScript = editScript.Replace( "{editorid}", editorID );
				}

				if ( !String.IsNullOrEmpty( displayScript ) || !String.IsNullOrEmpty( editScript ) )
				{
					jsScriptBuilder.AppendLine( displayScript + "\r\n" + editScript );
				}

				// see if there is any CSS associated with this YafBBCode
				if ( row ["DisplayCSS"] != DBNull.Value && !String.IsNullOrEmpty( row ["DisplayCSS"].ToString().Trim() ) )
				{
					// yes, add it into the builder
					cssBuilder.AppendLine( LocalizeCustomBBCodeElement(row ["DisplayCSS"].ToString().Trim()) );
				}
			}

			if ( jsScriptBuilder.ToString().Trim().Length > 0 )
			{
				YafContext.Current.PageElements.RegisterJsBlock( currentPage, scriptID + "_script", jsScriptBuilder.ToString() );
			}

			if ( cssBuilder.ToString().Trim().Length > 0 )
			{
				// register the CSS from all custom bbcode...
				YafContext.Current.PageElements.RegisterCssBlock(scriptID + "_css", cssBuilder.ToString());
			}
		}

		/// <summary>
		/// Handles localization for a Custom YafBBCode Elements using
		/// the code [localization=tag]default[/localization]
		/// </summary>
		/// <param name="strToLocalize"></param>
		/// <returns></returns>
		static public string LocalizeCustomBBCodeElement( string strToLocalize )
		{
			Regex regExSearch = new Regex( _rgxBBCodeLocalizationTag, _options );

			var sb = new System.Text.StringBuilder( strToLocalize );

			Match m = regExSearch.Match( strToLocalize );
			while ( m.Success )
			{
				// get the localization tag...
				string tagValue = m.Groups ["tag"].Value;
				string defaultValue = m.Groups ["inner"].Value;

				// remove old code...
				sb.Remove( m.Groups [0].Index, m.Groups [0].Length );

				// insert localized value...
				string localValue = defaultValue;

				if ( YafContext.Current.Localization.GetTextExists( "BBCODEMODULE", tagValue ) )
				{
					localValue = YafContext.Current.Localization.GetText( "BBCODEMODULE", tagValue );
				}

				sb.Insert( m.Groups [0].Index, localValue );
				m = regExSearch.Match( sb.ToString() );
			}

			return sb.ToString();
		}

		/// <summary>
		/// Encodes HTML - same as <see cref="HttpServerUtility.HtmlEncode(string)" />
		/// </summary>
		/// <param name="html"></param>
		/// <returns></returns>
		static public string EncodeHTML( string html )
		{
			return System.Web.HttpContext.Current.Server.HtmlEncode( html );
		}

		/// <summary>
		/// Decodes HTML - same as <see cref="HttpServerUtility.HtmlDecode(string)" />
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		static public string DecodeHTML( string text )
		{
			return System.Web.HttpContext.Current.Server.HtmlDecode( text );
		}
	}
}
