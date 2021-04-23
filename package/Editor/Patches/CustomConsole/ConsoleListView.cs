﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEditor;
using UnityEngine;

namespace Needle.Demystify
{
	internal static class ConsoleListView
	{
		private static readonly LogEntry tempEntry = new LogEntry();
		private const string FilterPrefix = "";

		// called from console list with current list view element and console text
		internal static void ModifyText(ListViewElement element, ref string text)
		{
			if (!DemystifySettings.instance.ShowFileName) return;

			// LogEntries.SetFilteringText("PatchManager");
			if (LogEntries.GetEntryInternal(element.row, tempEntry))
			{
				var filePath = tempEntry.file;
				if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
				{
					var fileName = Path.GetFileNameWithoutExtension(filePath);
					const string colorPrefix = "<color=#999999>";
					const string colorPostfix = "</color>";

					string GetText()
					{
						return colorPrefix + "[" + fileName + "]" + colorPostfix;
					}

					var endTimeIndex = text.IndexOf("] ", StringComparison.InvariantCulture);
					// no time:
					if (endTimeIndex == -1)
					{
						text = $"{GetText()} {text}";
					}
					// contains time:
					else
					{
						text = $"{text.Substring(0, endTimeIndex + 1)} {GetText()}{text.Substring(endTimeIndex + 1)}";
					}


					if (DemystifySettings.instance.AutoFilter)
					{
						// text = element.row + " - " + text;

						// this is only for filtering
						var filter = Path.GetFullPath(filePath); // Path.GetDirectoryName(filePath) + fileName;
						// path = path.Substring((int)(path.Length * .5f));
						filter = MakeFilterable(filter);
						// text = filter;
						// return;

						// many spaces to hide the search match highlight for invisible text
						text +=
							$"\n                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                {filter}";
					}
				}
			}
		}

		private static string MakeFilterable(string str)
		{
			str = str.Replace("\\", "/");
			// str = str.Replace("/", ".");
			str = str.Replace(":", string.Empty);
			return str;
		}

		[InitializeOnLoadMethod]
		private static void Init()
		{
			Selection.selectionChanged += OnSelectionChanged;
		}

		private static string previousFilter;
		private static readonly List<Component> tempComponents = new List<Component>();

		private static void OnSelectionChanged()
		{
			if (!DemystifySettings.instance || !DemystifySettings.instance.AutoFilter) return;

			var sel = Selection.activeObject;
			if (!sel) return;

			void SetFilter(string filter)
			{
				filter = MakeFilterable(filter);
				const int maxLength = 50;
				if (filter.Length > maxLength)
					filter = filter.Substring(filter.Length - maxLength);
				LogEntries.SetFilteringText(filter);
				if (Patch_Console.ConsoleWindow)
				{
					Patch_Console.ConsoleWindow.GetType().GetField("m_SearchText", AccessTools.allDeclared).SetValue(Patch_Console.ConsoleWindow, filter);
					Patch_Console.ConsoleWindow.Repaint();
				}
			}

			if (EditorUtility.IsPersistent(sel))
			{
				var path = AssetDatabase.GetAssetPath(sel);
				// path = Path.GetFullPath(path);
				var fileInfo = new FileInfo(path);

				string PrependParentDirectories(string filter, DirectoryInfo info, int maxLevel, int level = 0)
				{
					if (level >= maxLevel) return filter;
					if (info == null) return filter;
					filter = info.Name + "/" + filter;
					return PrependParentDirectories(filter, info.Parent, maxLevel, ++level);
				}
				if (fileInfo.Exists)
				{
					path = PrependParentDirectories(fileInfo.Name, fileInfo.Directory, 1);// fileInfo.Directory?.Name + "/" + fileInfo.Name;
				}
				else
				{
					var dirInfo = new DirectoryInfo(path);
					path = PrependParentDirectories(dirInfo.Name, dirInfo.Parent, 2);
					// path = dirInfo.Parent?.Name + "/" + dirInfo.Name;
				}
				// var dir = new DirectoryInfo(path).Name;
				// var file = Path.GetFileName(path);
				// path = dir + "/" + file;
				
				// path = Path.GetFileName(path);
				// path = path.Substring((int)(path.Length * .5f));
				// var file = Path.GetFileName(path);
				if (previousFilter == null)
					previousFilter = LogEntries.GetFilteringText();
				SetFilter(path);
			}
			else
			{
				if (sel is GameObject go)
				{
					tempComponents.Clear();
					go.GetComponents(tempComponents);
					foreach (var comp in tempComponents)
					{
						if (comp is Transform) continue;
						// var instanceId = comp.GetInstanceID();
						// var path = AssetDatabase.GetAssetPath(instanceId);
						// // Debug.Log(instanceId + " Found " + path);

						var filter = comp.GetType().Name;
						var res = AssetDatabase.FindAssets(filter);
						foreach (var guid in res)
						{
							var path = AssetDatabase.GUIDToAssetPath(guid);
							if (path.EndsWith(".cs"))
							{
								// Debug.Log(file);
								SetFilter(path);
							}
						}
						// Debug.Log("assets: " + string.Join("\n", res));
						//
						// if (!string.IsNullOrEmpty(path))
						// {
						// 	Debug.Log(file);
						// 	break;
						// }

						// SetFilter(string.Empty);
					}
				}
			}
		}

		// private static void EditorUpdate()
		// {
		// 	var sel = Selection.activeObject;
		// 	if (!sel)
		// 	{
		// 		return;
		// 	}
		// 	LogEntries.SetFilteringText("PortalVisibility"); 
		// }
	}
}