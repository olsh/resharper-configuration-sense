using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;
using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;
using JetBrains.Util;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class TreeNodeExtensions
    {
        [CanBeNull]
        public static string GetAccessorPath(this ITreeNode treeNode)
        {
            if (TryFindReferenceExpression<IElementAccessExpression>(treeNode, out var referenceExpression))
            {
                return null;
            }

            return GetReferencePath(referenceExpression);
        }

        public static IEnumerable<IDeclaredType> GetAccessorSuperTypes(this ITreeNode treeNode)
        {
            if (TryFindReferenceExpression<IElementAccessExpression>(treeNode, out var referenceExpression))
            {
                return Enumerable.Empty<IDeclaredType>();
            }

            var typeOwner = referenceExpression.Reference.Resolve().DeclaredElement as ITypeOwner;
            var declaredType = typeOwner?.Type as IDeclaredType;
            if (declaredType == null)
            {
                return Enumerable.Empty<IDeclaredType>();
            }

            var declaredTypes = new HashSet<IDeclaredType> { declaredType };
            declaredTypes.AddRange(declaredType.GetSuperTypes());

            return declaredTypes;
        }

        [CanBeNull]
        public static string GetMethodPath(this ITreeNode treeNode)
        {
            if (TryFindReferenceExpression<IInvocationExpression>(treeNode, out var referenceExpression))
            {
                return null;
            }

            return GetReferencePath(referenceExpression);
        }

        private static string GetReferencePath(IReferenceExpression referenceExpression)
        {
            var typeMember = referenceExpression.Reference.Resolve().DeclaredElement as ITypeMember;

            var containingType = typeMember?.GetContainingType();
            if (containingType == null)
            {
                return null;
            }

            return $"{containingType.GetClrName()}.{typeMember.ShortName}";
        }

        private static bool TryFindReferenceExpression<T>(
            ITreeNode treeNode,
            out IReferenceExpression referenceExpression)
        {
            while (!(treeNode is T) && (treeNode != null))
            {
                treeNode = treeNode.Parent;
            }

            referenceExpression = treeNode?.FirstChild as IReferenceExpression;
            if (referenceExpression == null)
            {
                return true;
            }

            return false;
        }
    }
}
