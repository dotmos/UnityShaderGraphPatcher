# Unity Shadergraph Patcher
![UnityVersion](https://img.shields.io/static/v1?label=unity&message=2022.3%2B&color=blue&style=flat&logo=Unity)
![GitHub](https://img.shields.io/github/license/dotmos/unityshadergraphpatcher)

Patcher utility for Unity Shadergraph shaders.</br>
Adds features to Shadergraph shaders that are currently not available in Shadergraph.</br>
Tested with Shadergraph 14.0.11 and URP 14.0.11 . HDRP has not been tested.
The SV_InstanceID issue has been fixed in Unity6 and you no longer need to patch it.

### Features

- Use SV_InstanceID instead of unity_InstanceID when using the InstanceID-node. This breaks instancing support for the shader, but in return allows using the shader with IndexedIndirect render functions. i.e. Graphics.RenderPrimitivesIndexedIndirect. NOTE: This has been fixed in Unity6 and you no longer need to patch this.
- Add "nointerpolation" attribute to CustomInterpolators / Vertexshader output
- Support for uint, int, min16uint and min16int vertex input attributes. i.e. "float4 uv2 : TEXCOORD2;" becomes "min16uint4 uv2 : TEXCOORD2;". NOTE: This will **ONLY** patch vertex INPUT! All other nodes/code will not be affected. 

### Patching

When patching a shader, the original shader(graph) will not be touched and a new shader will be created instead.</br>
The new shader will have the suffix "_Patched".</br>
i.e. Opaque.shadergraph will become Opaque_Patched.shadergraph</br>
The name of the new shader will also be changed.</br>
i.e. "Shader Graphs/Opaque" will become "Shader Graphs/Opaque_Patched"

<img src="./Docs/Graph.jpg">

If patching both SV_InstanceID and nointerpolation, "InstanceID" in the screenshot above will contain SV_InstanceID instead of unity_InstanceID and "NoInterpolationData" will get the "nointerpolation" attribute.

### How To

<img src="./Docs/PatcherWindow.jpg">

Copy the "Editor" folder to your Unity project. The patcher will then be available under "Tools/Shader Graph Patcher".</br>
Drag and drop a shadergraph or shader to the window and select the features you want to patch.</br>
Then press "Patch"

~ Use at your own risk. ~
