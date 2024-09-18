using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.CodeAnalysis.Testing;
using VerifyCS = ParallelizableAnalyzer.Test.CSharpCodeFixVerifier<
    ParallelizableAnalyzer.ParallelizableAnalyzerAnalyzer,
    ParallelizableAnalyzer.ParallelizableAnalyzerCodeFixProvider>;

namespace ParallelizableAnalyzer.Test
{
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
            DiagnosticResult[] expected = new[]
            {
                VerifyCS.Diagnostic().WithSpan(11, 17, 11, 38).WithArguments("Task.Delay", "100"),
                VerifyCS.Diagnostic().WithSpan(12, 17, 12, 38).WithArguments("Task.Delay", "200"),
            };

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task DiagnosticsOnAwaitsWithinForAsync()
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
            DiagnosticResult expected = VerifyCS.Diagnostic().WithLocation(13, 21).WithArguments("Task.Delay", "100");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task DiagnosticsOnAwaitsWithinForeachAsync()
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
            DiagnosticResult expected = VerifyCS.Diagnostic().WithLocation(15, 21).WithArguments("Task.Delay", "100");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task DiagnosticsOnAwaitsWithinWhileAsync()
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
            DiagnosticResult expected = VerifyCS.Diagnostic().WithLocation(13, 21).WithArguments("Task.Delay", "100");

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }

        [TestMethod]
        public async Task DiagnosticsOnAwaitsWithinDoWhileForAsync()
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
            DiagnosticResult expected = VerifyCS.Diagnostic().WithLocation(13, 21).WithArguments("Task.Delay", "100");

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

            DiagnosticResult[] expected = new[]
            {
                VerifyCS.Diagnostic().WithSpan(11, 17, 11, 38).WithArguments("Task.Delay", "100"),
                VerifyCS.Diagnostic().WithSpan(15, 21, 15, 42).WithArguments("Task.Delay", "200"),
                VerifyCS.Diagnostic().WithSpan(16, 21, 16, 42).WithArguments("Task.Delay", "300"),
            };

            await VerifyCS.VerifyAnalyzerAsync(test, expected);
        }
    }
}
