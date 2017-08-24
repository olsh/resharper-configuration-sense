using System;
using System.Linq;
using JetBrains.DocumentModel;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Tree;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class RangeExtensions
    {
        public static TextLookupRanges EvaluateRanges(this ISpecificCodeCompletionContext context)
        {
            var basicContext = context.BasicContext;

            var startOffset = basicContext.CaretDocumentOffset;
            var endOffset = basicContext.SelectedRange.EndOffset;

            var caretTreeOffset = basicContext.CaretTreeOffset;

            if (basicContext.File.FindTokenAt(caretTreeOffset) is ITokenNode tokenNode && tokenNode.IsAnyStringLiteral())
            {
                startOffset = tokenNode.GetDocumentStartOffset();
                endOffset = tokenNode.GetDocumentEndOffset();
            }

            var replaceRange = new DocumentRange(startOffset, endOffset);

            return new TextLookupRanges(replaceRange, replaceRange);
        }

        public static bool IsInsideAccessorPath(this ISpecificCodeCompletionContext context, string path)
        {
            var nodeAt = context.BasicContext.File.FindNodeAt(context.BasicContext.CaretDocumentOffset);

            var accessorPath = nodeAt.GetAccessorPath();
            if (accessorPath == null)
            {
                return false;
            }

            return accessorPath.Equals(path);
        }

        public static bool IsInsideMethodPath(this ISpecificCodeCompletionContext context, string path)
        {
            var nodeAt = context.BasicContext.File.FindNodeAt(context.BasicContext.CaretDocumentOffset);

            var accessorPath = nodeAt.GetMethodPath();
            if (accessorPath == null)
            {
                return false;
            }

            return accessorPath.Equals(path);
        }

        public static bool IsInsideAccessorType(this ISpecificCodeCompletionContext context, string accessorType)
        {
            var nodeAt = context.BasicContext.File.FindNodeAt(context.BasicContext.CaretDocumentOffset);

            var accessorClrType = nodeAt.GetAccessorSuperTypes();
            if (accessorClrType == null)
            {
                return false;
            }

            return accessorClrType.Any(t => t.ToString().Equals(accessorType, StringComparison.OrdinalIgnoreCase));
        }
    }
}
