using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = ParallelizableAnalyzer.Test.CSharpCodeFixVerifier<
    ParallelizableAnalyzer.ParallelizableAnalyzerAnalyzer,
    ParallelizableAnalyzer.ParallelizableAnalyzerCodeFixProvider>;

namespace ParallelizableAnalyzer.Test;

[TestClass]
public class ParallelizableAnalyzerUnitTest
{
    [TestMethod]
    public async Task NoDiagnosticsOnEmptyInputAsync()
    {
        var test = @"";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [TestMethod]
    public async Task NoDiagnosticsOnMethodWithSingleAwaitAsync()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            public async Task TestMethod()
            {
                await Task.Delay(100);
            }
        }
    }";

        await VerifyCS.VerifyAnalyzerAsync(test);
    }

    [TestMethod]
    public async Task DiagnosticsOnMethodWithTwoAwaitsAsync()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            public async Task TestMethod()
            {
                await Task.Delay(100);
                await Task.Delay(200);
            }
        }
    }";
        DiagnosticResult expected = VerifyCS.Diagnostic().WithSpan(9, 31, 9, 41).WithArguments("TestMethod");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task DiagnosticsOnAwaitWithinForAsync()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            public async Task TestMethod()
            {
                for (int i = 0; i < 5; i++)
                {
                    await Task.Delay(100);
                }
            }
        }
    }";
        DiagnosticResult expected = VerifyCS.Diagnostic().WithSpan(9, 31, 9, 41).WithArguments("TestMethod");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task DiagnosticsOnAwaitWithinForeachAsync()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            private static int[] numbers = new int[] { 1, 2, 3, 4, 5 };

            public async Task TestMethod()
            {
                foreach (int i in numbers)
                {
                    await Task.Delay(100);
                }
            }
        }
    }";
        DiagnosticResult expected = VerifyCS.Diagnostic().WithSpan(11, 31, 11, 41).WithArguments("TestMethod");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task DiagnosticsOnAwaitWithinWhileAsync()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            public async Task TestMethod()
            {
                while (true)
                {
                    await Task.Delay(100);
                }
            }
        }
    }";
        DiagnosticResult expected = VerifyCS.Diagnostic().WithSpan(9, 31, 9, 41).WithArguments("TestMethod");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task DiagnosticsOnAwaitWithinDoWhileForAsync()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            public async Task TestMethod()
            {
                do
                {
                    await Task.Delay(100);
                }
                while (true);
            }
        }
    }";
        DiagnosticResult expected = VerifyCS.Diagnostic().WithSpan(9, 31, 9, 41).WithArguments("TestMethod");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task DiagnosticsOnAwaitsBothInAndOutOfLoops()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            public async Task TestMethod()
            {
                await Task.Delay(100);

                while (true)
                {
                    await Task.Delay(200);
                    await Task.Delay(300);
                }
            }
        }
    }";
        DiagnosticResult expected = VerifyCS.Diagnostic().WithSpan(9, 31, 9, 41).WithArguments("TestMethod");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task DiagnosticsOnAwaitWithinLoopInConstructorAsync()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            public TestClass()
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(100);
                    }
                });
            }
        }
    }";
        DiagnosticResult expected = VerifyCS.Diagnostic().WithSpan(9, 20, 9, 29).WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }

    [TestMethod]
    public async Task DiagnosticsOnConstructorWithTwoAwaitsAsync()
    {
        var test = @"
    using System;
    using System.Threading.Tasks;

    namespace TestNamespace
    {
        class TestClass
        {
            public TestClass()
            {
                Task.Run(async () =>
                {
                    while (true)
                    {
                        await Task.Delay(100);
                    }
                });
            }
        }
    }";
        DiagnosticResult expected = VerifyCS.Diagnostic().WithSpan(9, 20, 9, 29).WithArguments("TestClass");

        await VerifyCS.VerifyAnalyzerAsync(test, expected);
    }
}
