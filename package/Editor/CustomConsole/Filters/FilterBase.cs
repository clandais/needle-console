﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Needle.Demystify
{
	[Serializable]
	public abstract class FilterBase<T> : IConsoleFilter
	{
		private bool _isEnabled = true;

		public bool Enabled
		{
			get => _isEnabled;
			set
			{
				if (value == _isEnabled) return;
				_isEnabled = value;
				if (ConsoleFilter.Contains(this))
					ConsoleFilter.MarkDirty();
			}
		}

		[Serializable]
		public struct FilterEntry
		{
			public T Element;
			public bool Active;
			public bool Solo;
		}

		[SerializeField] private List<FilterEntry> entries;
		private List<int> excludedCountPerFilter;

		public int GetExcluded(int index)
		{
			if (index >= 0 && index < excludedCountPerFilter.Count) return excludedCountPerFilter[index];
			return 0;
		}

		protected FilterBase()
		{
			this.entries = new List<FilterEntry>();
			excludedCountPerFilter = new List<int>();
		}

		protected FilterBase(List<FilterEntry> list)
		{
			if (list != null)
			{
				entries = list;
				excludedCountPerFilter = new List<int>(new int[list.Count]);
			}
			else
			{
				list = new List<FilterEntry>();
				excludedCountPerFilter = new List<int>();
			}
		}

		public int Count => entries.Count;
		public T this[int index] => entries[index].Element;
		public bool IsActiveAtIndex(int index) => entries[index].Active;
		public bool IsSoloAtIndex(int index) => entries[index].Solo;


		public bool IsActive(T element)
		{
			for (var i = 0; i < entries.Count; i++)
			{
				if (entries[i].Equals(element))
					return entries[i].Active;
			}

			return false;
		}

		public int GetActiveCount() => entries.Count(e => e.Active);

		public abstract string GetLabel(int index);

		public bool TryGetIndex(T element, out int index)
		{
			for (var i = 0; i < entries.Count; i++)
			{
				if (entries[i].Element.Equals(element))
				{
					index = i;
					return true;
				}
			}

			index = -1;
			return false;
		}

		public bool Contains(T element) => entries.Any(e => e.Element.Equals(element));

		public void SetActiveAtIndex(int index, bool active)
		{
			if (this.entries[index].Active != active)
			{
				WillChange?.Invoke(this);
				var ex = this.entries[index];
				ex.Active = active;
				entries[index] = ex;
				NotifyConsole(active);
			}
		}

		public void SetActive(T element, bool active)
		{
			if (TryGetIndex(element, out var i))
			{
				SetActiveAtIndex(i, active);
			}
		}

		public void SetSoloAtIndex(int index, bool solo)
		{
			var cur = this.entries[index].Solo;
			if (cur == solo) return;
			WillChange?.Invoke(this);
			var e = entries[index];
			e.Solo = solo;
			// if (solo) e.Active = true;
			entries[index] = e;
			NotifyConsole(solo);
		}

		public void SetSolo(T element, bool solo)
		{
			if (TryGetIndex(element, out var index))
			{
				SetSoloAtIndex(index, solo);
			}
		}

		public virtual void Add(T entry, bool isActive = true, bool isSolo = false)
		{
			if (!entries.Any(e => e.Element.Equals(entry)))
			{
				WillChange?.Invoke(this);
				entries.Add(new FilterEntry() {Element = entry, Active = isActive, Solo = isSolo});
				excludedCountPerFilter.Add(0);
				OnChanged();
				NotifyConsole(isActive);
			}
			else if (isActive && TryGetIndex(entry, out var index))
			{
				SetActiveAtIndex(index, true);
			}
		}

		public virtual void Remove(int index)
		{
			WillChange?.Invoke(this);
			entries.RemoveAt(index);
			excludedCountPerFilter.RemoveAt(index);
			OnChanged();
			NotifyConsole(false);
		}

		public virtual void Clear()
		{
			if (Count > 0)
			{
				WillChange?.Invoke(this);
				entries.Clear();
				excludedCountPerFilter.Clear();
				OnChanged();
				NotifyConsole(false);
			}
		}

		public bool HasAnySolo()
		{
			return entries.Any(s => s.Solo);
		}

		public void BeforeFilter()
		{
			for (var index = 0; index < excludedCountPerFilter.Count; index++)
			{
				excludedCountPerFilter[index] = 0;
			}
		}

		public FilterResult Filter(string message, int mask, int row, LogEntryInfo info)
		{
			var res = OnFilter(message, mask, row, info);
			if (res.result == FilterResult.Exclude && res.index >= 0)
			{
				excludedCountPerFilter[res.index] += 1;
			}
			return res.result;
		}

		protected virtual (FilterResult result, int index) OnFilter(string message, int mask, int row, LogEntryInfo info)
		{
			for (var index = 0; index < Count; index++)
			{
				var ex = this[index];
				if ((IsActiveAtIndex(index) || IsSoloAtIndex(index)) && MatchFilter(ex, index, message, mask, row, info)) // ex.Equals(info.file))
				{
					var res = IsSoloAtIndex(index) ? FilterResult.Solo : FilterResult.Exclude;
					// Debug.Log((res == FilterResult.Solo) + ", " + Path.GetFileName(info.file));
					return (res, index);
				}
			}

			return (FilterResult.Keep, -1);
		}

		protected abstract bool MatchFilter(T entry, int index, string message, int mask, int row, LogEntryInfo info);

		public abstract void AddLogEntryContextMenuItems(GenericMenu menu, LogEntryInfo clickedLog);

		
		protected virtual void OnChanged()
		{
		}


		public void OnGUI()
		{
			var header = ObjectNames.NicifyVariableName(GetType().Name);
			var key = "ConsoleFilter" + header;

			header += " (" + GetActiveCount() + "/" + Count + ")";
			if (!Enabled) header += " - Disabled";
			var foldout = SessionState.GetBool(key, true);
			foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, new GUIContent(header), EditorStyles.foldoutHeader, ShowOptionsContextMenu);

			void ShowOptionsContextMenu(Rect r)
			{
				var menu = new GenericMenu();
				if (Enabled)
					menu.AddItem(new GUIContent("Disable"), true, () => Enabled = false);
				else
					menu.AddItem(new GUIContent("Enable"), false, () => Enabled = true);
				menu.AddSeparator(string.Empty);
				menu.AddItem(new GUIContent("Clear"), false, Clear);
				menu.DropDown(r);
			}

			if (Event.current.type == EventType.MouseDown)
			{
				var lr = GUILayoutUtility.GetLastRect();
				if (Event.current.button == 1 && lr.Contains(Event.current.mousePosition))
				{
					var r = new Rect(Event.current.mousePosition, Vector2.zero);
					ShowOptionsContextMenu(r);
				}
			}

			SessionState.SetBool(key, foldout);

			if (foldout)
			{
				var anySolo = HasAnySolo();

				EditorGUI.indentLevel++;
				var iconWidth = GUILayout.Width(15);
				var statsStyle = new GUIStyle(EditorStyles.label);
				statsStyle.alignment = TextAnchor.MiddleRight;
				
				for (var index = 0; index < Count; index++)
				{
					var prevColor = GUI.color;
					var isSolo = IsSoloAtIndex(index);
					var isActive = IsActiveAtIndex(index);
					if (anySolo && !isSolo)
					{
						GUI.color = ConsoleFilterExtensions.DisabledColor;
					}

					var file = this[index];
					var label = GetLabel(index);
					using (new GUILayout.HorizontalScope())
					{
						GUILayout.Space(5);
						// var ex = EditorGUILayout.ToggleLeft(new GUIContent(label, file.ToString()), isActive);
						// SetActiveAtIndex(index, ex);

						var eye = new GUIContent(isActive ? Textures.EyeClosed : Textures.EyeOpen);
						// eye.text = " 0";
						using (new GUIColorScope(Color.white))
						{
							var width = iconWidth;// GUILayout.Width(30);
							var res = GUILayout.Toggle(isActive, eye, Styles.FilterToggleButton(), width);
							if (res != isActive)
							{
								SetActiveAtIndex(index, res);
							}
						}
						GUILayout.Space(3);

						using (new GUIColorScope(isSolo ? new Color(1, .5f, .5f) : GUI.color))
						{
							var res = GUILayout.Toggle(isSolo, new GUIContent(Textures.Solo), Styles.FilterToggleButton(), iconWidth);
							if (res != isSolo)
							{
								SetSoloAtIndex(index, res);
							}
						}
						

						GUILayout.Space(-15);
						
						EditorGUILayout.LabelField(new GUIContent(label, file.ToString()), GUILayout.ExpandWidth(true));

						var excluded = GetExcluded(index);// ConsoleFilter.GetStats(this);
						if (excluded > 0)
						{
							using (new GUIColorScope(new Color(1, 1, 1, .7f)))
							{
								GUILayout.Label("hides " + excluded, statsStyle, GUILayout.Width(100));
							}
						}

						if (GUILayout.Button(new GUIContent(Textures.Remove), Styles.FilterToggleButton(), iconWidth))
						{
							Remove(index);
							index -= 1;
						}

						GUILayout.Space(7f);
					}

					GUI.color = prevColor;
				}

				EditorGUI.indentLevel--;
			}

			EditorGUILayout.EndFoldoutHeaderGroup();
		}

		public event Action<IConsoleFilter> WillChange, HasChanged;

		private void NotifyConsole(bool activateFilteringIfDisabled)
		{
			HasChanged?.Invoke(this);

			if (Enabled && ConsoleFilter.Contains(this))
			{
				ConsoleFilter.MarkDirty();
				if (activateFilteringIfDisabled && !ConsoleFilter.enabled)
					ConsoleFilter.enabled = true;
			}
		}

		protected void AddContextMenuItem(GenericMenu menu, string text, T element)
		{
			var active = TryGetIndex(element, out var index);
			if (active) active = IsActiveAtIndex(index);

			menu.AddItem(new GUIContent(text), active, () =>
			{
				if (!active)
				{
					Add(element);
				}
				else
				{
					SetActiveAtIndex(index, ConsoleFilter.enabled);
				}
			});
		}
	}
}