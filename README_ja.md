# AspectForUnity
## 概要

AspectForUnityは、Unityプロジェクトにアスペクト指向プログラミング(AOP)の機能を提供します。  
ILPostProcessorを使用して、メソッドの前後に処理を挿入することができます。  
それによりログ出力、パフォーマンス測定、例外処理などの横断的関心事を、ビジネスロジックから分離して実装できます。  
これらは、コンパイル時に挿入されるためラインタイムパフォーマンスへの影響を最小限に抑えます。  


## 動作確認環境
|  環境  |  バージョン  |
| ---- | ---- |
| Unity | 6000.0.60f1 |
| .Net | 4.x, Standard 2.1 |

## 主な機能

- **JoinPoint.Before**: メソッド実行前に処理を挿入
- **JoinPoint.After**: メソッド実行後に処理を挿入
- **JoinPoint.AfterReturning**: メソッドが正常に終了した後に処理を挿入
- **JoinPoint.AfterThrowing**: メソッドが例外をスローした後に処理を挿入
- **正規表現によるPointcut**: メソッド名やクラス名などを正規表現でマッチング
- **パラメータバインディング**: メソッドの引数/型引数/戻り値のバインディング
- **Unsafe Injection**: 戻り値やパラメータの変更

## インストール方法
### ILPostProcessorCommonのインストール
- [ILPostProcessorCommon v2.5.0](https://github.com/Katsuya100/ILPostProcessorCommon/tree/ver2.5.0)

### AspectForUnityのインストール
1. [Window > Package Manager]を開く。
2. [+ > Add package from git url...]をクリックする。
3. `https://github.com/Katsuya100/AspectForUnity.git?path=packages`と入力し[Add]をクリックする。

#### うまくいかない場合
上記方法は、gitがインストールされていない環境ではうまく動作しない場合があります。  
[Releases](https://github.com/Katsuya100/AspectForUnity/releases)から該当のバージョンの`com.katuusagi.aspectforunity.tgz`をダウンロードし  
[Package Manager > + > Add package from tarball...]を使ってインストールしてください。  

#### それでもうまくいかない場合
[Releases](https://github.com/Katsuya100/AspectForUnity/releases)から該当のバージョンの`Katuusagi.AspectForUnity.unitypackage`をダウンロードし  
[Assets > Import Package > Custom Package]からプロジェクトにインポートしてください。

## 基本的な使い方

### 1. Aspectクラスの作成
クラスに`Aspect`属性を付与してアスペクトクラスを定義します。

```.cs
using Katuusagi.AspectForUnity;

[Aspect]
public class LoggingAspect
{
}
```

### 2. Adviceメソッドの実装
アスペクトクラス内にAdviceメソッドを実装し、`Advice`属性とPointcut属性を付与します。  
下記サンプルでは後述の`RegexPointcut`を使用して、メソッド名に`TestMethod`を含むメソッドに対してアドバイスを適用しています。  
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

例えば以下のメソッド等にAdviceが挿入される
```.cs
public class SampleClass
{
    public static void TestMethod()
    {
        Debug.Log("method body");
    }
}
```

### 3. 実行結果
TestMethodを実行すると、以下のように出力されます。
```
before method
method body
after method
```

## アドバイスに設定するJoinPoint

### Before

メソッド実行前に処理を挿入します。

```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice()
{
    // メソッド実行前の処理
}
```

### After

メソッド実行後に処理を挿入します（例外が発生しても実行されます）。

```.cs
[Advice(JoinPoint.After)]
[RegexPointcut(".*")]
public static void AfterAdvice()
{
    // メソッド実行後の処理
}
```

### AfterReturning

メソッドが正常に終了した後に処理を挿入します。

```.cs
[Advice(JoinPoint.AfterReturning)]
[RegexPointcut(".*")]
public static void AfterReturningAdvice()
{
    // メソッドが正常終了した後の処理
}
```

### AfterThrowing

メソッドが例外をスローした後に処理を挿入します。

```.cs
[Advice(JoinPoint.AfterThrowing)]
[RegexPointcut(".*")]
public static void AfterThrowingAdvice()
{
    // 例外発生時の処理
}
```

## Pointcut属性

Pointcut属性は、Adviceメソッドが適用されるメソッドを指定します。  
複数条件を設定でき、AND条件でマッチングされます。

### RegexPointcut

`メソッド識別名`という内部表現に対して、正規表現を使用してメソッドをマッチングします。  
`PointcutNameFlag` を組み合わせることで`メソッド識別名`に含まれる要素を指定できます。

※メソッド識別名の一例
`String SampleController::GetStatus<T>(Int32 parameter)`

```.cs
// "Get"で始まるメソッド名
[RegexPointcut("^Get.*", PointcutNameFlag.MethodName)]

// "Controller"で終わるクラス名
[RegexPointcut(".*Controller$", PointcutNameFlag.DeclaringTypeName)]

// "Controller"で終わるクラスの"Get"で始まるメソッド
[RegexPointcut(".*Controller::Get.*", PointcutNameFlag.DeclaringTypeName | PointcutNameFlag.MethodName)]
```

##### メソッド識別名構成例

すべての要素が含まれる場合以下のように構成されます  
`AssemblyFamily.AssemblyName[assembly:AssemblyAttribute][module:ModuleAttribute][declaring:DeclaringAttribute][return:ReturnAttribute][MethodAttribute("AttributeParameter",Property="AttributeProperty")]public sealed override ReturnType DeclaringTypeName<[DeclaringGenericAttribute]TDeclaring>MethodName<[GenericAttribute]TMethod>([ParameterAttribute]ParameterType parameterName)`  
メソッド識別名の各要素は以下のように対応します。  


##### PointcutNameFlag オプション

| Flag                  | 説明                         | 上記メソッド識別名例内の部品 |
|-----------------------|------------------------------| --------------------------------|
| AssemblyAttribute    | アセンブリ属性をメソッド識別名に含む       | `[assembly:AssemblyAttribute]` |
| AssemblyName         | アセンブリ名をメソッド識別名に含む         | `AssemblyFamily.AssemblyName` |
| ModuleAttribute     | モジュール属性をメソッド識別名に含む       | `[module:ModuleAttribute]` |
| DeclaringTypeAttribute | 宣言型の属性をメソッド識別名に含む        | `[declaring:DeclaringAttribute]` |
| DeclaringTypeName   | 宣言型名をメソッド識別名に含む            | `DeclaringTypeName` |
| DeclaringTypeGenericArgumentAttribute | 宣言型のジェネリック引数の属性をメソッド識別名に含む | `<TDeclaring>` |
| DeclaringTypeGenericArgumentName | 宣言型のジェネリック引数名をメソッド識別名に含む | `<[DeclaringGenericAttribute]>` |
| MethodAttribute     | メソッド属性をメソッド識別名に含む         | `[MethodAttribute]` |
| MethodName          | メソッド名をメソッド識別名に含む           | `MethodName` |
| ReturnTypeAttribute | 戻り値の属性をメソッド識別名に含む         | `[return:ReturnAttribute]` |
| ReturnTypeName      | 戻り値の型名をメソッド識別名に含む         | `ReturnType` |
| GenericArgumentAttribute | ジェネリック引数の属性をメソッド識別名に含む  | `<TMethod>` |
| GenericArgumentName | ジェネリック引数名をメソッド識別名に含む     | `<[GenericAttribute]>` |
| ParameterAttribute  | パラメータ属性をメソッド識別名に含む        | `([ParameterAttribute])` |
| ParameterTypeName   | パラメータ型名をメソッド識別名に含む        | `(ParameterType)` |
| ParameterName       | パラメータ名をメソッド識別名に含む          | `(parameterName)` |
| MethodAccessModifier | メソッドのpublic/private/protected修飾子をメソッド識別名に含む  | `public` |
| MethodStaticModifier | メソッドのstatic修飾子をメソッド識別名に含む  | `static` |
| MethodOverrideModifier | メソッドのoverride/abstract/virtual/sealed修飾子をメソッド識別名に含む | `sealed override` |
| AttributeArguments  | 属性のコンストラクタ引数をメソッド識別名に含む | `("AttributeParameter")` |
| AttributeProperties | 属性のプロパティをメソッド識別名に含む      | `(Property="AttributeProperty")` |
| AncestorDeclaringTypeAttribute | 親クラスの属性を再帰的に遡りメソッド識別名に含む<br/>DeclaringTypeAttributeが有効なときにのみ使用可能     | `[declaring:DeclaringAttribute]`<br/>以下のように再帰的に遡る<br/>`[declaring:DeclaringAttribute,AncestorDeclaringTypeAttribute]` |
| AssemblyFullName    | アセンブリの完全修飾名をメソッド識別名に含む<br/>AssemblyNameが有効なときにのみ使用可能  | `AssemblyFamily.AssemblyName`<br/>以下のようにフルネームになる<br/>`AssemblyFamily.AssemblyName, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null` | 
| TypeFullName        | 型の完全修飾名をメソッド識別名に含む<br/>いずれかのTypeNameが有効なときにのみ使用可能  | `DeclaringTypeName`他<br/>以下のようにフルネームになる<br/>`Namespace.DeclaringTypeName` |
| Simple              | 基本となる識別名 | N/A |
| LocalSignature      | アセンブリ内での識別名 | N/A |
| GlobalSignature     | グローバルな識別名 | N/A |
| All                 | 全ての要素をメソッド識別名に含む<br/>※アップデートにより挙動が変わる可能性があります。 | N/A |

##### メソッド識別名を確認する方法
メソッド識別名を確認したい場合、対象の関数に `OutputPointcutMethodName` 属性を付与してください。
```.cs
// 出力したい識別名をPointcutNameFlagで指定
[OutputPointcutMethodName(PointcutNameFlag.Simple)]
public void SampleMethod(int parameter)
{
    // メソッド本体
}
```
###### 出力先
`Logs/PointcutMethodName/[アセンブリ名]/[クラス名].txt`

## パラメーターのバインディング
### 基本的なバインディング
Adviceメソッドのパラメータに対象メソッドの引数名と同じ名前を付けることで、値をバインドできます。
```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice(int parameter1, string parameter2)
{
    Debug.Log($"parameter1: {parameter1}, parameter2: {parameter2}");
}
```

以下が注入先のメソッド
```.cs
public class SampleClass
{
    public static void TestMethod(int parameter1, string parameter2)
    {
        // メソッド本体の処理
    }
}
```

### 特殊なバインディング
Adviceメソッドのパラメータに以下の属性を付けることで、実行時の情報を取得できます。

#### PointcutThis

対象メソッドの`this`インスタンスを取得します。

```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice([PointcutThis] object self)
{
    Debug.Log($"instance type: {self.GetType().Name}");
}
```

#### PointcutMethod

対象メソッドの情報を取得します。

```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice([PointcutMethod] MethodBase method)
{
    Debug.Log($"method name: {method.Name}");
}
```

#### PointcutParameters

対象メソッドのパラメータを配列で取得します。

```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice([PointcutParameters] ParameterArray parameters)
{
    Debug.Log($"parameter count: {parameters.Length}");
}
```

#### PointcutReturned

対象メソッドの戻り値を取得します。
※AfterReturningでのみ使用可能

```.cs
[Advice(JoinPoint.AfterReturning)]
[RegexPointcut("^String$", PointcutNameFlag.ReturnTypeName)]
public static void AfterReturningAdvice([PointcutReturned] string returnValue)
{
    Debug.Log($"return value: {returnValue}");
}
```

#### PointcutThrown

スローされた例外を取得します。
※AfterThrowingでのみ使用可能

```.cs
[Advice(JoinPoint.AfterThrowing)]
[RegexPointcut(".*")]
public static void AfterThrowingAdvice([PointcutThrown] Exception exception)
{
    Debug.LogError($"exception: {exception.Message}");
}
```

#### PointcutGenericBind

ジェネリックパラメータのバインド方法を指定します。


```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(@"<T>(T value)", PointcutNameFlag.GenericArgumentName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.PointcutParameterName)]
public static void GenericAdvice<[PointcutGenericBind(GenericBinding.ParameterType)]T>(T value)
{
    Debug.Log($"generic argument: {typeof(T).Name}, value: {value}");
}
```

##### GenericBinding オプション

| BindingType | 説明                     |
|-------------|--------------------------|
| GenericParameterName | ジェネリックパラメータ名でバインドする。<br/>デフォルト挙動。 |
| ParameterType | パラメータの型として使われる場合に、暗黙的にバインドする。 |

## 高度な機能

### Unsafe Injection

引数にrefをつけることで、戻り値やパラメータを変更できます。

```.cs
[Advice(JoinPoint.AfterReturning, unsafeInjection: true)]
[RegexPointcut("^Int32(Int32 parameter)$", PointcutNameFlag.ReturnTypeName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.PointcutParameterName)]
public static void ModifyReturn(ref int parameter, [PointcutReturned] ref int returnValue)
{
    parameter = 42;  // 引数を変更
    returnValue = 999;  // 戻り値を変更
}
```

## アスペクトのブロック

特定のメソッドでアスペクトの適用を無効化できます。

```.cs
[BlockAspect(typeof(LoggingAspect))]
public void NoLoggingMethod()
{
    // このメソッドにはLoggingAspectが適用されません
}
```

以下の記法でAssembly全体にアスペクトの適用を無効化することも可能です。
```.cs
[assembly: BlockAspect(typeof(LoggingAspect))]
```

## パフォーマンスに関する注意

- ILPostProcessorによるコンパイル時のコード生成のため、実行時のオーバーヘッドは最小限です
- ただし、多数のアスペクトを適用するとコンパイル時間が増加する可能性があります

## サンプル: パフォーマンス測定

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

## サンプル: 例外ハンドリング

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
        // 例外をログに記録したり、エラー報告サービスに送信したりできます
    }
}
```

## 技術的な詳細

### アーキテクチャ

- **ILPostProcessor**: Unity.CompilationPipelineを使用してコンパイル時にILコードを変更
- **Mono.Cecil**: ILコードの読み取りと書き込みに使用
- **属性ベースの設定**: アスペクトとアドバイスの定義に属性を使用
