# Debug Draw API for Unity

```DbgDraw``` is an API that provides the ability to render various 2D and 3D shapes for visual debugging purposes. It's similar to Unity's [Gizmos](https://docs.unity3d.com/ScriptReference/Gizmos.html) and [Handles](https://docs.unity3d.com/ScriptReference/Handles.html) API's.

Unity provides a rather limited set of debug draw functions in the Debug class, such as [Debug.DrawLine](https://docs.unity3d.com/ScriptReference/Debug.DrawLine.html) for example.

Unlike Unity's Gizmo, which requires rendering code to be located in a [OnDrawGizmos](https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnDrawGizmos.html) method, you can issue DbgDraw calls from anywhere in your code. Rather than issuing these render calls immediately, DbgDraw collects those calls and defers the actual rendering to a later time. This allows to call DbgDraw from any method, be it Start(), Update() etc.

DbgDraw provides functionality to render the following shapes:
* Arc
* Cube
* Disc
* Line, Lines and PolyLine
* Matrix
* Plane
* Pyramid
* Quad
* Ray
* Sphere
* Tube

Most of these shapes can be rendered solid and in wireframe.


# Example

```csharp
using Oddworm.Framework;

// Draw a wireframe cube for a single frame
DbgDraw.WireCube(position, rotation, scale, color); 

// Draw a solid cube for ten seconds
DbgDraw.Cube(position, rotation, scale, color, 10);
```
Please import the "DbgDraw Examples" file, which part of this package, for more examples.


# Limitations

* DbgDraw works with Unity 2020.3 and later versions.
* DbgDraw works when you have defined `DBG_DRAW_ENABLED` and `DbgDraw.Enabled` is set to true (which it is by default).
* DbgDraw works in play mode only.
* DbgDraw has been created for visual debugging purposes ONLY! It is NOT as a fast general purpose shape rendering API.


# Installation

~~Open in Unity Window > Package Manager, choose "Add package from git URL" and insert one of the Package URL's you can find below.~~

Copy the contents of this repo (minus the `.git` folder) into a new folder named `Assets/Plugins/DrawDbg`.

# FAQ

### It's not working in the editor/in a build

DbgDraw is enabled when you define `DBG_DRAW_ENABLED` in the player settings, and `DBG_DRAW_DISABLED` is NOT defined.

### Remove DbgDraw calls from release builds

Remove `DBG_DRAW_ENABLED` from the defined symbols in the player settings (make sure to check any overriden values in your active build profile).

### "Hidden/DbgDraw-Shaded" in Always Included Shaders

When enabled, this plugin automatically adds the ```Hidden/DbgDraw-Shaded``` shader to the 'Always Included Shaders' list in the Player settings when you create a build. This is required to make the DbgDraw package work in a build. It's a very simple shader that does add a trivial amount of size to the build.
When disabled, this shader is removed from said list and shouldn't appear in the build report. If it's still being included then something is referencing it and you'll need to find out what that is.
