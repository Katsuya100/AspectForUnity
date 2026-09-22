# AspectForUnity
## 概要

AspectForUnityは、Unityプロジェクトにアスペクト指向プログラミング(AOP)の機能を提供します。  
ILPostProcessorを使用して、メソッドの前後に処理を挿入することができます。  
それによりログ出力、パフォーマンス測定、例外処理などの横断的関心事を、ビジネスロジックから分離して実装できます。  
対象メソッドのILはコンパイル時に書き換えられ、実行時には挿入済みのAdviceが通常のメソッド呼び出しとして動作します。  


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
- **JoinPoint.Around**: メソッド実行をラップし、元のメソッドを実行するタイミングを制御
- **正規表現によるPointcut**: メソッド名やクラス名などを正規表現でマッチング
- **直接指定Pointcut**: メソッド名、宣言型、属性、ジェネリックパラメータ、パラメータ型、戻り値型を直接指定してマッチング
- **パラメータバインディング**: メソッドの引数/型引数/戻り値のバインディング
- **Proceeding Context**: Around Adviceから元のメソッドを実行し、戻り値を参照または変更
- **非同期実行シーケンススコープ**: 1つの論理的な非同期実行シーケンスにまたがってAdviceを適用
- **Struct Aspect**: クラスまたは構造体にAspectを定義
- **Unsafe Injection**: 戻り値やパラメータの変更
- **適用ブロック**: Assembly、Module、型、メソッド単位でAspectの適用を抑止

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
クラスまたは構造体に`Aspect`属性を付与してアスペクトを定義します。

```.cs
using Katuusagi.AspectForUnity;

[Aspect]
public static class LoggingAspect
{
}
```

`[Aspect]`は構造体にも付与できます。

### 2. Adviceメソッドの実装
Aspectクラスまたは構造体内に `public`、戻り値 `void` のAdviceメソッドを実装し、`Advice`属性とPointcut属性を付与します。各Adviceには、メソッドまたはその宣言型に1つ以上のPointcut属性が必要です。
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

### Around

Around Adviceは対象メソッドの実行をラップします。Adviceメソッドに`[PointcutProceed]`を付けたパラメータをちょうど1つ用意し、`Proceed()`を呼び出すことで次のAround Adviceまたは元のメソッドを実行します。Around Adviceでは`Proceed()`を必ず1回だけ呼び出してください。

戻り値が`void`のメソッドには`ProceedingContext`を使用します。

```.cs
[Advice(JoinPoint.Around)]
[MethodNamePointcut("TestMethod")]
public static void AroundAdvice([PointcutProceed] ProceedingContext proceed)
{
    Debug.Log("before");
    proceed.Proceed();
    Debug.Log("after");
}
```

戻り値があるメソッドには`ProceedingContext<T>`を使用し、`Proceed()`の後に結果を参照できます。

```.cs
[Advice(JoinPoint.Around)]
[MethodNamePointcut("GetValue")]
public static void AroundAdvice([PointcutProceed] ProceedingContext<int> proceed)
{
    proceed.Proceed();
    Debug.Log($"return value: {proceed.ReturnValue}");
}
```

`unsafeInjection: true`を指定すると、`UnsafeProceedingContext<T>`の書き込み可能な`ReturnValue`で結果を変更できます。

## Pointcut属性

Pointcut属性は、Adviceメソッドが適用されるメソッドを指定します。  
複数条件を設定でき、AND条件でマッチングされます。

### 直接指定Pointcut

以下のPointcutは、正規表現を作成せずにメソッドのメタデータへマッチングします。複数のPointcutを指定した場合はAND条件でマッチングされます。

| Pointcut | マッチング対象 |
| --- | --- |
| `MethodNamePointcut` | 対象メソッド名 |
| `DeclaringTypePointcut` | 宣言型の名前または型 |
| `AttributeTypePointcut` | 対象メソッドに付与された属性型 |
| `DeclaringAttributeTypePointcut` | 宣言型に付与された属性型 |
| `GenericParameterNamePointcut` | ジェネリックパラメータ名。繰り返しインデックスに対応 |
| `ParameterTypePointcut` | パラメータ型の名前または型。繰り返しインデックスに対応 |
| `ReturnTypePointcut` | 戻り値型の名前または型 |

例えば、以下のように指定できます。

```.cs
[Advice(JoinPoint.Before)]
[MethodNamePointcut("GetValue")]
[ReturnTypePointcut(typeof(int))]
public static void BeforeGetValue()
{
    // int GetValueメソッドに対する処理
}
```

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
```
AssemblyName[assembly:AssemblyAttribute][module:ModuleAttribute][declaring:DeclaringAttribute][return:ReturnAttribute][MethodAttribute("AttributeParameter",Property="AttributeProperty")]public static sealed override ReturnType DeclaringTypeName<[DeclaringGenericAttribute]TDeclaring>::MethodName<[GenericAttribute]TMethod>([ParameterAttribute]ParameterType parameterName)
```
メソッド識別名の各要素は以下のように対応します。  


##### PointcutNameFlag オプション

| Flag                  | 説明                         | 上記メソッド識別名例内の部品 |
|-----------------------|------------------------------| --------------------------------|
| AssemblyAttribute    | アセンブリ属性をメソッド識別名に含む       | `[assembly:AssemblyAttribute]` |
| AssemblyName         | アセンブリ名をメソッド識別名に含む         | `AssemblyName` |
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
| AssemblyFullName    | アセンブリの完全修飾名をメソッド識別名に含む<br/>AssemblyNameが有効なときにのみ使用可能  | `AssemblyName`<br/>以下のようにフルネームになる<br/>`AssemblyName, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null` | 
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
Adviceメソッドのパラメータに挿入先メソッドの引数名と同じ名前を付けることで、値をバインドできます。
```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(".*")]
public static void BeforeAdvice(int parameter1, string parameter2)
{
    Debug.Log($"parameter1: {parameter1}, parameter2: {parameter2}");
}
```

以下が挿入先のメソッド
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

#### PointcutProceed

Around Adviceの実行コンテキストを取得します。パラメータに`[PointcutProceed]`を付与し、戻り値が`void`のメソッドには`ProceedingContext`、戻り値があるメソッドには`ProceedingContext<T>`を使用します。

`Proceed()`は次のAround Adviceまたは対象メソッドを実行します。必ず1回だけ呼び出してください。`ProceedingContext<T>.ReturnValue`は`Proceed()`後に読み取れます。`unsafeInjection: true`を指定した場合は`UnsafeProceedingContext<T>`で戻り値を変更できます。

#### PointcutGenericBind

ジェネリックパラメータのバインド方法を指定します。


```.cs
[Advice(JoinPoint.Before)]
[RegexPointcut(@"<T>\(T value\)", PointcutNameFlag.GenericArgumentName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.ParameterName)]
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
| ReturnType | 対象メソッドの戻り値型からジェネリック引数を推論する。 |

## 高度な機能

### Unsafe Injection

引数にrefをつけることで、戻り値やパラメータを変更できます。

```.cs
[Advice(JoinPoint.AfterReturning, unsafeInjection: true)]
[RegexPointcut(@"^Int32\(Int32 parameter\)$", PointcutNameFlag.ReturnTypeName | PointcutNameFlag.ParameterTypeName | PointcutNameFlag.ParameterName)]
public static void ModifyReturn(ref int parameter, [PointcutReturned] ref int returnValue)
{
    parameter = 42;  // 引数を変更
    returnValue = 999;  // 戻り値を変更
}
```

### Adviceのスコープ

Adviceのデフォルトは`AdviceScope.Invocation`で、メソッド呼び出しの境界ごとに適用されます。`AdviceScope.AsyncExecutionSequence`を指定すると、awaitやyieldによる中断・再開を含む1つの論理的な非同期実行シーケンスに対してAdviceを適用できます。

```.cs
[Advice(JoinPoint.Before, AdviceScope.AsyncExecutionSequence)]
[MethodNamePointcut("RunAsync")]
public static void BeforeAsyncSequence()
{
    Debug.Log("async sequence started");
}
```

`AsyncExecutionSequence`ではAround Adviceはサポートされません。

### Aspectの適用範囲を明示化
#### Assembly内と参照元にのみ適用
Assembly内にAspectを定義することで外部Assemblyに影響を与えなくなる。  
ただし、Aspectが定義されたAssemblyを参照している他のAssemblyには適用される。

#### すべてのAssemblyに適用
1. `packages/Runtime/AspectEntry/AspectEntry.asmdef` を参照するAssembly Definition Reference（`.asmref`）を作成する。  
2. その `.asmref` と同じAssemblyにAspectクラスを配置する。
3. すべてのAssemblyDefinitionにアスペクトが適用されます。

### Aspectの適用を拒否する

特定のメソッドでAspectの適用を無効化できます。

```.cs
[BlockAspect]
public void NoLoggingMethod()
{
    // このメソッドにはLoggingAspectが適用されません
}
```

以下の記法でAssembly全体にアスペクトの適用を無効化することも可能です。
```.cs
[assembly: BlockAspect]
```

`BlockAspect` はAssembly、Module、Class、Struct、Enum、Method、Constructorに指定でき、指定した範囲へのすべてのAspectの適用をブロックします。

## パフォーマンスに関する注意

- Pointcutの正規表現判定とIL書き換えはコンパイル時に行われ、実行時の正規表現判定やプロキシ呼び出しはありません
- 実行時にはAdvice呼び出し自体のコストがあり、`PointcutMethod` は `MethodBase` の取得、`PointcutParameters` は引数のボックス化とプール配列への格納を伴います
- ただし、多数のアスペクトを適用するとコンパイル時間が増加する可能性があります

## サンプル: パフォーマンス測定

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

## サンプル: 例外ハンドリング

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
        // 例外をログに記録したり、エラー報告サービスに送信したりできます
    }
}
```

## 技術的な詳細

### アーキテクチャ

- **ILPostProcessor**: Unity.CompilationPipelineを使用してコンパイル時にILコードを変更
- **Mono.Cecil**: ILコードの読み取りと書き込みに使用
- **属性ベースの設定**: アスペクトとアドバイスの定義に属性を使用
- **対象Assembly**: `Katuusagi.AspectForUnity` を参照するAssemblyを処理

### Adviceの制約

- Adviceは `[Aspect]` クラスまたは構造体に `public void`（staticなら `public static void`）として宣言し、`out` パラメータは使用できません。
- static Aspectはそのまま利用できます。インスタンスAspectは抽象型・ジェネリック型にできず、対象メソッドごとに一致する `[Advice(JoinPoint.Before)]` 付き `public` コンストラクタがちょうど1つ必要です。そのAspectのインスタンスAdviceは、コンストラクタが生成したインスタンスを共有します。
- Around Adviceには`[PointcutProceed]`パラメータがちょうど1つ必要で、`Proceed()`を必ず1回だけ呼び出してください。`PointcutReturned`と`PointcutThrown`は使用できません。
- Around Adviceはコンストラクタには適用できず、`AsyncExecutionSequence`のAdviceにも適用できません。
- Adviceの宣言や対象メソッドとの型バインディングに誤りがある場合は、Unityのコンパイルログにエラーが出力されます。
