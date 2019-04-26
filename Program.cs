using System;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Runtime.Loader;
using System.Reflection;
using System.IO;
using Microsoft.CodeAnalysis.Emit;

namespace SpikeCompilationAtRuntime
{
    class Program
    {
        static void Main(string[] args)
        {
            const string code = @"using System;
using System.IO;
namespace RoslynCore
{
 public static class Helper
 {
  public static double CalculateCircleArea(double radius)
  {
    return radius * radius * Math.PI;
  }
  }
}";
            var fileName = "a.dll";
            var tree = SyntaxFactory.ParseSyntaxTree(code);
            // Detect the file location for the library that defines the object type
            var systemRefLocation = typeof(object).GetTypeInfo().Assembly.Location;
            // Create a reference to the library
            var systemReference = MetadataReference.CreateFromFile(systemRefLocation);
            // A single, immutable invocation to the compiler
            // to produce a library
            var compilation = CSharpCompilation.Create(fileName)
              .WithOptions(
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
              .AddReferences(systemReference)
              .AddSyntaxTrees(tree);

            string path = Path.Combine(Directory.GetCurrentDirectory(), fileName);
            EmitResult compilationResult = compilation.Emit(path);

            if (compilationResult.Success)
            {
                var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(path);
                var watch = System.Diagnostics.Stopwatch.StartNew();   
                double radius = 10;
                object result =
                  asm.GetType("RoslynCore.Helper").GetMethod("CalculateCircleArea").
                  Invoke(null, new object[] { radius });
                watch.Stop();
                Console.WriteLine($"Circle area with radius = {radius} is {result}");
                Console.WriteLine($"Elapsed {watch.ElapsedMilliseconds} miliseconds");
            }




        }
    }
}
