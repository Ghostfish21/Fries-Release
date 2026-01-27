## 总览 Overview
Fries Utility Pack 是一个用于 Unity 便捷开发的工具包。它包含以下模块：
1. `Evtsys` 事件系统

## Evtsys
这是一个使用 `ILPP` 和 `Roslyn` 的便捷事件系统，支持用户在任何地方轻松调用任何能访问到的事件

### 事件声明
事件需要在任意类中按如下方式声明
```
public class AnyClass {
  // ↓ 必须要有  ↓ 随意访问符   ↓ 任意名称   ↓ 任意数量的参数
  [EvtDeclarer] public struct EventName { MyType evtArgument1; MyType evtArgument2; ... }
  //                   ↑ 必须是 struct
}
```
Evtsys 会按 EvtDeclarer 标记来寻找所有事件，并自动注册它们。
注意，如果这个事件会被很频繁的触发，比如在 Update 循环中，取决于参数的类型，可能会因为装箱产生 GC 压力。
如果参数有值类型的参数，事件触发时就会产生装箱。但是如果参数都是引用类型的就不会有装箱。对于有性能需求的事件，请使用 装箱优化事件
所有事件只支持编译前声明，不支持运行时声明事件

### 事件触发
对于上述方式声明的事件，可以通过以下方法触发它
```
Evt.TriggerNonAlloc<AnyClass.EventName>(evtArg1, evtArg2, ...);
```

### 静态事件监听器
通过以下代码监听事件
```
[EvtListener(typeof(AnyClass.EventName))]
// ↓ Public/Private ↓ 任何名字 ↓ 参数列表必须和 AnyClass.EventName 的成员变量一致
public static void myListener(MyType evtArgument1, MyType evtArgument2, ...) {
//     ↑ 必须是 static void
  Debug.Log("Event triggered!");
}
```
Evtsys 会检测并自动注册静态事件监听器，这样把方法写完后，每次 Evt.TriggerNonAlloc ... 都会调用到这个监听器
监听器支持设置 ExecutionOrder，具体见 EvtListener 类

### 实例事件监听器
通过以下方式注册实例事件监听器
```
[EvtCallback(typeof(AnyClass.EventName))]
//     ↓ 必须不是 static
public void myListener(MyType evtArgument1, MyType evtArgument2, ...) {
  Debug.Log($"Event triggered for instance {this.GetInstanceId()}!");
}
```
目前实例事件监听器支持 MonoBehaviour 和 ScriptableObject, 以及 System.Object 使用。对于 MonoBehaviour 来说
拥有实例监听器的 MonoBehaviour 必须提供 Awake 方法（无论方法是空的还是有内容都可以，它只要求这个类有一个 Awake 方法）
Evtsys 会向发现 实例监听器 的类（不是基类也不是派生类，而是同一个类）中的 Awake 方法的开头注入收集实例的代码。因此，如果这个类存在继承/被继承关系，
请确保这个类的 Awake 方法会被执行到。

这里是没有那么重要的内容：
对于 ScriptableObject 和 System.Object 来说，我对它们的测试不是很多，可能会有 BUG。简单来说，ScriptableObject 的注入点是 Awake。System.Object 的注入点是构造器
（注：对于 System.Object 来说，它必须提供一个 == 的重载，该重载应该定义当该实例应该被回收时 它自身 == null 返回 true）

### 装箱优化事件
装箱优化事件可以通过以下代码声明
```
// ↓ 必须打特性 ↓ 任意访问符         ↓ 任意事件名称
[EvtDeclarer] public partial class EventName { int i1; float f1; ... }
//                   ↑ 必须是 partial class     ↑ 任意参数
```
区别于普通事件的地方是，装箱优化事件必须直接位于某个 Namespace 下，不能被其他类所嵌套

装箱优化事件触发方式：
```
EventName.TriggerNonAlloc(i1, f1, ...);
```
以及这种事件的监听方式：
```
[EvtCallback(typeof(EventName))]
private void myListener(EventName data) {
  int i1 = data.getI1();
  float f1 = data.getF1();
  // ...
}
```
或者
```
[EvtListener(typeof(EventName))]
private static void myListener(EventName data) {
  int i1 = data.getI1();
  float f1 = data.getF1();
  // ...
}
```
data 的 get* 方法由 Evtsys 系统生成，如果在编译器里是红的，启动 Unity 刷新一下就好了
如果所有 Fries 引用都抛红，去 Preference 里面 External Tool 里刷新一下 csproj
如果需要修改 data 的数据内容，可以将时间名用 W 结尾。以 W 结尾的事件会自动生成 set* 方法
