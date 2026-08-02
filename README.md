# AspectForUnity
[日本語](README_ja.md)

## Overview

AspectForUnity provides Aspect-Oriented Programming (AOP) functionality to Unity projects.  
Using ILPostProcessor, you can insert processing before and after methods.  
This allows you to implement cross-cutting concerns such as logging, performance measurement, and exception handling separately from business logic.  
The target method IL is rewritten at compile time; at runtime, the inserted advice executes as ordinary method calls.  


## Verified Environment
|  Environment  |  Version  |
| ---- | ---- |
| Unity | 6000.0.60f1 |
| .Net | 4.x, Standard 2.1 |

## Main Features

- **JoinPoint.Before**: Insert processing before method execution
- **JoinPoint.After**: Insert processing after method execution
- **JoinPoint.AfterReturning**: Insert processing after method completes normally
- **JoinPoint.AfterThrowing**: Insert processing after method throws an exception
- **Regex-based Pointcut**: Match method names and class names using regular expressions
- **Parameter Binding**: Binding of method arguments/type arguments/return values
- **Unsafe Injection**: Modification of return values and parameters
- **Application Blocking**: Suppress aspect application at assembly, module, type, or method scope

## Installation Method
### Installing ILPostProcessorCommon
- [ILPostProcessorCommon v2.5.0](https://github.com/Katsuya100/ILPostProcessorCommon/tree/ver2.5.0)

### Installing AspectForUnity
1. Open [Window > Package Manager].
2. Click [+ > Add package from git url...].
3. Enter `https://github.com/Katsuya100/AspectForUnity.git?path=packages` and click [Add].

#### If It Doesn't Work
The above method may not work in environments where git is not installed.  
Download `com.katuusagi.aspectforunity.tgz` for the corresponding version from [Releases](https://github.com/Katsuya100/AspectForUnity/releases)  
and install it using [Package Manager > + > Add package from tarball...].

#### If It Still Doesn't Work
Download `Katuusagi.AspectForUnity.unitypackage` for the corresponding version from [Releases](https://github.com/Katsuya100/AspectForUnity/releases)  
and import it into your project from [Assets > Import Package > Custom Package].

## Basic Usage

### 1. Creating an Aspect Class
Define an aspect class by adding the `Aspect` attribute to a class.

```.cs
using Katuusagi.AspectForUnity;

[Aspect]
public static class LoggingAspect
{
}
```

### 2. Implementing Advice Methods
Implement `public void` advice methods within the aspect class and add the `Advice` attribute and a Pointcut attribute. Every advice requires at least one Pointcut on the method or its declaring type.  
In the sample below, we use `RegexPointcut` (described later) to apply advice to methods containing `TestMethod` in their method name.
```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*TestMethod.*", PointcutNameFlag.MethodName)]
public static void BeforeAdvice()
{
    Debug.Log($"before method");
}

[Advice(JoinPoint.AfterReturning)]
[RegexPointcut(".*TestMethod.*", PointcutNameFlag.MethodName)]
public static void AfterAdvice()
{
    Debug.Log($"after method");
}
```

For example, advice will be inserted into the following methods
```.cs
public class SampleClass
{
    public static void TestMethod()
    {
        Debug.Log("method body");
    }
}
```

### 3. Execution Result
When TestMethod is executed, the following will be output:
```
before method
method body
after method
```

## JoinPoint Settings for Advice

### Before

Insert processing before method execution.

```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice()
{
    // Processing before method execution
}
```

### After

Insert processing in a `finally` around the method body. It runs whether the method body returns normally or throws.

```.cs
[Advice(JoinPoint.After)]
[RegexPointcut(".*")]
public static void AfterAdvice()
{
    // Processing after method execution
}
```

### AfterReturning

Insert processing after method completes normally.

```.cs
[Advice(JoinPoint.AfterReturning)]
[RegexPointcut(".*")]
public static void AfterReturningAdvice()
{
    // Processing after method completes normally
}
```

### AfterThrowing

Insert processing when the method body throws, then rethrow the same exception. When `PointcutThrown` uses a derived exception type, the advice runs only for matching exceptions.

```.cs
[Advice(JoinPoint.AfterThrowing)]
[RegexPointcut(".*")]
public static void AfterThrowingAdvice()
{
    // Processing when exception occurs
}
```

## Pointcut Attributes

Pointcut attributes specify which methods the advice method will be applied to.  
Multiple conditions can be set and are matched with AND conditions. Pointcuts may be placed on the advice method or on its declaring type, including enclosing declaring types.

### RegexPointcut

Match methods using regular expressions against the internal representation called `method identifier name`.  
By combining with `PointcutNameFlag`, you can specify elements to include in the `method identifier name`.

When the second argument is omitted, `RegexPointcut` uses `PointcutNameFlag.Simple`.

*Example method identifier name (`Simple`)
`String SampleController::GetStatus<T>(Int32 parameter)`

```.cs
// Method names starting with "Get"
[RegexPointcut("^Get.*", PointcutNameFlag.MethodName)]

// Class names ending with "Controller"
[RegexPointcut(".*Controller$", PointcutNameFlag.DeclaringTypeName)]

// Methods starting with "Get" in classes ending with "Controller"
[RegexPointcut(".*Controller::Get.*", PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
```

##### Method Identifier Name Composition Example

When all elements are included, it is composed as follows: 
```
AssemblyName[assembly:AssemblyAttribute][module:ModuleAttribute][declaring:DeclaringAttribute][return:ReturnAttribute][MethodAttribute("AttributeParameter",Property="AttributeProperty")]public static sealed override ReturnType DeclaringTypeName<[DeclaringGenericAttribute]TDeclaring>::MethodName<[GenericAttribute]TMethod>([ParameterAttribute]ParameterType parameterName)
```
Each element of the method identifier name corresponds as follows:


##### PointcutNameFlag Options

| Flag                  | Description                         | Component in Above Method Identifier Name Example |
|-----------------------|-------------------------------------| --------------------------------|
| AssemblyAttribute    | Include assembly attributes in method identifier name       | `[assembly:AssemblyAttribute]` |
| AssemblyName         | Include assembly name in method identifier name         | `AssemblyName` |
| ModuleAttribute     | Include module attributes in method identifier name       | `[module:ModuleAttribute]` |
| DeclaringTypeAttribute | Include declaring type attributes in method identifier name        | `[declaring:DeclaringAttribute]` |
| DeclaringTypeName   | Include declaring type name in method identifier name            | `DeclaringTypeName` |
| DeclaringTypeGenericArgumentAttribute | Include declaring type generic argument attributes in method identifier name | `<[DeclaringGenericAttribute]>` |
| DeclaringTypeGenericArgumentName | Include declaring type generic argument names in method identifier name | `<TDeclaring>` |
| MethodAttribute     | Include method attributes in method identifier name         | `[MethodAttribute]` |
| MethodName          | Include method name in method identifier name           | `MethodName` |
| ReturnTypeAttribute | Include return value attributes in method identifier name         | `[return:ReturnAttribute]` |
| ReturnTypeName      | Include return value type name in method identifier name         | `ReturnType` |
| GenericArgumentAttribute | Include generic argument attributes in method identifier name  | `<[GenericAttribute]>` |
| GenericArgumentName | Include generic argument names in method identifier name     | `<TMethod>` |
| ParameterAttribute  | Include parameter attributes in method identifier name        | `([ParameterAttribute])` |
| ParameterTypeName   | Include parameter type names in method identifier name        | `(ParameterType)` |
| ParameterName       | Include parameter names in method identifier name          | `(parameterName)` |
| MethodAccessModifier | Include method public/private/protected modifiers in method identifier name  | `public` |
| MethodStaticModifier | Include method static modifier in method identifier name  | `static` |
| MethodOverrideModifier | Include method override/abstract/virtual/sealed modifiers in method identifier name | `sealed override` |
| AttributeArguments  | Include attribute constructor arguments in method identifier name | `("AttributeParameter")` |
| AttributeProperties | Include attribute properties in method identifier name      | `(Property="AttributeProperty")` |
| AncestorDeclaringTypeAttribute | Recursively traverse parent class attributes and include in method identifier name<br/>Can only be used when DeclaringTypeAttribute is enabled     | `[declaring:DeclaringAttribute]`<br/>Recursively traverses as follows<br/>`[declaring:DeclaringAttribute,AncestorDeclaringTypeAttribute]` |
| AssemblyFullName    | Include assembly fully qualified name in method identifier name<br/>Can only be used when AssemblyName is enabled  | `AssemblyName`<br/>Becomes full name as follows<br/>`AssemblyName, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null` | 
| TypeFullName        | Include type fully qualified name in method identifier name<br/>Can only be used when any TypeName is enabled  | `DeclaringTypeName` and others<br/>Becomes full name as follows<br/>`Namespace.DeclaringTypeName` |
| Simple              | Return type, declaring type, method name, method generic argument names, and parameter types/names | N/A |
| LocalSignature      | `Simple` plus fully qualified type names | N/A |
| GlobalSignature     | `LocalSignature` plus assembly name | N/A |
| All                 | Include all elements in method identifier name<br/>*Behavior may change with updates. | N/A |

##### How to Check Method Identifier Name
If you want to check the method identifier name, add the `OutputPointcutMethodName` attribute to the target function.
```.cs
// Specify the identifier name you want to output with PointcutNameFlag
[OutputPointcutMethodName(PointcutNameFlag.Simple)]
public void SampleMethod(int parameter)
{
    // Method body
}
```
###### Output Destination
`Logs/PointcutMethodName/[AssemblyName]/[fully-qualified-type-name].txt`  
The file contains the identifier generated with `All` and one identifier for each flag requested by the attribute. Characters invalid in file names are replaced with `_`.

## Parameter Binding
### Basic Binding
Give advice parameters the same names as target-method arguments to bind type-compatible values. A missing name or incompatible type produces a compilation error. `Before`, `AfterThrowing`, and `After` cannot bind target `out` parameters.
```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice(int parameter1, string parameter2)
{
    Debug.Log($"parameter1: {parameter1}, parameter2: {parameter2}");
}
```

The following is the target method
```.cs
public class SampleClass
{
    public static void TestMethod(int parameter1, string parameter2)
    {
        // Method body processing
    }
}
```

### Special Binding
By adding the following attributes to advice method parameters, you can obtain runtime information.

#### PointcutThis

Obtain the `this` instance of the target method. This applies only to instance methods, and the declared parameter type must be compatible with the target declaring type.

```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice([PointcutThis] object self)
{
    Debug.Log($"instance type: {self.GetType().Name}");
}
```

#### PointcutMethod

Obtain information about the target method.

```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice([PointcutMethod] MethodBase method)
{
    Debug.Log($"method name: {method.Name}");
}
```

#### PointcutParameters

Obtain the target method's parameters as a `ParameterArray`, readable through `Length` and its indexer. It is a `readonly ref struct` over a pooled array; use it only during the advice call and do not retain it.

```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice([PointcutParameters] ParameterArray parameters)
{
    Debug.Log($"parameter count: {parameters.Length}");
}
```

#### PointcutReturned

Obtain the target method's return value.  
*Only valid with AfterReturning. It cannot be used on `void` methods, and its type must be compatible with the return type.

```.cs
[Advice(JoinPoint.AfterReturning)]
[RegexPointcut("^String$", PointcutNameFlag.ReturnTypeName)]
public static void AfterReturningAdvice([PointcutReturned] string returnValue)
{
    Debug.Log($"return value: {returnValue}");
}
```

#### PointcutThrown

Obtain the thrown exception.  
*Only valid with AfterThrowing. Its type must be `Exception` or a derived exception type.

```.cs
[Advice(JoinPoint.AfterThrowing)]
[RegexPointcut(".*")]
public static void AfterThrowingAdvice([PointcutThrown] Exception exception)
{
    Debug.LogError($"exception: {exception.Message}");
}
```

#### PointcutGenericBind

Specify how to bind generic parameters.


```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(@"<T>\(T value\)", PointcutNameFlag.GenericArgumentName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.ParameterName)]
public static void GenericAdvice<[PointcutGenericBind(GenericBinding.ParameterType)]T>(T value)
{
    Debug.Log($"generic argument: {typeof(T).Name}, value: {value}");
}
```

##### GenericBinding Options

| BindingType | Description                     |
|-------------|-----------------------------|
| GenericParameterName | Bind by generic parameter name.<br/>Default behavior. |
| ParameterType | Infer the generic argument from its use in an ordinary advice parameter, `PointcutThis`, or `PointcutReturned`. No inferred type or multiple distinct inferred types is a compilation error. |

## Advanced Features

### Unsafe Injection

Set `unsafeInjection` on `Advice` to `true` and add `ref` to modify return values and target-method parameters. “Unsafe” here explicitly permits advice to rewrite values; it does not mean C# pointers or an `unsafe` context.

```.cs
[Advice(JoinPoint.AfterReturning, unsafeInjection: true)]
[RegexPointcut(@"^Int32\(Int32 parameter\)$", PointcutNameFlag.ReturnTypeName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.ParameterName)]
public static void ModifyReturn(ref int parameter, [PointcutReturned] ref int returnValue)
{
    parameter = 42;  // Modify argument
    returnValue = 999;  // Modify return value
}
```

### Explicitly Specifying Aspect Scope
#### Apply Only Within the Assembly and to References
A normal aspect is loaded while compiling its defining assembly and assemblies that directly reference that assembly. Unrelated assemblies are unaffected. The target assembly must also reference `Katuusagi.AspectForUnity`.

#### Apply to All Assemblies
1. Create an Assembly Definition Reference (`.asmref`) that references `packages/Runtime/AspectEntry/AspectEntry.asmdef`.
2. Place the Aspect class in the same assembly as that `.asmref`.
3. The aspect will be applied to all AssemblyDefinitions.

## Blocking Aspects

You can disable aspect application for specific methods.

```.cs
[BlockAspect]
public void NoLoggingMethod()
{
    // LoggingAspect will not be applied to this method
}
```

It is also possible to disable aspect application for the entire Assembly with the following notation:
```.cs
[assembly: BlockAspect]
```

`BlockAspect` can target an assembly, module, class, struct, enum, method, or constructor and blocks all aspect application within that scope.

## Performance Considerations

- Pointcut regex matching and IL rewriting happen at compile time, so runtime execution performs no regex matching or proxy dispatch
- Runtime still pays for the advice calls themselves; `PointcutMethod` obtains a `MethodBase`, while `PointcutParameters` boxes arguments and stores them in a pooled array
- However, applying many aspects may increase compilation time

## Sample: Performance Measurement

```.cs
using System.Diagnostics;
using Katuusagi.AspectForUnity;

[Aspect]
public static class PerformanceAspect
{
    private static Stopwatch stopwatch = new Stopwatch();

    [Advice(JoinPoint.Before)]
    [RegexPointcut(".*")]
    public static void StartTimer()
    {
        stopwatch.Restart();
    }

    [Advice(JoinPoint.After)]
    [RegexPointcut(".*")]
    public static void StopTimer([PointcutMethod] MethodBase method)
    {
        stopwatch.Stop();
        Debug.Log($"{method.Name} duration: {stopwatch.ElapsedMilliseconds}ms");
    }
}
```

## Sample: Exception Handling

```.cs
using System;
using Katuusagi.AspectForUnity;

[Aspect]
public static class ExceptionHandlingAspect
{
    [Advice(JoinPoint.AfterThrowing)]
    [RegexPointcut(".*")]
    public static void HandleException(
        [PointcutMethod] MethodBase method,
        [PointcutThrown] Exception exception)
    {
        Debug.LogError($"method: {method.Name} exception: {exception.Message}");
        // You can log exceptions or send them to an error reporting service
    }
}
```

## Technical Details

### Architecture

- **ILPostProcessor**: Modifies IL code at compile time using Unity.CompilationPipeline
- **Mono.Cecil**: Used for reading and writing IL code
- **Attribute-based Configuration**: Uses attributes to define aspects and advice
- **Target Assemblies**: Processes assemblies that reference `Katuusagi.AspectForUnity`
- **Method Transformation**: Moves the original body to a generated method and rebuilds the original method as the advice wrapper

### Advice Constraints

- Declare advice in an `[Aspect]` class as `public void` (`public static void` for static advice). Advice cannot use `out` parameters.
- Static aspects work directly. An instance aspect cannot be abstract or generic and needs exactly one matching `public` constructor marked `[Advice(JoinPoint.Before)]` per target method. Its instance advice shares the object created by that constructor.
- Invalid advice declarations or incompatible bindings to target methods are reported in Unity's compilation log.
