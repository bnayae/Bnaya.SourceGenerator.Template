#pragma warning disable HAA0301 // Closure Allocation Source
#pragma warning disable HAA0601 // Value type to reference type conversion causing boxing allocation
#pragma warning disable HAA0401 // Possible allocation of reference type enumerator
using System.Collections.Immutable;
using System.Text;

using Skeleton.BuilderPatternGeneration;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Skeleton.Generators;

[Generator]
public partial class Generator : AttributeGeneratorBase
{
    protected override string TargetAttribute { get; } = "PartialAttribute";

    protected override IEnumerable<GenerationContent> OnGenerate(
            SourceProductionContext context,
            Compilation compilation,
            GenerationInput input)
    {
        INamedTypeSymbol typeSymbol = input.Symbol;
        TypeDeclarationSyntax syntax = input.Syntax;
        if (!syntax.IsPartial())
        {
            var diagnostic = Diagnostic.Create(
                new DiagnosticDescriptor("BLDPTRN: 001", "partial is required",
                $"{typeSymbol.Name}, must be marked as 'partial'", "CustomErrorCategory",
                DiagnosticSeverity.Error, isEnabledByDefault: true),
                Location.None); 
            context.ReportDiagnostic(diagnostic);
            yield break;
        }

        var cancellationToken = context.CancellationToken;

        ImmutableArray<MemberInfo> props = 
                typeSymbol.GetProperties()
                              .Where(p =>
                                                    !(p.IsReadOnly && p.Name == "EqualityContract") &&
                                                    !p.IsReadOnly)
                              .Select((p, i) =>
                              {
                                  var def = "default";
                                  var pSyntax = p.ToSyntaxNode<PropertyDeclarationSyntax>(compilation, cancellationToken);
                                  if (pSyntax?.Initializer != null)
                                  {
                                      def = pSyntax.Initializer.Value.ToFullString();
                                  }
                                  return new MemberInfo(p.Name, p.Type, p.IsRequired, i, def);
                              })
                              .ToImmutableArray();


        var tSyntax = typeSymbol.ToSyntaxNode<TypeDeclarationSyntax>(compilation, cancellationToken);
        var additionType = typeSymbol.IsRecord && typeSymbol.TypeKind == TypeKind.Struct ? " struct" : string.Empty;

        yield return Decorate(GenerateNullableBuilder());
        yield return Decorate(GeneratePartialClass());

        #region Decorate

        GenerationContent Decorate(GenerationContent generator)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine($"// Generated on {DateTimeOffset.UtcNow:yyyy-MM-dd}");
            builder.AppendLine($"#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.");
            builder.AppendLine($"#pragma warning disable CS0108 // hides inherited member.");
            builder.AppendLine();
            var usingLines = syntax.GetUsing();
            foreach (var line in usingLines)
            {
                builder.AppendLine(line.Trim());
            }

            var ns = typeSymbol.ContainingNamespace.ToDisplayString();
            if (ns != null)
                builder.AppendLine($"namespace {ns};");

            usingLines = syntax.GetUsingWithinNamespace();
            foreach (var line in usingLines)
            {
                builder.AppendLine(line.Trim());
            }

            builder.AppendLine(generator.Content);

            return new GenerationContent(generator.FileName, builder.ToString());
        }

        #endregion // Decorate

        #region GeneratePartialClass

        GenerationContent GeneratePartialClass()
        {
            StringBuilder builder = new StringBuilder();

            string type = syntax.Keyword.Text;
            builder.AppendLine($"partial {type}{additionType} {typeSymbol.Name}");
            builder.AppendLine("{");

            if (type == "record")
            {
                builder.AppendLine($"\tpublic static {typeSymbol.Name} operator +({typeSymbol.Name} a, {typeSymbol.Name}Nullable b)");
                builder.AppendLine("\t{");
                builder.AppendLine("\t\tvar result = a with {");
                foreach (var member in props)
                {
                    string name = member.Name;
                    builder.AppendLine($"\t\t\t{name} = b.{name} ?? a.{name},");
                }
                builder.Remove( builder.Length - 1, 1);
                builder.AppendLine("\t\t};");
                builder.AppendLine("\t\treturn result;");
                builder.AppendLine("\t}");
            }
            
            builder.AppendLine("}");

            return new GenerationContent($"{typeSymbol.Name}.partial.generated.cs", builder.ToString());
        }

        #endregion // GeneratePartialClass

        #region GenerateNullableBuilder

        GenerationContent GenerateNullableBuilder()
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine($"public {syntax.Keyword.Text}{additionType} {typeSymbol.Name}Nullable");
            builder.AppendLine("{");

            foreach (var member in props)
            {
                bool isNullable = member.Type.NullableAnnotation == NullableAnnotation.Annotated;
                string nullableChar = isNullable ? string.Empty : "?";
                builder.AppendLine($"\tpublic {member.Type}{nullableChar} {member.Name}{{ get; init; }}");
            }

            builder.AppendLine("}");


            return new GenerationContent($"{typeSymbol.Name}.nullable.generated.cs", builder.ToString());
        }

        #endregion // GenerateNullableBuilder
    }
}