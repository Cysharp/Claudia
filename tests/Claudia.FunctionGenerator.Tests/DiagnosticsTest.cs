namespace Claudia.FunctionGenerator.Tests;

public class DiagnosticsTest
{
    public class GeneratorDiagnosticsTest(ITestOutputHelper output)
    {
        void Compile(int id, string code, bool allowMultipleError = false)
        {   
            var diagnostics = CSharpGeneratorRunner.RunGenerator(code);

            // ignore CS0759: No defining declaration found for implementing declaration of partial method 'method'.
            // ignore CS8795: Partial method 'method' must have an implementation part because it has accessibility modifiers.
            diagnostics = diagnostics.Where(x => x.Id != "CS0759" && x.Id != "CS8795").ToArray();

            foreach (var item in diagnostics)
            {
                output.WriteLine(item.ToString());
            }

            if (!allowMultipleError)
            {
                diagnostics.Length.Should().Be(1);
                diagnostics[0].Id.Should().Be("CLFG" + id.ToString("000"));
            }
            else
            {
                diagnostics.Select(x => x.Id).Should().Contain("CLFG" + id.ToString("000"));
            }
        }

        [Fact]
        public void CLFG001_MuestBePartial()
        {
            Compile(1, """
using Claudia;

public static class Hoge
{
    [ClaudiaFunction]
    public static int Method(int x) => x;
}
""");
        }

        [Fact]
        public void CLFG002_NestedNotAllow()
        {
            Compile(2, """
using Claudia;

public static partial class Hoge
{
    public static partial class Fuga
    {
        [ClaudiaFunction]
        public static int Method(int x) => x;
    }
}
""");
        }

        [Fact]
        public void CLFG003_GenericTypeNotSupported()
        {
            Compile(3, """
using Claudia;

public static partial class Hoge<T>
{
    [ClaudiaFunction]
    public static int Method(int x) => x;
}
""");
        }

        [Fact]
        public void CLFG004_MustBeStatic()
        {
            Compile(4, """
using Claudia;

public partial class Hoge
{
    [ClaudiaFunction]
    public static int Method(int x) => x;
}
""");
        }

        [Fact]
        public void CLFG005_MethodNeedsDocumentationCommentXml()
        {
            Compile(5, """
using Claudia;

public static partial class Hoge
{
    [ClaudiaFunction]
    public static int Method(int x) => x;
}
""");
        }

        [Fact]
        public void CLFG006_MethodNeedsSummary()
        {
            Compile(6, """
using Claudia;

public static partial class Hoge
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="x">X1</param>
    [ClaudiaFunction]
    public static int Method(int x) => x;
}
""");
        }

        [Fact]
        public void CLFG007_ParameterNeedsDescription()
        {
            Compile(7, """
using Claudia;

public static partial class Hoge
{
    /// <summary>
    /// Foo
    /// </summary>
    /// <param name="x"></param>
    [ClaudiaFunction]
    public static int Method(int x) => x;
}
""", true);
        }

        [Fact]
        public void CLFG008_AllParameterNeedsDescription()
        {
            Compile(8, """
using Claudia;

public static partial class Hoge
{
    /// <summary>
    /// Foo
    /// </summary>
    [ClaudiaFunction]
    public static int Method(int x) => x;
}
""", true);
        }

        [Fact]
        public void CLFG009_ParameterTypeIsNotSupported()
        {
            // void return
            Compile(9, """
using Claudia;

public static partial class Hoge
{
    /// <summary>
    /// Foo
    /// </summary>
    /// <param name="x">X2</param>
    [ClaudiaFunction]    
    public static int Method(System.Exception x) => 0;
}
""");
        }

        [Fact]
        public void CLFG010_VoidReturnIsNotSupported()
        {
            Compile(10, """
using Claudia;

public static partial class Hoge
{
    /// <summary>
    /// Foo
    /// </summary>
    /// <param name="x">X2</param>
    [ClaudiaFunction]    
    public static void Method1(int x){ }
}
""");

            Compile(10, """
using Claudia;
using System.Threading.Tasks;

public static partial class Hoge
{
    /// <summary>
    /// Foo
    /// </summary>
    /// <param name="x">X2</param>
    [ClaudiaFunction]    
    public static Task Method2(int x) => Task.CompletedTask;
}
""");

            Compile(10, """
using Claudia;
using System.Threading.Tasks;

public static partial class Hoge
{
    /// <summary>
    /// Foo
    /// </summary>
    /// <param name="x">X2</param>
    [ClaudiaFunction]    
    public static ValueTask Method3(int x) => ValueTask.CompletedTask;
}
""");



        }
    }
}
