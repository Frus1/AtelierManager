using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace AtelierManager.Controls
{
    public class TabControlEx : TabControl
    {
        private readonly Dictionary<object, ContentControl> _contentMap = new();

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            UpdateContent();
        }

        protected override void OnSelectionChanged(SelectionChangedEventArgs e)
        {
            base.OnSelectionChanged(e);
            UpdateContent();
        }

        private void UpdateContent()
        {
            if (Items.Count == 0) return;

            var panel = GetTemplateChild("PART_ItemsHolder") as Panel;
            if (panel == null) return;

            foreach (var item in Items)
            {
                var tabItem = item as TabItem ?? ItemContainerGenerator.ContainerFromItem(item) as TabItem;
                if (tabItem == null) continue;

                if (!_contentMap.TryGetValue(item, out var contentControl))
                {
                    contentControl = new ContentControl
                    {
                        Content = tabItem.Content,
                        ContentTemplate = tabItem.ContentTemplate,
                        ContentTemplateSelector = tabItem.ContentTemplateSelector,
                        ContentStringFormat = tabItem.ContentStringFormat,
                        Visibility = Visibility.Collapsed
                    };

                    _contentMap[item] = contentControl;
                    panel.Children.Add(contentControl);

                    tabItem.Content = null;
                }

                contentControl.Visibility = Equals(item, SelectedItem) ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }
}