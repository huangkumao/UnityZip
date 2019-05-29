//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2019 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Text;
using System.IO;

/// <summary>
/// Font maker lets you create font prefabs with a single click of a button.
/// </summary>

public class UIFontMaker : EditorWindow
{
	[DoNotObfuscateNGUI] public enum FontType
	{
		GeneratedBitmap,	// Bitmap font, created from a dynamic font using FreeType
		ImportedBitmap,		// Imported bitmap font, created using BMFont or another external tool
		Dynamic,			// Dynamic font, used as-is
	}

	[DoNotObfuscateNGUI] public enum Create
	{
		None,
		Bitmap,		// Bitmap font, created from a dynamic font using FreeType
		Import,		// Imported bitmap font
		Dynamic,	// Dynamic font, used as-is
	}

	[DoNotObfuscateNGUI] public enum CharacterMap
	{
		Numeric,	// 0 through 9
		Ascii,		// Character IDs 32 through 127
		Latin,		// Ascii + various accented character such as "é"
		Custom,		// Only explicitly specified characters will be included
	}

	[System.NonSerialized] FontType mType = FontType.GeneratedBitmap;
	[System.NonSerialized] int mFaceIndex = 0;

	/// <summary>
	/// Type of character map chosen for export.
	/// </summary>

	static CharacterMap characterMap
	{
		get { return (CharacterMap)NGUISettings.GetInt("NGUI Character Map", (int)CharacterMap.Ascii); }
		set { NGUISettings.SetInt("NGUI Character Map", (int)value); }
	}

	/// <summary>
	/// Update all labels associated with this font.
	/// </summary>

	void MarkAsChanged ()
	{
		var obj = NGUISettings.FMFont != null ? NGUISettings.FMFont : NGUISettings.BMFont;

		if (obj != null)
		{
			List<UILabel> labels = NGUIEditorTools.FindAll<UILabel>();

			foreach (UILabel lbl in labels)
			{
				if (lbl.ambigiousFont == obj)
				{
					lbl.ambigiousFont = null;
					lbl.ambigiousFont = obj;
				}
			}
		}
	}

	/// <summary>
	/// Atlas selection callback.
	/// </summary>

	void OnSelectAtlas (Object obj)
	{
		// Legacy atlas support
		if (obj != null && obj is GameObject) obj = (obj as GameObject).GetComponent<UIAtlas>();

		var atlas = obj as INGUIAtlas;

		if (NGUISettings.atlas != atlas)
		{
			NGUISettings.atlas = atlas;
			Repaint();
		}
	}

	/// <summary>
	/// Refresh the window on selection.
	/// </summary>

	void OnSelectionChange () { Repaint(); }

	void OnUnityFont (Object obj)
	{
		NGUISettings.FMFont = (Font)obj;
		Repaint();
	}

	/// <summary>
	/// Draw the UI for this tool.
	/// </summary>

	void OnGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		GUILayout.Space(3f);

		NGUIEditorTools.DrawHeader("Input", true);
		NGUIEditorTools.BeginContents(false);

		GUILayout.BeginHorizontal();
		mType = (FontType)EditorGUILayout.EnumPopup("Type", mType, GUILayout.MinWidth(200f));
		NGUIEditorTools.DrawPadding();
		GUILayout.EndHorizontal();
		Create create = Create.None;

		if (mType == FontType.ImportedBitmap)
		{
			NGUISettings.fontData = EditorGUILayout.ObjectField("Font Data", NGUISettings.fontData, typeof(TextAsset), false) as TextAsset;
			NGUISettings.fontTexture = EditorGUILayout.ObjectField("Texture", NGUISettings.fontTexture, typeof(Texture2D), false, GUILayout.Width(140f)) as Texture2D;
			NGUIEditorTools.EndContents();

			// Draw the atlas selection only if we have the font data and texture specified, just to make it easier
			EditorGUI.BeginDisabledGroup(NGUISettings.fontData == null || NGUISettings.fontTexture == null);
			{
				NGUIEditorTools.DrawHeader("Output", true);
				NGUIEditorTools.BeginContents(false);
				ComponentSelector.Draw(NGUISettings.atlas, OnSelectAtlas, false);
				NGUIEditorTools.EndContents();
			}
			EditorGUI.EndDisabledGroup();

			if (NGUISettings.fontData == null)
			{
				EditorGUILayout.HelpBox("To create a font from a previously exported FNT file, you need to use BMFont on " +
					"Windows or your choice of Glyph Designer or the less expensive bmGlyph on the Mac.\n\n" +
					"Either of these tools will create a FNT file for you that you will drag & drop into the field above.", MessageType.Info);
			}
			else if (NGUISettings.fontTexture == null)
			{
				EditorGUILayout.HelpBox("When exporting your font, you should get two files: the FNT, and the texture. Only one texture can be used per font.", MessageType.Info);
			}
			else if (NGUISettings.atlas == null)
			{
				EditorGUILayout.HelpBox("You can create a font that doesn't use a texture atlas. This will mean that the text " +
					"labels using this font will generate an extra draw call.\n\nIf you do specify an atlas, the font's texture will be added to it automatically.", MessageType.Info);
			}

			EditorGUI.BeginDisabledGroup(NGUISettings.fontData == null || NGUISettings.fontTexture == null);
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(20f);
				if (GUILayout.Button("Create the Font")) create = Create.Import;
				GUILayout.Space(20f);
				GUILayout.EndHorizontal();
			}
			EditorGUI.EndDisabledGroup();
		}
		else
		{
			GUILayout.BeginHorizontal();
			if (NGUIEditorTools.DrawPrefixButton("Source"))
				ComponentSelector.Show<Font>(OnUnityFont, new string[] { ".ttf", ".otf" });

			Font ttf = EditorGUILayout.ObjectField(NGUISettings.FMFont, typeof(Font), false) as Font;
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			{
				NGUISettings.FMSize = EditorGUILayout.IntField("Size", NGUISettings.FMSize, GUILayout.Width(120f));

				if (mType == FontType.Dynamic)
				{
					NGUISettings.fontStyle = (FontStyle)EditorGUILayout.EnumPopup(NGUISettings.fontStyle);
					NGUIEditorTools.DrawPadding();
				}
			}
			GUILayout.EndHorizontal();

			// Choose the font style if there are multiple faces present
			if (mType == FontType.GeneratedBitmap)
			{
				if (!FreeType.isPresent)
				{
					string filename = (Application.platform == RuntimePlatform.WindowsEditor) ? "FreeType.dll" : "FreeType.dylib";
					EditorGUILayout.HelpBox(filename + " is missing", MessageType.Error);

					GUILayout.BeginHorizontal();
					GUILayout.Space(20f);

					if (GUILayout.Button("Find " + filename))
					{
						string dllPath = EditorUtility.OpenFilePanel("Find " + filename, NGUISettings.currentPath,
							(Application.platform == RuntimePlatform.WindowsEditor) ? "dll" : "dylib");

						if (!string.IsNullOrEmpty(dllPath))
						{
							if (System.IO.Path.GetFileName(dllPath) == filename)
							{
								NGUISettings.currentPath = System.IO.Path.GetDirectoryName(dllPath);
								NGUISettings.pathToFreeType = dllPath;
							}
							else Debug.LogError("The library must be named '" + filename + "'");
						}
					}
					GUILayout.Space(20f);
					GUILayout.EndHorizontal();
				}
				else if (ttf != null)
				{
					string[] faces = FreeType.GetFaces(ttf);

					if (faces != null)
					{
						if (mFaceIndex >= faces.Length) mFaceIndex = 0;

						if (faces.Length > 1)
						{
							GUILayout.Label("Style", EditorStyles.boldLabel);
							for (int i = 0; i < faces.Length; ++i)
							{
								GUILayout.BeginHorizontal();
								GUILayout.Space(10f);
								if (DrawOption(i == mFaceIndex, " " + faces[i]))
									mFaceIndex = i;
								GUILayout.EndHorizontal();
							}
						}
					}

					NGUISettings.fontKerning = EditorGUILayout.Toggle("Kerning", NGUISettings.fontKerning);

					GUILayout.Label("Characters", EditorStyles.boldLabel);

					CharacterMap cm = characterMap;

					GUILayout.BeginHorizontal(GUILayout.Width(100f));
					GUILayout.BeginVertical();
					GUI.changed = false;
					if (DrawOption(cm == CharacterMap.Numeric, " Numeric")) cm = CharacterMap.Numeric;
					if (DrawOption(cm == CharacterMap.Ascii, " ASCII")) cm = CharacterMap.Ascii;
					if (DrawOption(cm == CharacterMap.Latin, " Latin")) cm = CharacterMap.Latin;
					if (DrawOption(cm == CharacterMap.Custom, " Custom")) cm = CharacterMap.Custom;
					if (GUI.changed) characterMap = cm;
					GUILayout.EndVertical();

					EditorGUI.BeginDisabledGroup(cm != CharacterMap.Custom);
					{
						if (cm != CharacterMap.Custom)
						{
							string chars = "";

							if (cm == CharacterMap.Ascii)
							{
								for (int i = 33; i < 127; ++i)
									chars += System.Convert.ToChar(i);
							}
							else if (cm == CharacterMap.Numeric)
							{
								chars = "0123456789";
							}
							else if (cm == CharacterMap.Latin)
							{
								for (int i = 33; i < 127; ++i)
									chars += System.Convert.ToChar(i);

								for (int i = 161; i < 256; ++i)
									chars += System.Convert.ToChar(i);
							}

							NGUISettings.charsToInclude = chars;
						}

						GUI.changed = false;

						string text = NGUISettings.charsToInclude;

						if (cm == CharacterMap.Custom)
						{
							text = EditorGUILayout.TextArea(text, GUI.skin.textArea,
								GUILayout.Height(80f), GUILayout.Width(Screen.width - 100f));
						}
						else
						{
							GUILayout.Label(text, GUI.skin.textArea,
								GUILayout.Height(80f), GUILayout.Width(Screen.width - 100f));
						}

						if (GUI.changed)
						{
							string final = "";

							for (int i = 0; i < text.Length; ++i)
							{
								char c = text[i];
								if (c < 33) continue;
								string s = c.ToString();
								if (!final.Contains(s)) final += s;
							}

							if (final.Length > 0)
							{
								char[] chars = final.ToCharArray();
								System.Array.Sort(chars);
								final = new string(chars);
							}
							else final = "";

							NGUISettings.charsToInclude = final;
						}
					}
					EditorGUI.EndDisabledGroup();
					GUILayout.EndHorizontal();
				}
			}
			NGUIEditorTools.EndContents();

			if (mType == FontType.Dynamic)
			{
				EditorGUI.BeginDisabledGroup(ttf == null);
				GUILayout.BeginHorizontal();
				GUILayout.Space(20f);
				if (GUILayout.Button("Create the Font")) create = Create.Dynamic;
				GUILayout.Space(20f);
				GUILayout.EndHorizontal();
				EditorGUI.EndDisabledGroup();

				// Helpful info
				if (ttf == null)
				{
					EditorGUILayout.HelpBox("You don't have to create a NGUIFont to use dynamic fonts. You can just reference the Unity Font directly on the label.", MessageType.Info);
				}
				EditorGUILayout.HelpBox("Please note that dynamic fonts can't be made a part of an atlas, and using dynamic fonts will result in at least one extra draw call.", MessageType.Warning);
			}
			else
			{
				bool isBuiltIn = (ttf != null) && string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(ttf));

				// Draw the atlas selection only if we have the font data and texture specified, just to make it easier
				EditorGUI.BeginDisabledGroup(ttf == null || isBuiltIn || !FreeType.isPresent);
				{
					NGUIEditorTools.DrawHeader("Output", true);
					NGUIEditorTools.BeginContents(false);
					ComponentSelector.Draw(NGUISettings.atlas, OnSelectAtlas, false);
					NGUIEditorTools.EndContents();

					if (ttf == null)
					{
						EditorGUILayout.HelpBox("You can create a bitmap font by specifying a dynamic font to use as the source.", MessageType.Info);
					}
					else if (isBuiltIn)
					{
						EditorGUILayout.HelpBox("You chose an embedded font. You can't create a bitmap font from an embedded resource.", MessageType.Warning);
					}
					else if (NGUISettings.atlas == null)
					{
						EditorGUILayout.HelpBox("You can create a font that doesn't use a texture atlas. This will mean that the text " +
							"labels using this font will generate an extra draw call.\n\nIf you do specify an atlas, the font's texture will be added to it automatically.", MessageType.Info);
					}

					GUILayout.BeginHorizontal();
					GUILayout.Space(20f);
					if (GUILayout.Button("Create the Font")) create = Create.Bitmap;
					GUILayout.Space(20f);
					GUILayout.EndHorizontal();
				}
				EditorGUI.EndDisabledGroup();
			}
		}

		if (create == Create.None) return;

		// Open the "Save As" file dialog
		var path = EditorUtility.SaveFilePanelInProject("Save As", "New Font.asset", "asset", "Save font as...", NGUISettings.currentPath);

		if (string.IsNullOrEmpty(path)) return;
		NGUISettings.currentPath = System.IO.Path.GetDirectoryName(path);

		// Load the font's asset
		var asset = AssetDatabase.LoadAssetAtPath<NGUIFont>(path);

		string fontName;

		// Font doesn't exist yet
		if (asset == null)
		{
			if (asset == null) asset = ScriptableObject.CreateInstance<NGUIFont>();

			fontName = path.Replace(".asset", "");
			fontName = fontName.Substring(path.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
		}
		else fontName = asset.name;

		if (create == Create.Dynamic)
		{
			asset.atlas = null;
			asset.dynamicFont = NGUISettings.FMFont;
			asset.dynamicFontStyle = NGUISettings.fontStyle;
			asset.defaultSize = NGUISettings.FMSize;
		}
		else if (create == Create.Import)
		{
			Material mat = null;

			if (NGUISettings.atlas != null)
			{
				// Add the font's texture to the atlas
				UIAtlasMaker.AddOrUpdate(NGUISettings.atlas, NGUISettings.fontTexture);
			}
			else
			{
				// Create a material for the font
				string matPath = path.Replace(".asset", ".mat");
				mat = AssetDatabase.LoadAssetAtPath(matPath, typeof(Material)) as Material;

				// If the material doesn't exist, create it
				if (mat == null)
				{
					Shader shader = Shader.Find("Unlit/Transparent Colored");
					mat = new Material(shader);

					// Save the material
					AssetDatabase.CreateAsset(mat, matPath);
					AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

					// Load the material so it's usable
					mat = AssetDatabase.LoadAssetAtPath(matPath, typeof(Material)) as Material;
				}

				mat.mainTexture = NGUISettings.fontTexture;
			}

			asset.dynamicFont = null;
			BMFontReader.Load(asset.bmFont, asset.name, NGUISettings.fontData.bytes);

			if (NGUISettings.atlas == null)
			{
				asset.atlas = null;
				asset.material = mat;
			}
			else
			{
				asset.spriteName = NGUISettings.fontTexture.name;
				asset.atlas = NGUISettings.atlas;
			}

			NGUISettings.FMSize = asset.defaultSize;
		}
		else if (create == Create.Bitmap)
		{
			// Create the bitmap font
			BMFont bmFont;
			Texture2D tex;

			if (FreeType.CreateFont(
				NGUISettings.FMFont,
				NGUISettings.FMSize, mFaceIndex,
				NGUISettings.fontKerning,
				NGUISettings.charsToInclude, 1, out bmFont, out tex))
			{
				asset.bmFont = bmFont;
				tex.name = fontName;

				if (NGUISettings.atlas != null)
				{
					// Add this texture to the atlas and destroy it
					UIAtlasMaker.AddOrUpdate(NGUISettings.atlas, tex);
					NGUITools.DestroyImmediate(tex);
					NGUISettings.fontTexture = null;
					tex = null;

					asset.atlas = NGUISettings.atlas;
					asset.spriteName = fontName;
				}
				else
				{
					var texPath = path.Replace(".asset", ".png");
					var matPath = path.Replace(".asset", ".mat");

					var png = tex.EncodeToPNG();
					FileStream fs = File.OpenWrite(texPath);
					fs.Write(png, 0, png.Length);
					fs.Close();

					// See if the material already exists
					Material mat = AssetDatabase.LoadAssetAtPath(matPath, typeof(Material)) as Material;

					// If the material doesn't exist, create it
					if (mat == null)
					{
						Shader shader = Shader.Find("Unlit/Transparent Colored");
						mat = new Material(shader);

						// Save the material
						AssetDatabase.CreateAsset(mat, matPath);
						AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

						// Load the material so it's usable
						mat = AssetDatabase.LoadAssetAtPath(matPath, typeof(Material)) as Material;
					}
					else AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

					// Re-load the texture
					tex = AssetDatabase.LoadAssetAtPath(texPath, typeof(Texture2D)) as Texture2D;

					// Assign the texture
					mat.mainTexture = tex;
					NGUISettings.fontTexture = tex;

					asset.atlas = null;
					asset.material = mat;
				}
			}
			else return;
		}

		if (asset != null)
		{
			AssetDatabase.CreateAsset(asset, path);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

			NGUISettings.FMFont = null;
			NGUISettings.BMFont = asset;
		}

		MarkAsChanged();
		Selection.activeObject = asset;
	}

	/// <summary>
	/// Helper function that draws a slightly padded toggle
	/// </summary>

	static public bool DrawOption (bool state, string text, params GUILayoutOption[] options)
	{
		GUILayout.BeginHorizontal();
		GUILayout.Space(10f);
		bool val = GUILayout.Toggle(state, text, EditorStyles.radioButton, options);
		GUILayout.EndHorizontal();
		return val;
	}
}
