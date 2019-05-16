[![NuGet version](https://badge.fury.io/nu/DllCaller.svg)](https://www.nuget.org/packages/DllCaller)

# DllCaller
インターフェースから、PInvokeメソッドが実装されたクラスを動的に生成します。

## 特徴
* DllImportで指定するDLLのパスを実行時に解決できます
* SetDllDirectoryのためにディレクトリを分ける必要がありません
* 1つのインターフェースを書くだけで使用できます
    * 複数のDllImportやデリゲートを定義する必要はありません
* Marshal.GetDelegateForFunctionPointer()のデリゲートをキャッシュする必要はありません
* 比較的少ないオーバーヘッドでDLLを呼び出せます（約7%~30%:引数により変化）
* 冗長なDllImport属性を一部まとめられます

## 使い方
### インターフェースを定義
1. DLL(例:Test_x86.dll / Test_x64.dll)の関数のインターフェース(例:ITestDll)を作成します
1. インターフェースにNativeDllLocation属性を付けます
    * NativeDllLocation属性は、実行プロセスが32bit/64bitかどうかでリンクするDLLパスを選択します
1. インターフェースのメソッドにNativeMethod属性を付けます
    * NativeMethod属性は、DllImport属性とほぼ同等です
1. その他、メソッドやパラメーターに必要なMarshalAs属性等を付けます

```csharp
using DllCaller;

[NativeDllLocation(@"Test_x86.dll", @"Test_x64.dll")]
public interface ITestDll
{
    [NativeMethod(EntryPoint = "func_void", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    void FuncVoid();

    [NativeMethod(EntryPoint = "func_void_int", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    void FuncVoidInt(int i);

    [NativeMethod(EntryPoint = "func_bool_int", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    bool FuncBoolInt(int i);

    [NativeMethod(EntryPoint = "func_int_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    int FuncIntString([MarshalAs(UnmanagedType.LPWStr)] string str);
}
```

### インスタンスの作成（PInvokeメソッドの動的生成）
NativeDll.CreateInstance<T>()を呼び出してインスタンスを作成します。

このメソッドを呼び出すと、指定したインターフェースに定義されたメソッドと同等のPInvokeメソッド生成し、そのPInvokeメソッドを呼び出すようにインターフェースを実装したクラスの生成とインスタンス化を行います。

```csharp
public static class TestDll
{
    public static readonly ITestDll Instance = NativeDll.CreateInstance<ITestDll>();
}
```

以上で、TestDll.InstanceのメソッドからDLLの関数が呼び出されます。

### DLLパスをコードで解決する方法
NativeDllLocator属性で、コードでDLLパスを指定できます。

INativeDllLocatorインターフェースを実装するクラス(例:TestDllLocator)を作成します。
DLLパスを返すLocateメソッドを実装します。

```csharp
public class TestDllLocator : INativeDllLocator
{
    public string Locate(Type type)
    {
        return (Environment.Is64BitProcess ? "x64" : "x86") + "\\TestDll.dll";
    }
}
```

ITestDllインターフェースにNativeDllLocator属性を付け、TestDllLocatorクラスを指定します。

```csharp
[NativeDllLocator(typeof(TestDllLocator))]
public interface ITestDll
```

_注意: NativeDllLocation属性とNativeDllLocator属性は同時に使用できません。_

### NativeMethod属性の省略
GlobalNativeMethod属性で、各メソッドのNativeMethod属性の指定を省略できます。

```csharp
[NativeDllLocation(@"Test_x86.dll", @"Test_x64.dll")]
[GlobalNativeMethod(CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
public interface ITestDll
{
    void func_void();
    void func_void_int(int i);
    
    [return: MarshalAs(UnmanagedType.Bool)]
    bool func_bool_int(int i);

    int func_int_string([MarshalAs(UnmanagedType.LPWStr)] string str);
}
```

GlobalNativeMethod属性を付けた状態で、メソッドにNativeMethod属性を付けた場合は、NativeMethod属性が優先されます。

### 生成されるクラスの確認
NativeDll.CreateInstance<T>(bool assemblyOutput)メソッドにtrueを指定すると、動的に生成される型を含んだアセンブリが出力されます。
出力されるアセンブリの名前は、'指定されたインターフェース名Impl.dll'となります。

ildasmやILSpy等でこのアセンブリを読み込むことにより、生成されたクラスを確認できます。
