using JetBrains.ReSharper.Psi;
using JetBrains.ReSharper.Psi.CSharp.Tree;
using JetBrains.ReSharper.Psi.Tree;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class TreeNodeExtensions
    {
        #region Methods

        public static string GetAccessorClrType(this ITreeNode treeNode)
        {
            while (!(treeNode is IElementAccessExpression) && treeNode != null)
            {
                treeNode = treeNode.Parent;
            }

            var referenceExpression = treeNode?.FirstChild as IReferenceExpression;
            if (referenceExpression == null)
            {
                return null;
            }

            var typeMember = referenceExpression.Reference.Resolve().DeclaredElement as ITypeMember;

            var containingType = typeMember?.GetContainingType();
            if (containingType == null)
            {
                return null;
            }

            return $"{containingType.GetClrName()}.{typeMember.ShortName}";
        }

        #endregion
    }
}
