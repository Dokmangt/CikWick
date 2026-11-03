using System;
using System.Collections.Generic;
using UnityEngine;

namespace FolderColor.Editor
{
	public class FolderColorDatabase : ScriptableObject
	{
		[HideInInspector]
		public FolderColorDataControl.FolderColorMode Mode = FolderColorDataControl.FolderColorMode.Global;
		[HideInInspector]
		public string UserId;

		[HideInInspector]
		public List<IconColorMix> IconEntries = new();

		// Old
		[HideInInspector]
		public List<FolderColorDataControl.FolderColorEntry> Entries = new();
	}

	[Serializable]
	public class IconColorMix
	{
		public IconColorMix(string guid, string path, Color color, 
			bool highlightName, bool highlightChildren)
		{
			Guid = guid;
			IconPath = path;
			IconColor = color;
			HighlightName = highlightName;
			HighlightChildren = highlightChildren;
		}
		
		public IconColorMix(string guid, string path, Color color)
		{
			Guid = guid;
			IconPath = path;
			IconColor = color;
			HighlightName = false;
			HighlightChildren = false;
		}
		
		public string Guid;
		public string IconPath;
		public Color IconColor;
		public bool HighlightName;
		public bool HighlightChildren;
	}
}