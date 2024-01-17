using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Starfield_Interactive_Smart_Slate.Common
{
    internal static class Framework
    {
        public static void ClearInnerListViews(DependencyObject parent, ListView exceptThis)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);

                if (child is ListView listView && child != exceptThis)
                {
                    listView.SelectedItem = null;
                    continue;
                }

                // Recursively search for inner ListViews in child elements
                if (VisualTreeHelper.GetChildrenCount(child) > 0)
                {
                    ClearInnerListViews(child, exceptThis);
                }
            }
        }

        public static ListView FindParentListView(ListViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);

            while (parent != null && !(parent is ListView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as ListView;
        }

        public static T FindAncestor<T>(DependencyObject current) where T : DependencyObject
        {
            do
            {
                if (current is T ancestor)
                {
                    return ancestor;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            while (current != null);

            return null;
        }
    }
}