# BLACK TUNDRA - FOUNDATION
The foundation package implements basic boilerplate game logic and provides a plethora of utilities to make game development faster and more secure.
## Project Structure
The project is split into different directories/sub-namespaces. All code is contained within the `BlackTundra.Foundation` namespace.
### BlackTundra.Foundation
This namespace contains the main classes that help the package function. All scripts automatically execute code to set themselves up when the application is launched. The main script in this namespace is the `Core.cs` class; which is responsible for acting as the games core and ensuring all foundation systems are functional. The core can be used to shutdown the application.

### BlackTundra.Foundation.Collections
The collections namespace contains all C# collections that the foundation package introduces. There is also an additonal namespace within the collections called `BlackTundra.Foundation.Collections.Generic` which contains generic collections.

### BlackTundra.Foundation.Control
All InputSystem implementations are stored here. This namespace is responsible for handling input for the application. It is capable of tracking multiple input users across the application which can be used to implement split screen functionality in the application and other use cases with more than one user on the same machine. The main class to be concerned with in this namespace is the `InputUser` class, which controls a single input user and statically tracks other existing users.

### BlackTundra.Foundation.IO
All input/output logic such as file saving/loading is defined here. This namespace contains a very important class called `FileSystem`, which helps manage the applications local file system. This class should be used for saving/loading any files/objects.

### BlackTundra.Foundation.Logging
Logging is implemented here. Logging is handled for the most part automatically by the foundation package; but if you want to implement your own custom logger, you can do so using the `LogManager` class, where you can request a new `Logger` instance to push logs to.

### BlackTundra.Foundation.Platform
All platform specific implementations are defined here. This contains logic for interfacing with different systems such as Steam, Origin, Epic Games Launcher, Xbox Store, etc. Not all systems will be implemented, but in the future they'll be added as needed. These will automatically be added into the project depending on what platform you are compiling for.

### BlackTundra.Foundation.Security
To make the application more secure, security logic is built-in to the foundation package and used by many sub-systems to ensure the game is not as easy to tamper with. Basic encryption/decryption is implemented, as well as obfuscation and key generation. This namespace is useful when trying to secure sensitive data. It can otherwise be ignored if using other foundation systems to handle data since they will implement the security for you.

### BlackTundra.Foundation.Utility
Any miscillanious utility classes are implemented here. This namespace contains classes that extend default C# and Unity objects to make them easier to work with. This is by far one of the most useful namespaces, to see more about what it can do it's best to look through it and read the documentation for each of the utility classes inside of it.

## Useful Features

### Custom Commands
To implement a custom command, decorate a static method of return type `bool` with the `[Command]` attribute and ensure the method signature matches `(CommandInfo info)`.
#### Example:
```csharp
[Command( // this attribute marks this static method as a command
    Name = "mycommand",
    Description = "This is my custom command!!",
    Usage = "mycommand"
        + "\n\tExecutes my custom command."
)]
private static bool MyCommand(CommandInfo info) {
    ConsoleWindow console = Core.consoleWindow; // get the console window
    console.Print("Hello world!"); // print a message to the console window
    return true; // command successful, return false if the command was not successful
}
```

### Custom Serialization
To implement custom serializable classes, decorate a type with the `[Serializable]` and `[SerializableImplementationOf(typeof(TYPE HERE))]` attributes. Ensure the implementation has a constructor with a signature that only takes in the target type (without any `in`, `out`, or `ref` keywords). After adding a custom type, make sure to check the main log file to ensure it was bound correctly by the `ObjectUtility`. You should also make sure there is a way to cast the serialized implementation back to the target type using an explicit cast operator.
#### Example:
```csharp
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using BlackTundra.Foundation.Utility;
[Serializable]
[SerializableImplementationOf(typeof(Vector2))]
[StructLayout(LayoutKind.Explicit, Size = 8, Pack = 1)]
public struct SerializableVector2 {
    [FieldOffset(0)]
    public float x;
    [FieldOffset(4)]
    public float y;
    public SerializableVector2(Vector2 v) {
        x = v.x;
        y = v.y;
    }
    public static explicit operator Vector2(in SerializableVector2 v) => new Vector2(v.x, v.y);
}
```

### Custom Initialisation & Termination Methods
Custom initialization and termination methods can be created by decorating a static method with the `[CoreInitialise]` or `[CoreTerminate]` attributes.
#### Example:
```csharp
[CoreInitialise]
private static void Initialise() { // called when the application is started
    // code here
}
[Core Terminate]
private static void Terminate() { // called when the application is closed
    // code here
}
```

## Project Configuration

### Version
The application version should follow the format: `{major}.{minor}.{release}{type}`.

#### Format
| Name      | Description                                     | Value |
| --------- | ----------------------------------------------- | ----- |
| `major`   | Main version/build iteration.                   | x > 0 |
| `minor`   | Current iteration of the `major` version/build. |   x   |
| `release` | Current iteration of the `minor` version/build. |   x   |
| `type`    | Release type                                    | a/b/f |

#### Release Types
| Symbol | Description |
| ------ | ----------- |
| a      | Alpha       |
| b      | Beta        |
| f      | Final       |

### Input System
The foundation package supports the new Unity InputSystem package. The project should be set up to use the new InputSystem package.
For more information on setting up a project with the InputSystem package, click [here](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.0/manual/Installation.html).

Once the new input system is imported into the project, the foundation package will recompile with additonal input functionality. This functionality can be found in the `BlackTundra.Foundation.Control` namespace.

### Scripting Backend & API Compatibility Level
The foundation package was written using the `Mono` scrpting backend and the `.NET 4.x` API compatibility level.