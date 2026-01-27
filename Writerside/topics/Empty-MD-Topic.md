# Features

Fries is a powerful, comprehensive utility toolkit for Unity that boosts productivity and accelerates development.

Document v1.2 - by **_Guiyu_**

Release repo: [[Fries Release](https://github.com/Ghostfish21/Fries-Release)]

Experimental repo: [[Fries](https://github.com/Ghostfish21/Fries)]

### Event System 
One-line event & listener declarations, automatic subscription/unsubscription, and one-line event dispatching — with near zero-GC overhead.<br>
The system is implemented with Roslyn Source Generator and IL Post-Processor
![Event System.png](Event System.png)

### Prioritized Layered Input Router
A system that passes and consumes the input signal layer by layer. The system is compatible with both Unity New Input System and Legacy Input Manager, and extendable for custom Input Method.
![Input Dispatcher.png](Input Dispatcher.png)

### Network Persistent Object
Persistence framework that automatically loads/unloads network primitives (e.g., **`NetworkVariable<T>`**, **`NetworkList<T>`**) and plain C# objects; implemented with **Roslyn Source Generators**.
![NetPersistObject.png](NetPersistObject.png)

### Everything Pool
Pooling everything. Inspector-driven MonoBehaviour pools + lazy pools for arbitrary C# objects, designed to cut GC overhead and improve runtime performance.
![Everything Pool.png](Everything Pool_2.png)

### Type Tag
Tag GameObjects with their MonoBehaviours. <br>
O(1) tag queries and component retrieval.
![Type Tag.png](Type Tag_2.png)

### Prefab Parameters
Set arguments for MonoBehaviours' Awake and Start methods!
![Prefab Parameters.png](Prefab Parameters.png)

### In-game Console
A Minecraft style in-game console core that supports custom Display UI
![In-game Console.png](In-game Console.png)

### Custom Data
A MonoBehaviour that lets you attach arbitrary data on it / Declare global variable through it.
![Custom Data.png](Custom Data.png)

### Block Map
A 3D Grid system that 