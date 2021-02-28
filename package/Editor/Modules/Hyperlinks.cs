﻿using System.Text.RegularExpressions;
using UnityEngine;

namespace Needle.Demystify
{
	internal static class Hyperlinks
	{
		private static readonly Regex hyperlinks = new Regex(@"((?<brackets>\))?(?<prefix> in) (?<file>.*?):line (?<line>\d+)(?<post>.*))",
			RegexOptions.Compiled | RegexOptions.Multiline);

		public static void FixLines(ref string stacktrace)
		{
			var lines = stacktrace.Split('\n');
			stacktrace = "";
			foreach (var t in lines)
			{
				var line = t;
				// hyperlinks capture 
				var path = Hyperlinks.Fix(ref line);
				if (!string.IsNullOrEmpty(path))
				{
					line += ")" + path;
					Filepaths.TryMakeRelative(ref line);
				}
				stacktrace += line + "\n";
			}
		}

		/// <summary>
		/// parse demystify path format and reformat to unity hyperlink format
		/// </summary>
		/// <param name="line"></param>
		/// <returns>hyperlink that must be appended to line once further processing is done</returns>
		public static string Fix(ref string line)
		{
			var match = hyperlinks.Match(line);
			if (match.Success)
			{
				var file = match.Groups["file"];
				var lineNr = match.Groups["line"];
				var brackets = match.Groups["brackets"].ToString();
				if (string.IsNullOrEmpty(brackets)) brackets = ")";
				var post = match.Groups["post"];

				var end = line.Substring(match.Index, line.Length - match.Index);
				line = line.Remove(match.Index, end.Length) + brackets;
				var newString = " (at " + file + ":" + lineNr + ")\n";
				if (post.Success)
					newString += post;
				return newString;
			}

			return string.Empty;
		}


		public static void ApplyHyperlinkColor(ref string stacktrace)
		{
			if (string.IsNullOrEmpty(stacktrace)) return;
			var str = stacktrace;
			const string pattern = @"(?<open>\(at )(?<pre><a href=.*?>)(?<path>.*)(?<post><\/a>)(?<close>\))";
			str = Regex.Replace(str, pattern, m =>
				{
					if (m.Success && SyntaxHighlighting.CurrentTheme.TryGetValue("link", out var col))
					{
						var open = m.Groups["open"].Value;
						var close = m.Groups["close"].Value;
						var pre = m.Groups["pre"].Value;
						var post = m.Groups["post"].Value;
						var path = m.Groups["path"].Value;
						var col_0 = $"<color={col}>";
						var col_1 = "</color>";
						var res = $"{col_0}{open}{col_1}{pre}{col_0}{path}{col_1}{post}{col_0}{close}{col_1}";
						return res;
					}
					return m.Value;
				},
				RegexOptions.Compiled);
			stacktrace = str;
		}
	}
}