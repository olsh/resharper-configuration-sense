using System;

using JetBrains.Metadata.Reader.API;
using JetBrains.ReSharper.Feature.Services.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Feature.Services.CSharp.CodeCompletion.Infrastructure;
using JetBrains.ReSharper.Psi.CSharp.Util.Literals;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class RangeExtensions
    {
        #region Methods

        public static TextLookupRanges EvaluateRanges(this CSharpCodeCompletionContext context)
        {
            var basicContext = context.BasicContext;
            var selectedRange = basicContext.SelectedRange.TextRange;
            var documentRange = basicContext.CaretDocumentRange.TextRange;
            var caretTreeOffset = basicContext.CaretTreeOffset;
            var tokenNode = basicContext.File.FindTokenAt(caretTreeOffset) as ITokenNode;

            if (tokenNode != null && tokenNode.IsAnyStringLiteral())
            {
                documentRange = tokenNode.GetDocumentRange().TextRange;
            }

            var replaceRange = new TextRange(
                documentRange.StartOffset, 
                Math.Max(documentRange.EndOffset, selectedRange.EndOffset));

            return new TextLookupRanges(replaceRange, replaceRange);
        }

        public static bool IsInsideElement(this CSharpCodeCompletionContext context, IClrTypeName typeName)
        {
            var nodeAt = context.BasicContext.File.FindNodeAt(context.BasicContext.CaretDocumentRange);

            var accessorClrType = nodeAt.GetAccessorClrType();
            if (accessorClrType == null)
            {
                return false;
            }

            return accessorClrType.Equals(typeName.FullName);
        }

        #endregion
    }
}
