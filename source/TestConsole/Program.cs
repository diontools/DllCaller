using DllCaller;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Text;

namespace TestConsole
{
    static class Program
    {
        private static void Test(TestPattern testPattern, int times, int averaging, bool ignoreFirst)
        {
            if (ignoreFirst)
            {
                foreach (var action in testPattern.Actions)
                {
                    action();
                }
            }

            long firstTime = -1;

            foreach (var action in testPattern.Actions)
            {
                //GC.Collect();
                //GC.WaitForPendingFinalizers();

                long totalTime = 0;

                for (int avg = 0; avg < averaging; avg++)
                {
                    Console.Write("Test");
                    Console.Write(avg + 1);

                    var sw = Stopwatch.StartNew();

                    for (int i = 0; i < times; i++)
                    {
                        action();
                    }

                    sw.Stop();

                    totalTime += sw.Elapsed.Ticks;

                    Console.CursorLeft = 0;
                }

                if (firstTime < 0)
                {
                    firstTime = totalTime;
                }

                Console.WriteLine(
                    "{0,25} {1:#,0.000}[ms/test] {2:0.000000000}[ns/call] {3:0.000%}",
                    testPattern.Name,
                    new TimeSpan(totalTime / averaging).TotalMilliseconds,
                    new TimeSpan(totalTime / averaging).TotalMilliseconds * 1000 / times,
                    (totalTime / (double)firstTime));
            }
        }

        class TestPattern
        {
            public string Name { get; set; }
            public Action[] Actions { get; set; }
        }

        static void Main()
        {
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High;
            System.Threading.Thread.CurrentThread.Priority = System.Threading.ThreadPriority.Highest;

            TestPattern[] testPatters =
            {
#region Normal
                new TestPattern
                {
                    Name = "void",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoid(),
                        () => TestDll.Instance.FuncVoid(),
                    }
                },
                new TestPattern
                {
                    Name = "void(int)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt(1234),
                        () => TestDll.Instance.FuncVoidInt(1234),
                    }
                },
                new TestPattern
                {
                    Name = "int(string)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncIntString("abc"),
                        () => TestDll.Instance.FuncIntString("abc"),
                    }
                },
                new TestPattern
                {
                    Name = "bool(int)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncBoolInt(1),
                        () => TestDll.Instance.FuncBoolInt(1),
                    }
                },
                new TestPattern
                {
                    Name = "bool(string,int)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncBoolStringInt("abc", 1234),
                        () => TestDll.Instance.FuncBoolStringInt("abc", 1234),
                    }
                },
                new TestPattern
                {
                    Name = "int(string,int,char,int)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncIntArg4("abc", 1234, 'A', 456),
                        () => TestDll.Instance.FuncIntArg4("abc", 1234, 'A', 456),
                    }
                },
                new TestPattern
                {
                    Name = "void(BigStructure)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidBigStructure(new BigStructure()),
                        () => TestDll.Instance.FuncVoidBigStructure(new BigStructure()),
                    }
                },
#endregion

#region ParamLen
                new TestPattern
                {
                    Name = "void(int1)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt1(1),
                        () => TestDll.Instance.FuncVoidInt1(1),
                    }
                },
                new TestPattern
                {
                    Name = "void(int2)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt2(1,2),
                        () => TestDll.Instance.FuncVoidInt2(1,2),
                    }
                },
                new TestPattern
                {
                    Name = "void(int3)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt3(1,2,3),
                        () => TestDll.Instance.FuncVoidInt3(1,2,3),
                    }
                },
                new TestPattern
                {
                    Name = "void(int4)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt4(1,2,3,4),
                        () => TestDll.Instance.FuncVoidInt4(1,2,3,4),
                    }
                },
                new TestPattern
                {
                    Name = "void(int5)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt5(1,2,3,4,5),
                        () => TestDll.Instance.FuncVoidInt5(1,2,3,4,5),
                    }
                },
                new TestPattern
                {
                    Name = "void(int6)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt6(1,2,3,4,5,6),
                        () => TestDll.Instance.FuncVoidInt6(1,2,3,4,5,6),
                    }
                },
                new TestPattern
                {
                    Name = "void(int7)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt7(1,2,3,4,5,6,7),
                        () => TestDll.Instance.FuncVoidInt7(1,2,3,4,5,6,7),
                    }
                },
                new TestPattern
                {
                    Name = "void(int8)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt8(1,2,3,4,5,6,7,8),
                        () => TestDll.Instance.FuncVoidInt8(1,2,3,4,5,6,7,8),
                    }
                },
                new TestPattern
                {
                    Name = "void(int9)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt9(1,2,3,4,5,6,7,8,9),
                        () => TestDll.Instance.FuncVoidInt9(1,2,3,4,5,6,7,8,9),
                    }
                },
                new TestPattern
                {
                    Name = "void(int10)",
                    Actions = new Action[]
                    {
                        () => TestDllImport.FuncVoidInt10(1,2,3,4,5,6,7,8,9,10),
                        () => TestDll.Instance.FuncVoidInt10(1,2,3,4,5,6,7,8,9,10),
                    }
                },
#endregion
            };

            const int TestTimes = 50000000;
            const int TestAveraging = 5;
            const bool IgnoreFirst = true;

            try
            {
                for (; ; )
                {
                    Console.WriteLine("TestTimes:{0}[call/test] Averaging:{1} IgnoreFirstCall:{2}", TestTimes, TestAveraging, IgnoreFirst);

                    foreach (var tp in testPatters)
                    {
                        Test(tp, TestTimes, TestAveraging, IgnoreFirst);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadLine();
            }
        }

        private static unsafe void Abc(string str)
        {
            fixed (char* p = str)
            {
                TestDllImport.FuncIntStringPtr((IntPtr)p);
            }
        }
    }


    [NativeDllLocation(@"x86\TestDll.dll", @"x64\TestDll.dll")]
    //[NativeDllLocator(typeof(TestDllLocator))]
    //[GlobalNativeMethod()]
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
        //[return: MarshalAs(UnmanagedType.Bool)]
        int FuncIntString(string str);

        [NativeMethod(EntryPoint = "func_bool_string_int", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        bool FuncBoolStringInt([MarshalAs(UnmanagedType.LPWStr)] string abc, [Out] int xyz);

        [NativeMethod(EntryPoint = "func_int_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        int FuncStrCount([MarshalAs(UnmanagedType.LPWStr)] string abc);

        [NativeMethod(EntryPoint = "func_int_string_int_char_int", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        int FuncIntArg4([MarshalAs(UnmanagedType.LPWStr)] string abc, int xyz, char c, int val);

        [NativeMethod(EntryPoint = "func_int_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        int FuncIntStringPtr(IntPtr str);


        [NativeMethod(EntryPoint = "func_void_int1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt1(int v1);

        [NativeMethod(EntryPoint = "func_void_int2", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt2(int v1, int v2);

        [NativeMethod(EntryPoint = "func_void_int3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt3(int v1, int v2, int v3);

        [NativeMethod(EntryPoint = "func_void_int4", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt4(int v1, int v2, int v3, int v4);

        [NativeMethod(EntryPoint = "func_void_int5", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt5(int v1, int v2, int v3, int v4, int v5);

        [NativeMethod(EntryPoint = "func_void_int6", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt6(int v1, int v2, int v3, int v4, int v5, int v6);

        [NativeMethod(EntryPoint = "func_void_int7", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt7(int v1, int v2, int v3, int v4, int v5, int v6, int v7);

        [NativeMethod(EntryPoint = "func_void_int8", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt8(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8);

        [NativeMethod(EntryPoint = "func_void_int9", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt9(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9);

        [NativeMethod(EntryPoint = "func_void_int10", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidInt10(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9, int v10);


        [NativeMethod(EntryPoint = "func_void_bigStructure", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        void FuncVoidBigStructure(BigStructure bs);
    }

    public class ImplTestDll : ITestDll
    {
        public void FuncVoid()
        {
            TestDllImport.FuncVoid();
        }

        public void FuncVoidInt(int i)
        {
            TestDllImport.FuncVoidInt(i);
        }

        public bool FuncBoolInt(int i)
        {
            return TestDllImport.FuncBoolInt(i);
        }

        public int FuncIntString(string str)
        {
            return TestDllImport.FuncIntString(str);
        }

        public bool FuncBoolStringInt(string abc, int xyz)
        {
            return TestDllImport.FuncBoolStringInt(abc, xyz);
        }

        public int FuncStrCount(string abc)
        {
            return TestDllImport.FuncStrCount(abc);
        }


        public int FuncIntArg4(string abc, int xyz, char c, int val)
        {
            return TestDllImport.FuncIntArg4(abc, xyz, c, val);
        }


        public int FuncIntStringPtr(IntPtr str)
        {
            throw new NotImplementedException();
        }



        public void FuncVoidInt1(int v1)
        {
            TestDllImport.FuncVoidInt1(v1);
        }

        public void FuncVoidInt2(int v1, int v2)
        {
            TestDllImport.FuncVoidInt2(v1, v2);
        }

        public void FuncVoidInt3(int v1, int v2, int v3)
        {
            TestDllImport.FuncVoidInt3(v1, v2, v3);
        }

        public void FuncVoidInt4(int v1, int v2, int v3, int v4)
        {
            TestDllImport.FuncVoidInt4(v1, v2, v3, v4);
        }

        public void FuncVoidInt5(int v1, int v2, int v3, int v4, int v5)
        {
            TestDllImport.FuncVoidInt5(v1, v2, v3, v4, v5);
        }

        public void FuncVoidInt6(int v1, int v2, int v3, int v4, int v5, int v6)
        {
            TestDllImport.FuncVoidInt6(v1, v2, v3, v4, v5, v6);
        }

        public void FuncVoidInt7(int v1, int v2, int v3, int v4, int v5, int v6, int v7)
        {
            TestDllImport.FuncVoidInt7(v1, v2, v3, v4, v5, v6, v7);
        }

        public void FuncVoidInt8(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8)
        {
            TestDllImport.FuncVoidInt8(v1, v2, v3, v4, v5, v6, v7, v8);
        }

        public void FuncVoidInt9(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9)
        {
            TestDllImport.FuncVoidInt9(v1, v2, v3, v4, v5, v6, v7, v8, v9);
        }

        public void FuncVoidInt10(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9, int v10)
        {
            TestDllImport.FuncVoidInt10(v1, v2, v3, v4, v5, v6, v7, v8, v9, v10);
        }


        public void FuncVoidBigStructure(BigStructure bs)
        {
            TestDllImport.FuncVoidBigStructure(bs);
        }
    }


    public class TestDll
    {
        public static readonly ITestDll Instance = NativeDll.CreateInstance<ITestDll>(true);
    }

    public class TestDllLocator : INativeDllLocator
    {
        public string Locate(Type type)
        {
            return (Environment.Is64BitProcess ? "x64" : "x86") + "\\TestDll.dll";
        }
    }


    public static class TestDllImport
    {
        private const string DllName = "x64\\TestDll.dll";

        [DllImport(DllName, EntryPoint = "func_void", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoid();

        [DllImport(DllName, EntryPoint = "func_void_int", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt(int i);

        [DllImport(DllName, EntryPoint = "func_bool_int", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FuncBoolInt(int i);

        [DllImport(DllName, EntryPoint = "func_int_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int FuncIntString(string str);

        [DllImport(DllName, EntryPoint = "func_bool_string_int", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool FuncBoolStringInt([MarshalAs(UnmanagedType.LPWStr)] string abc, [Out] int xyz);

        [DllImport(DllName, EntryPoint = "func_int_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int FuncStrCount([MarshalAs(UnmanagedType.LPWStr)] string abc);

        [DllImport(DllName, EntryPoint = "func_int_string_int_char_int", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int FuncIntArg4([MarshalAs(UnmanagedType.LPWStr)] string abc, int xyz, char c, int val);

        [DllImport(DllName, EntryPoint = "func_int_string", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern int FuncIntStringPtr(IntPtr str);


        [DllImport(DllName, EntryPoint = "func_void_int1", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt1(int v1);

        [DllImport(DllName, EntryPoint = "func_void_int2", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt2(int v1, int v2);

        [DllImport(DllName, EntryPoint = "func_void_int3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt3(int v1, int v2, int v3);

        [DllImport(DllName, EntryPoint = "func_void_int4", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt4(int v1, int v2, int v3, int v4);

        [DllImport(DllName, EntryPoint = "func_void_int5", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt5(int v1, int v2, int v3, int v4, int v5);

        [DllImport(DllName, EntryPoint = "func_void_int6", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt6(int v1, int v2, int v3, int v4, int v5, int v6);

        [DllImport(DllName, EntryPoint = "func_void_int7", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt7(int v1, int v2, int v3, int v4, int v5, int v6, int v7);

        [DllImport(DllName, EntryPoint = "func_void_int8", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt8(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8);

        [DllImport(DllName, EntryPoint = "func_void_int9", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt9(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9);

        [DllImport(DllName, EntryPoint = "func_void_int10", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidInt10(int v1, int v2, int v3, int v4, int v5, int v6, int v7, int v8, int v9, int v10);


        [DllImport(DllName, EntryPoint = "func_void_bigStructure", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern void FuncVoidBigStructure(BigStructure bs);
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct BigStructure
    {
        unsafe fixed byte bytes[1024];
    }
}
 