﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Needle.Demystify
{
	[Serializable]
	public abstract class BaseFilterWithActiveState<T> : IConsoleFilter
	{
		private bool _isEnabled = true;
		public bool Enabled
		{
			get => _isEnabled;
			set
			{
				if (value == _isEnabled) return;
				_isEnabled = value;
				ConsoleFilter.MarkDirty();
			}
		}

		private List<T> excluded = new List<T>();
		private List<bool> active = new List<bool>();

		public int Count => excluded.Count;
		public T this[int index] => excluded[index];
		public bool IsActive(int index) => active[index];

		public int GetActiveCount() => active.Count(e => e);
		public abstract string GetLabel(int index);

		public bool TryGetIndex(T element, out int index)
		{
			for (var i = 0; i < excluded.Count; i++) 
			{
				if (excluded[i].Equals(element))
				{
					index = i;
					return true;
				}
			}

			index = -1;
			return false;
		}

		public bool Contains(T element) => excluded.Contains(element);

		public bool IsActive(T element)
		{
			for (var i = 0; i < excluded.Count; i++)
			{
				if (excluded[i].Equals(element))
					return active[i];
			}

			return false;
		}

		public void SetActive(int index, bool active)
		{
			if (this.active[index] != active)
			{
				this.active[index] = active;
				if (Enabled && ConsoleFilter.Contains(this))
					ConsoleFilter.MarkDirty();
			}
		}

		public virtual void Add(T entry, bool isActive = true)
		{
			if (!excluded.Contains(entry))
			{
				excluded.Add(entry);
				active.Add(isActive);
				OnChanged();
				if (Enabled && ConsoleFilter.Contains(this))
					ConsoleFilter.MarkDirty();
			}
		}

		public virtual void Remove(int index)
		{
			excluded.RemoveAt(index);
			active.RemoveAt(index);
			OnChanged();
			if (Enabled && ConsoleFilter.Contains(this))
				ConsoleFilter.MarkDirty();
		}

		public abstract bool Exclude(string message, int mask, int row, LogEntryInfo info);

		public abstract void AddLogEntryContextMenuItems(GenericMenu menu, LogEntryInfo clickedLog);

		protected virtual void OnChanged(){}

		public void OnGUI()
		{
			var header = ObjectNames.NicifyVariableName(GetType().Name);
			var key = "ConsoleFilter" + header;
			
			header += " [" + GetActiveCount() + "/" + Count + "]";
			var foldout = SessionState.GetBool(key, true);
			foldout = EditorGUILayout.BeginFoldoutHeaderGroup(foldout, header);
			// foldout = EditorGUILayout.Foldout(foldout, header);
			SessionState.SetBool(key, foldout);
			Enabled = foldout;

			if(foldout)
			{
				EditorGUI.indentLevel++;
				for (var index = 0; index < Count; index++)
				{
					var file = this[index];
					var label = GetLabel(index);
					using (new GUILayout.HorizontalScope())
					{
						var ex = EditorGUILayout.ToggleLeft(new GUIContent(label, file.ToString()), IsActive(index));
						SetActive(index, ex);
						if (GUILayout.Button("x", GUILayout.Width(20)))
						{
							Remove(index);
							index -= 1;
						}
					}
				}
				EditorGUI.indentLevel--;
			}
			
			EditorGUILayout.EndFoldoutHeaderGroup();
		}
	}
}