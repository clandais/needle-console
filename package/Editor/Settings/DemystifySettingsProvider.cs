﻿using System;
using System.Collections.Generic;
using System.Linq;
using needle.EditorPatching;
using UnityEditor;
using UnityEngine;

namespace needle.demystify
{
	public class DemystifySettingsProvider : SettingsProvider
	{
		[SettingsProvider]
		public static SettingsProvider CreateDemystifySettings()
		{
			try
			{
				DemystifySettings.instance.Save();
				return new DemystifySettingsProvider("Project/Needle/Unity Demystify", SettingsScope.Project);
			}
			catch (System.Exception e)
			{
				Debug.LogException(e);
			}

			return null;
		}

		private DemystifySettingsProvider(string path, SettingsScope scopes, IEnumerable<string> keywords = null) : base(path, scopes, keywords)
		{
		}

		private Vector2 scroll;

		public override void OnGUI(string searchContext)
		{
			base.OnGUI(searchContext);
			var settings = DemystifySettings.instance;

			EditorGUI.BeginChangeCheck();

			using (var s = new EditorGUILayout.ScrollViewScope(scroll))
			{
				scroll = s.scrollPosition;
				DrawActivateGUI();

				EditorGUILayout.Space(10);
				EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
				settings.FixHyperlinks = EditorGUILayout.ToggleLeft("Fix Hyperlinks", settings.FixHyperlinks);
				DrawSyntaxGUI(settings);
			}

			GUILayout.FlexibleSpace();
			EditorGUILayout.Space(10);
			using (new EditorGUILayout.HorizontalScope())
			{
				settings.DevelopmentMode = EditorGUILayout.ToggleLeft("Development Mode", settings.DevelopmentMode);
			}

			if (EditorGUI.EndChangeCheck())
			{
				settings.Save();
			}
		}

		private static void DrawSyntaxGUI(DemystifySettings settings)
		{
			settings.UseSyntaxHighlighting = EditorGUILayout.ToggleLeft("Syntax Highlighting", settings.UseSyntaxHighlighting);
			using (new EditorGUI.DisabledScope(!settings.UseSyntaxHighlighting))
			{
				var theme = settings.CurrentTheme;
				if (theme != null)
				{
					EditorGUILayout.BeginHorizontal();
					EditorGUILayout.LabelField(theme.Name, EditorStyles.boldLabel);
					GUILayout.FlexibleSpace();
					EditorGUILayout.EndHorizontal();
					
					EditorGUI.BeginChangeCheck();
					for (var index = 0; index < theme.Entries?.Count; index++)
					{
						var entry = theme.Entries[index];
						entry.Color = EditorGUILayout.ColorField(entry.Key, entry.Color);
					}

					if (EditorGUI.EndChangeCheck())
					{
						theme.SetActive();
					}
				}

				EditorGUILayout.BeginHorizontal();
				if (GUILayout.Button("Print Highlighted Log"))
				{
					var str = GUIUtils.SyntaxHighlightVisualization;
					ApplySyntaxHighlightingMultiline(ref str);
					var p = settings.UseSyntaxHighlighting;
					settings.UseSyntaxHighlighting = false;
					Debug.Log("Example Log: " + "\n\n" + str + "\n\n--------\n");
					settings.UseSyntaxHighlighting = p;
				}
				// if (GUILayout.Button("Copy Theme"))
				// {
				// }
				// GUILayout.FlexibleSpace();
				EditorGUILayout.EndHorizontal();
				
				// if (GUILayout.Button("Reset Theme"))
				// {
				// 	settings.SetDefaultTheme();
				// }
			}
		}

		private static void DrawActivateGUI()
		{
			static IEnumerable<string> Patches()
			{
				yield return typeof(Patch_Exception).FullName;
				yield return typeof(Patch_StacktraceUtility).FullName;
			}

			if (!Patches().All(PatchManager.IsActive))
			{
				EditorGUILayout.HelpBox("Unity Demystify is disabled, click the Button below to enable it", MessageType.Info);
				if (GUILayout.Button("Enable Unity Demystify"))
					foreach (var p in Patches())
						PatchManager.EnablePatch(p);
			}
			else
			{
				if (GUILayout.Button("Disable Unity Demystify"))
					foreach (var p in Patches())
						PatchManager.DisablePatch(p);
			}
		}
		
		
		
		/// <summary>
		/// this is just for internal use and "visualizing" via GUI
		/// </summary>
		private static void ApplySyntaxHighlightingMultiline(ref string str)
		{
			var lines = str.Split('\n');
			str = "";
			// Debug.Log("lines: " + lines.Count());
			foreach (var t in lines)
			{
				var line = t;
				var pathIndex = line.IndexOf("C:/git/", StringComparison.Ordinal);
				if (pathIndex > 0) line = line.Substring(0, pathIndex - 4);
				if (!line.TrimStart().StartsWith("at "))
					line = "at " + line;
				SyntaxHighlighting.AddSyntaxHighlighting(ref line);
				line = line.Replace("at ", "");
				str += line + "\n";
			}
		}
	}
}