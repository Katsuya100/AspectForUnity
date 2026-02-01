# AspectForUnity
[日本語](README_ja.md)

## Overview

AspectForUnity provides Aspect-Oriented Programming (AOP) functionality to Unity projects.  
Using ILPostProcessor, you can insert processing before and after methods.  
This allows you to implement cross-cutting concerns such as logging, performance measurement, and exception handling separately from business logic.  
These are inserted at compile time, minimizing the impact on runtime performance.  


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
public class LoggingAspect
{
}
```

### 2. Implementing Advice Methods
Implement advice methods within the aspect class and add the `Advice` attribute and Pointcut attribute.
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

Insert processing after method execution (executed even if an exception occurs).

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

Insert processing after method throws an exception.

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
Multiple conditions can be set and are matched with AND conditions.

### RegexPointcut

Match methods using regular expressions against the internal representation called `method identifier name`.
By combining with `PointcutNameFlag`, you can specify elements to include in the `method identifier name`.

*Example of method identifier name
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
`AssemblyFamily.AssemblyName[assembly:AssemblyAttribute][module:ModuleAttribute][declaring:DeclaringAttribute][return:ReturnAttribute][MethodAttribute("AttributeParameter",Property="AttributeProperty")]public sealed override ReturnType DeclaringTypeName<[DeclaringGenericAttribute]TDeclaring>MethodName<[GenericAttribute]TMethod>([ParameterAttribute]ParameterType parameterName)`
Each element of the method identifier name corresponds as follows:


##### PointcutNameFlag Options

| Flag                  | Description                         | Component in Above Method Identifier Name Example |
|-----------------------|-------------------------------------| --------------------------------|
| AssemblyAttribute    | Include assembly attributes in method identifier name       | `[assembly:AssemblyAttribute]` |
| AssemblyName         | Include assembly name in method identifier name         | `AssemblyFamily.AssemblyName` |
| ModuleAttribute     | Include module attributes in method identifier name       | `[module:ModuleAttribute]` |
| DeclaringTypeAttribute | Include declaring type attributes in method identifier name        | `[declaring:DeclaringAttribute]` |
| DeclaringTypeName   | Include declaring type name in method identifier name            | `DeclaringTypeName` |
| DeclaringTypeGenericArgumentAttribute | Include declaring type generic argument attributes in method identifier name | `<TDeclaring>` |
| DeclaringTypeGenericArgumentName | Include declaring type generic argument names in method identifier name | `<[DeclaringGenericAttribute]>` |
| MethodAttribute     | Include method attributes in method identifier name         | `[MethodAttribute]` |
| MethodName          | Include method name in method identifier name           | `MethodName` |
| ReturnTypeAttribute | Include return value attributes in method identifier name         | `[return:ReturnAttribute]` |
| ReturnTypeName      | Include return value type name in method identifier name         | `ReturnType` |
| GenericArgumentAttribute | Include generic argument attributes in method identifier name  | `<TMethod>` |
| GenericArgumentName | Include generic argument names in method identifier name     | `<[GenericAttribute]>` |
| ParameterAttribute  | Include parameter attributes in method identifier name        | `([ParameterAttribute])` |
| ParameterTypeName   | Include parameter type names in method identifier name        | `(ParameterType)` |
| ParameterName       | Include parameter names in method identifier name          | `(parameterName)` |
| MethodAccessModifier | Include method public/private/protected modifiers in method identifier name  | `public` |
| MethodStaticModifier | Include method static modifier in method identifier name  | `static` |
| MethodOverrideModifier | Include method override/abstract/virtual/sealed modifiers in method identifier name | `sealed override` |
| AttributeArguments  | Include attribute constructor arguments in method identifier name | `("AttributeParameter")` |
| AttributeProperties | Include attribute properties in method identifier name      | `(Property="AttributeProperty")` |
| AncestorDeclaringTypeAttribute | Recursively traverse parent class attributes and include in method identifier name<br/>Can only be used when DeclaringTypeAttribute is enabled     | `[declaring:DeclaringAttribute]`<br/>Recursively traverses as follows<br/>`[declaring:DeclaringAttribute,AncestorDeclaringTypeAttribute]` |
| AssemblyFullName    | Include assembly fully qualified name in method identifier name<br/>Can only be used when AssemblyName is enabled  | `AssemblyFamily.AssemblyName`<br/>Becomes full name as follows<br/>`AssemblyFamily.AssemblyName, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null` | 
| TypeFullName        | Include type fully qualified name in method identifier name<br/>Can only be used when any TypeName is enabled  | `DeclaringTypeName` and others<br/>Becomes full name as follows<br/>`Namespace.DeclaringTypeName` |
| Simple              | Basic identifier name | N/A |
| LocalSignature      | Identifier name within assembly | N/A |
| GlobalSignature     | Global identifier name | N/A |
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
`Logs/PointcutMethodName/[AssemblyName]/[ClassName].txt`

## Parameter Binding
### Basic Binding
By giving the advice method's parameters the same name as the target method's arguments, you can bind values.
```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice(int parameter1, string parameter2)
{
    Debug.Log($"parameter1: {parameter1}, parameter2: {parameter2}");
}
```

The following is the injection target method
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

Obtain the `this` instance of the target method.

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

Obtain the target method's parameters as an array.

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
*Can only be used with AfterReturning

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
*Can only be used with AfterThrowing

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
[RegexPointcut(@"<T>(T value)", PointcutNameFlag.GenericArgumentName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.PointcutParameterName)]
public static void GenericAdvice<[PointcutGenericBind(GenericBinding.ParameterType)]T>(T value)
{
    Debug.Log($"generic argument: {typeof(T).Name}, value: {value}");
}
```

##### GenericBinding Options

| BindingType | Description                     |
|-------------|-----------------------------|
| GenericParameterName | Bind by generic parameter name.<br/>Default behavior. |
| ParameterType | Implicitly bind when used as a parameter type. |

## Advanced Features

### Unsafe Injection

By adding ref to arguments, you can modify return values and parameters.

```.cs
[Advice(JoinPoint.AfterReturning, unsafeInjection: true)]
[RegexPointcut("^Int32(Int32 parameter)$", PointcutNameFlag.ReturnTypeName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.PointcutParameterName)]
public static void ModifyReturn(ref int parameter, [PointcutReturned] ref int returnValue)
{
    parameter = 42;  // Modify argument
    returnValue = 999;  // Modify return value
}
```

## Blocking Aspects

You can disable aspect application for specific methods.

```.cs
[BlockAspect(typeof(LoggingAspect))]
public void NoLoggingMethod()
{
    // LoggingAspect will not be applied to this method
}
```

It is also possible to disable aspect application for the entire Assembly with the following notation:
```.cs
[assembly: BlockAspect(typeof(LoggingAspect))]
```

## Performance Considerations

- Due to compile-time code generation by ILPostProcessor, runtime overhead is minimal
- However, applying many aspects may increase compilation time

## Sample: Performance Measurement

```.cs
using System.Diagnostics;
using Katuusagi.AspectForUnity;

[Aspect]
public class PerformanceAspect
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
public class ExceptionHandlingAspect
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
