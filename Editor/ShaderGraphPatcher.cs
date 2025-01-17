using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class ShaderGraphPatcherWindow : EditorWindow {

	enum UVTypeChoiceEnum : int {
		Default = 0,
		Min16uint = 1, 
		Min16int = 2,
		Uint = 3,
		Int = 4
	}
	const string UV_TYPE_DEFAULT = "Default";
	List<string> uvTypeChoices = new List<string>() {
		UV_TYPE_DEFAULT,
		"Min16uint",
		"Min16int",
		"Uint",
		"Int"
	};
	Dictionary<UVTypeChoiceEnum, string> uvTypePatchLUT = new Dictionary<UVTypeChoiceEnum, string>() {
		{UVTypeChoiceEnum.Min16uint, "min16uint" },
		{UVTypeChoiceEnum.Min16int, "min16int" },
		{UVTypeChoiceEnum.Uint, "uint" },
		{UVTypeChoiceEnum.Int, "int" },
	};

	[MenuItem("Tools/Shader Graph Patcher")]
	public static void Show() {
		ShaderGraphPatcherWindow wnd = GetWindow<ShaderGraphPatcherWindow>();
		wnd.titleContent = new GUIContent("ShaderGraphPatcherWindow");
	}

	public void CreateGUI() {
		VisualElement root = rootVisualElement;

		Label header = new Label("ShaderGraph-shader / ShaderGraph patch utility.\nHover over elements for tooltips.\nThe patched shader(graph) will have the \"_Patched\" suffix.\nThe original shader(graph) will not be patched.\n\n");
		root.Add(header);

		var shaderGraphField = new ObjectField();
		shaderGraphField.tooltip = "Drag & Drop a ShaderGraph here.";
		shaderGraphField.objectType = typeof(UnityEngine.Object);//objectField.objectType = typeof(ShaderGraphAsset);
		shaderGraphField.label = "ShaderGraph:";
		root.Add(shaderGraphField);

		root.Add(new Label("and/or"));

		var shaderField = new ObjectField();
		shaderField.tooltip = "Drag & Drop a Shader here.";
		shaderField.objectType = shaderField.objectType = typeof(Shader);
		shaderField.label = "Shader:";
		root.Add(shaderField);

		root.Add(new Label("\nSettings:\n"));

		var patchSVInstanceID = new Toggle("Patch InstanceID = SV_InstanceID");
		patchSVInstanceID.tooltip = "The patched shader will use SV_InstanceID instead of unity_InstanceID when using the InstanceID-node. This breaks instancing support for the shader, but in return allows using the shader with IndexedIndirect render functions. i.e. Graphics.RenderPrimitivesIndexedIndirect";
		patchSVInstanceID.value = true;
		root.Add(patchSVInstanceID);

		var patchNoInterpolation = new Toggle("Patch \"NoInterpolation\" CustomInterpolators");
		patchNoInterpolation.tooltip = "Adds \"nointerpolation\" attribute to CustomInterpolators that contain \"NoInterpolation\" in their name. i.e. NoInterpolationMyData will get the \"nointerpolation\" attribute. IMPORTANT: These CustomInterpolators MUST use Vector4/float4 as unity will pack all CustomInterpolators into float4 to save space. If you don't use Vector4 for \"NoInterpolation\" interpolators, other interpolators might also get the \"nointerpolation\" attribute because of the packing!";
		patchNoInterpolation.value = true;
		root.Add(patchNoInterpolation);

		var patchUV0Type = new DropdownField("Patch UV0", uvTypeChoices, 0);
		patchUV0Type.tooltip = "Patch UV0 input type";
		root.Add(patchUV0Type);
		var patchUV1Type = new DropdownField("Patch UV1", uvTypeChoices, 0);
		patchUV1Type.tooltip = "Patch UV1 input type";
		root.Add(patchUV1Type);
		var patchUV2Type = new DropdownField("Patch UV2", uvTypeChoices, 0);
		patchUV2Type.tooltip = "Patch UV2 input type";
		root.Add(patchUV2Type);
		var patchUV3Type = new DropdownField("Patch UV3", uvTypeChoices, 0);
		patchUV3Type.tooltip = "Patch UV3 input type";
		root.Add(patchUV3Type);

		root.Add(new Label("\n"));

		Button patchButton = new Button(() => {
			if (shaderGraphField.value != null) {
				Patch(new PatchData(
					shaderGraphField.value,
					shaderField.value != null ? shaderField.value as Shader : null,
					patchSVInstanceID.value,
					patchNoInterpolation.value,
					GetUVTypeChoiceForString(patchUV0Type.value),
					GetUVTypeChoiceForString(patchUV1Type.value),
					GetUVTypeChoiceForString(patchUV2Type.value),
					GetUVTypeChoiceForString(patchUV3Type.value)
				));
			}
		});
		patchButton.tooltip = "Wubba Lubba Dub Dub!"; // Only when i look at the code for this patcher ;)
		patchButton.name = "patchButton";
		patchButton.text = "Patch";
		root.Add(patchButton);
	}

	UVTypeChoiceEnum GetUVTypeChoiceForString(string s) {
		if(	Enum.TryParse<UVTypeChoiceEnum>(s, out UVTypeChoiceEnum result)) {
			return result;
		} else {
			throw new System.Exception("UVType " + s + "not found/implemented");
		}
	}

	struct PatchData {
		public UnityEngine.Object shaderGraph;
		public UnityEngine.Shader shader;
		public bool patchSVInstanceID;
		public bool patchNoInterpolation;
		public UVTypeChoiceEnum uv0;
		public UVTypeChoiceEnum uv1;
		public UVTypeChoiceEnum uv2;
		public UVTypeChoiceEnum uv3;

		public PatchData(UnityEngine.Object shaderGraph, UnityEngine.Shader shader, bool patchSVInstanceID, bool patchNoInterpolation, UVTypeChoiceEnum uv0, UVTypeChoiceEnum uv1, UVTypeChoiceEnum uv2, UVTypeChoiceEnum uv3) {
			this.shaderGraph = shaderGraph;
			this.shader = shader;
			this.patchSVInstanceID = patchSVInstanceID;
			this.patchNoInterpolation = patchNoInterpolation;
			this.uv0 = uv0;
			this.uv1 = uv1;
			this.uv2 = uv2;
			this.uv3 = uv3;
		}

	}
	void Patch(PatchData data) {
		if(data.shaderGraph != null) {
			PatchShaderGraph(data);
		}
		if(data.shader != null) {
			PatchShader(data);
		}
	}

	/// <summary>
	/// Fixes the shader so it can be used with Indexed Indirect Rendering ( SV_InstanceID, nointerpolation, etc.)
	/// </summary>
	/// <param name="shader"></param>
	void PatchShader(PatchData data) {
		Debug.Log("Patched Shader " + data.shader.name);
		string pathToShader = AssetDatabase.GetAssetPath(data.shader as Shader);

		List<string> fixedShader = FixShader(pathToShader, data);

		string pathToFixedShader = pathToShader.Remove(pathToShader.Length - ".shader".Length);
		pathToFixedShader += "_Patched.shader";
		System.IO.File.WriteAllLines(pathToFixedShader, fixedShader);
		AssetDatabase.Refresh();
	}

	void PatchShaderGraph(PatchData patchData) {
		Debug.Log("Patched ShaderGraph  " + patchData.shaderGraph.name);
		//Get shader from shadergraph asset
		string pathToShaderGraph = AssetDatabase.GetAssetPath(patchData.shaderGraph);
		string shader = GenerateShaderCodeFromShaderGraphAsset(pathToShaderGraph);

		//Create a shader file from the shader that was contained in the shadergraph
		string pathToFixedShader = pathToShaderGraph.Remove(pathToShaderGraph.LastIndexOf("."));
		pathToFixedShader += "_Patched.shader";
		System.IO.File.WriteAllText(pathToFixedShader, shader);

		//Fix shader
		List<string> fixedShader = FixShader(pathToFixedShader, patchData);
		System.IO.File.WriteAllLines(pathToFixedShader, fixedShader);

		AssetDatabase.Refresh();
	}

	List<string> FixShader(string pathToShader, PatchData patchData) {
		string[] lines = System.IO.File.ReadAllLines(pathToShader);
		List<string> newLines = new List<string>();

		bool handleAttributes = false;
		bool handlePackedVaryings = false;
		bool handleVertexDescriptionInputs = false;
		bool handleBuildVertexDescriptionInputs = false;
		bool handleVertexDescriptionFunction = false;
		//Add "_Fixed" to shader name
		string header = lines[0].Remove(lines[0].LastIndexOf("\""));
		header += "_Patched\"";
		newLines.Add(header);
		//Loop over all lines (except header) of the shader and patch the shader to make use of "SV_InstanceID" instead of "unity_InstanceID" when using shadergraph's InstanceID-node
		//NOTE: The resulting shader will no longer support "classic" instancing!
		for (int i = 1; i < lines.Length; i++) {
			string l = lines[i];

			if (patchData.patchSVInstanceID) {
				// Remove UNITY_ANY_INSTANCING_ENABLED from vertex SV_InstanceID input "struct Attributes"
				if (handleAttributes) {
					if (l.Contains("UNITY_ANY_INSTANCING_ENABLED")) {
						continue;
					}
					if (l.Contains("#endif") && lines[i - 1].Contains("uint instanceID")) {
						continue;
					}
					if (l.Contains("}")) {
						handleAttributes = false;
					}
				}

				// Inject uint InstanceID to "struct VertexDescriptionInputs"
				if (handleVertexDescriptionInputs) {
					if (l.Contains("uint VertexID;") && lines[i + 1].Contains("uint InstanceID;") == false) {
						newLines.Add(l);
						newLines.Add("uint InstanceID;");
						handleVertexDescriptionInputs = false;
						continue;
					}
					if (l.Contains("}")) {
						handleVertexDescriptionInputs = false;
					}
				}

				// Inject InstanceID setup to "BuildVertexDescriptionInputs(...)"
				if (handleBuildVertexDescriptionInputs) {
					if (l.Contains("output.VertexID =") && lines[i + 1].Contains("output.InstanceID = input.instanceID;") == false) {
						newLines.Add(l);
						newLines.Add("output.InstanceID = input.instanceID;");
						handleBuildVertexDescriptionInputs = false;
						continue;
					}
					if (l.Contains("}")) {
						handleBuildVertexDescriptionInputs = false;
					}
				}

				// Use SV_InstanceID/IN.InstanceID instead of unity_InstanceID in "VertexDescriptionFunction(...)"
				if (handleVertexDescriptionFunction) {
					if (l.Contains("UnityGetInstanceID_float(") && lines[i + 1].Contains("= IN.InstanceID;") == false) {
						//Extract random identifier for "_InstanceID". i.e. "UnityGetInstanceID_float(_InstanceID_37fca6eb6d52424a843789dc0304aced_Out_0_Float);" has identifier "_InstanceID_37fca6eb6d52424a843789dc0304aced_Out_0_Float"
						string identifier = l.Remove(0, l.IndexOf("(") + 1);
						identifier = identifier.Remove(identifier.Length - 2);
						//Debug.Log(identifier);
						//newLines.Add(l);
						newLines.Add(identifier + " = IN.InstanceID;");
						handleVertexDescriptionFunction = false;
						continue;
					}
					if (l.Contains("}")) {
						handleVertexDescriptionFunction = false;
					}
				}

				if (l.Contains("struct Attributes")) {
					handleAttributes = true;
				}
				if (l.Contains("struct VertexDescriptionInputs")) {
					handleVertexDescriptionInputs = true;
				}
				if (l.Contains("VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)")) {
					handleBuildVertexDescriptionInputs = true;
				}
				if (l.Contains("VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)")) {
					handleVertexDescriptionFunction = true;
				}
			}

			if (patchData.patchNoInterpolation) {
				// Patch NoInterpolation
				if (handlePackedVaryings) {
					//Flag NoInterpolation4 as "nointerpolation
					if (l.Contains("float4 NoInterpolation") && l.Contains("nointerpolation ") == false) {
						l = l.Insert(l.IndexOf("float4 NoInterpolation"), "nointerpolation ");
					}
					if (l.Contains("}")) {
						handlePackedVaryings = false;
					}
				}

				if (l.Contains("struct PackedVaryings")) {
					handlePackedVaryings = true;
				}
			}

			//Patch UV type
			if(patchData.uv0 != UVTypeChoiceEnum.Default && l.Contains(" : TEXCOORD0;")) {
				l = l.Replace("float", uvTypePatchLUT[patchData.uv0]);
				l = l.Replace("half", uvTypePatchLUT[patchData.uv0]);
			}
			if (patchData.uv1 != UVTypeChoiceEnum.Default && l.Contains(" : TEXCOORD1;")) {
				l = l.Replace("float", uvTypePatchLUT[patchData.uv1]);
				l = l.Replace("half", uvTypePatchLUT[patchData.uv1]);
			}
			if (patchData.uv2 != UVTypeChoiceEnum.Default && l.Contains(" : TEXCOORD2;")) {
				l = l.Replace("float", uvTypePatchLUT[patchData.uv2]);
				l = l.Replace("half", uvTypePatchLUT[patchData.uv2]);
			}
			if (patchData.uv3 != UVTypeChoiceEnum.Default && l.Contains(" : TEXCOORD3;")) {
				l = l.Replace("float", uvTypePatchLUT[patchData.uv3]);
				l = l.Replace("half", uvTypePatchLUT[patchData.uv3]);
			}

			newLines.Add(l);
		}

		return newLines;
	}

	// Taken from https://discussions.unity.com/t/how-to-get-shader-source-code-from-script/839046/3
	private static object GetGraphData(string shaderAssetPath) {
		var importer = AssetImporter.GetAtPath(shaderAssetPath);

		var textGraph = File.ReadAllText(importer.assetPath, System.Text.Encoding.UTF8);
		var graphObjectType = Type.GetType("UnityEditor.Graphing.GraphObject, Unity.ShaderGraph.Editor")!;

		// var graphObject = CreateInstance<GraphObject>();
		var graphObject = ScriptableObject.CreateInstance(graphObjectType);

		graphObject.hideFlags = HideFlags.HideAndDontSave;
		bool isSubGraph;
		var extension = Path.GetExtension(importer.assetPath).Replace(".", "");
		switch (extension) {
			case "shadergraph":
			isSubGraph = false;
			break;
			case "ShaderGraph":
			isSubGraph = false;
			break;
			case "shadersubgraph":
			isSubGraph = true;
			break;
			default:
			throw new Exception($"Invalid file extension {extension}");
		}
		var assetGuid = AssetDatabase.AssetPathToGUID(importer.assetPath);

		// graphObject.graph = new GraphData { assetGuid = assetGuid, isSubGraph = isSubGraph, messageManager = null };
		var graphObject_graphProperty = graphObjectType.GetProperty("graph")!;
		var graphDataType = Type.GetType("UnityEditor.ShaderGraph.GraphData, Unity.ShaderGraph.Editor")!;
		var graphDataInstance = Activator.CreateInstance(graphDataType);
		graphDataType.GetProperty("assetGuid")!.SetValue(graphDataInstance, assetGuid);
		graphDataType.GetProperty("isSubGraph")!.SetValue(graphDataInstance, isSubGraph);
		graphDataType.GetProperty("messageManager")!.SetValue(graphDataInstance, null);
		graphObject_graphProperty.SetValue(graphObject, graphDataInstance);

		// MultiJson.Deserialize(graphObject.graph, textGraph);
		// = MultiJson.Deserialize<JsonObject>(graphObject.graph, textGraph, null, false);
		var multiJsonType = Type.GetType("UnityEditor.ShaderGraph.Serialization.MultiJson, Unity.ShaderGraph.Editor")!;
		var deserializeMethod = multiJsonType.GetMethod("Deserialize")!;
		var descrializeGenericMethod = deserializeMethod.MakeGenericMethod(graphDataType);
		descrializeGenericMethod.Invoke(null, new object[] { graphDataInstance, textGraph, null, false });

		// graphObject.graph.OnEnable();
		graphDataType.GetMethod("OnEnable")!.Invoke(graphDataInstance, null);

		// graphObject.graph.ValidateGraph();
		graphDataType.GetMethod("ValidateGraph")!.Invoke(graphDataInstance, null);

		// return graphData.graph
		return graphDataInstance;
	}

	//Taken from https://discussions.unity.com/t/how-to-get-shader-source-code-from-script/839046/3
	private static string GenerateShaderCodeFromShaderGraphAsset(string shaderAssetPath, string shaderName = null) {
		Type generatorType =
			Type.GetType("UnityEditor.ShaderGraph.Generator, Unity.ShaderGraph.Editor")!;
		Type modeType =
			Type.GetType("UnityEditor.ShaderGraph.GenerationMode, Unity.ShaderGraph.Editor")!;

		shaderName ??= Path.GetFileNameWithoutExtension(shaderAssetPath);

		object graphData = GetGraphData(shaderAssetPath);

		// new Generator(graphData, null, GenerationMode.ForReals, assetName, target:null, assetCollection:null, humanReadable: true);
		object forReals = ((FieldInfo)modeType.GetMember("ForReals")[0]).GetValue(null);
		object generator = Activator.CreateInstance(
			generatorType,
			new object[] { graphData, null, forReals, shaderName, null, null, true }
		);
		object shaderCode = generatorType
			.GetProperty("generatedShader", BindingFlags.Public | BindingFlags.Instance)!
			.GetValue(generator);

		return (string)shaderCode;
	}
}