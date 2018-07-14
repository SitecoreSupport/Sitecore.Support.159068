namespace Sitecore.Support.Data.Fields
{
  using System;
  using System.Reflection;
  using System.Collections;
  using System.Xml.Linq;
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Layouts;
  using Sitecore.Links;

  public partial class LayoutField
  {
    private class LinkRemover
    {
      #region Properties

      private readonly LayoutField layout;

      private static readonly MethodInfo GetParametersFieldsMethodInfo =
        typeof(Sitecore.Data.Fields.LayoutField).GetMethod("GetParametersFields",
          BindingFlags.Instance | BindingFlags.NonPublic);

      #endregion

      #region C'tors

      public LinkRemover(LayoutField layout)
      {
        this.layout = layout;
      }

      #endregion

      #region Public Methods

      public void RemoveLink([NotNull] ItemLink itemLink)
      {
        Assert.ArgumentNotNull(itemLink, "itemLink");

        string value = this.layout.Value;

        if (string.IsNullOrEmpty(value)) { return; }

        LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);

        this.DoRemoveLink(itemLink, layoutDefinition);
      }

      #endregion

      #region Private Methods

      private void DoRemoveLink(ItemLink itemLink, LayoutDefinition layoutDefinition)
      {
        this.RemoveLinkFromDevices(itemLink, layoutDefinition.Devices);

        this.SaveLayoutField(layoutDefinition);
      }

      private void RemoveLinkFromDevices(ItemLink itemLink, ArrayList devices)
      {
        if (devices == null) { return; }

        for (int n = devices.Count - 1; n >= 0; n--)
        {
          var device = devices[n] as DeviceDefinition;

          if (device == null) { continue; }

          if (CheckDeviceToRemove(device, itemLink))
          {
            devices.Remove(device);
            continue;
          }

          this.RemoveLinkFromDevice(itemLink, device);
        }
      }

      private void RemoveLinkFromDevice(ItemLink itemLink, DeviceDefinition device)
      {
        if (CheckLayoutToRemove(device.Layout, itemLink))
        {
          device.Layout = null;
          return;
        }

        this.RemoveLinkFromPlaceHolders(itemLink, device.Placeholders);

        this.RemoveLinkFromRenderings(itemLink, device.Renderings);
      }

      private void RemoveLinkFromPlaceHolders(ItemLink itemLink, ArrayList placeholders)
      {
        if (placeholders == null) { return; }

        for (int j = placeholders.Count - 1; j >= 0; j--)
        {
          this.RemoveLinkFromPlaceHolder(itemLink, placeholders, placeholders[j] as PlaceholderDefinition);
        }
      }

      private void RemoveLinkFromPlaceHolder(ItemLink itemLink, ArrayList placeholders, PlaceholderDefinition placeholder)
      {
        if (placeholder == null) { return; }

        if (CheckPlaceHolderToRemove(placeholder, itemLink))
        {
          placeholders.Remove(placeholder);
        }
      }

      private void RemoveLinkFromRenderings(ItemLink itemLink, ArrayList renderings)
      {
        if (renderings == null) { return; }

        for (int r = renderings.Count - 1; r >= 0; r--)
        {
          var rendering = renderings[r] as RenderingDefinition;

          if (rendering == null) { continue; }

          if (CheckRenderingToRemove(rendering, itemLink))
          {
            renderings.Remove(rendering);
            continue;
          }

          this.RemoveLinkFromRendering(itemLink, renderings[r] as RenderingDefinition);
        }
      }

      private void RemoveLinkFromRendering(ItemLink itemLink, RenderingDefinition rendering)
      {
        string targetItemId = itemLink.TargetItemID.ToString();

        if (rendering.Datasource == itemLink.TargetPath)
        {
          rendering.Datasource = string.Empty;
        }

        if (rendering.Datasource == targetItemId)
        {
          rendering.Datasource = string.Empty;
        }

        if (rendering.MultiVariateTest == targetItemId)
        {
          rendering.MultiVariateTest = null;
        }

        this.RemoveLinkFromRenderingParameters(itemLink, rendering);

        this.RemoveLinkFromRenderingRules(itemLink, rendering);
      }

      private void RemoveLinkFromRenderingRules(ItemLink itemLink, RenderingDefinition rendering)
      {
        if (rendering.Rules == null) { return; }

        var rulesField = new RulesField(this.layout.InnerField, rendering.Rules.ToString());
        rulesField.RemoveLink(itemLink);
        rendering.Rules = XElement.Parse(rulesField.Value);
      }

      private void RemoveLinkFromRenderingParameters(ItemLink itemLink, RenderingDefinition rendering)
      {
        if (string.IsNullOrEmpty(rendering.Parameters)) { return; }

        var renderingItemId = rendering.ItemID;

        Assert.IsNotNull(renderingItemId, nameof(renderingItemId));

        Item layoutItem = this.layout.InnerField.Database.GetItem(renderingItemId);

        if (layoutItem == null) { return; }

        var renderingParametersFieldCollection = (RenderingParametersFieldCollection)GetParametersFieldsMethodInfo.Invoke(this.layout, new object[] {layoutItem, rendering.Parameters});

        foreach (var field in renderingParametersFieldCollection.Values)
        {
          RemoveLinkFromCustomField(itemLink, field);
        }

        //rendering.Parameters = renderingParametersFieldCollection.GetParameters().ToString();
      }

      private void SaveLayoutField(LayoutDefinition layoutDefinition)
      {
        this.layout.Value = layoutDefinition.ToXml();
      }
      #endregion

      #region Static Methods

      private static void RemoveLinkFromCustomField(ItemLink itemLink, CustomField field)
      {
        if (!IsCustomFieldHasLink(field, itemLink)) { return; }

        bool handleEditingContext = !field.InnerField.Item.Editing.IsEditing;

        if (handleEditingContext) { field.InnerField.Item.Editing.BeginEdit(); }

        try
        {
          field.RemoveLink(itemLink);
        }
        finally
        {
          if (handleEditingContext) { field.InnerField.Item.Editing.EndEdit(); }
        }
      }

      private static bool IsCustomFieldHasLink(CustomField field, ItemLink itemLink)
      {
        Assert.IsNotNull(field, nameof(field));
        Assert.IsNotNull(itemLink, nameof(itemLink));

        if (string.IsNullOrEmpty(field.Value)) return false;

        return StringUtil.Contains(field.Value, itemLink.TargetPath, StringComparison.OrdinalIgnoreCase) ||
               StringUtil.Contains(field.Value, itemLink.TargetItemID.ToString(), StringComparison.OrdinalIgnoreCase);
      }

      #endregion

      #region Checkers

      private static bool CheckDeviceToRemove(DeviceDefinition device, ItemLink itemLink)
      {
        return device.ID == itemLink.TargetItemID.ToString();
      }

      private static bool CheckLayoutToRemove(string layoutId, ItemLink itemLink)
      {
        return layoutId == itemLink.TargetItemID.ToString();
      }

      private static bool CheckPlaceHolderToRemove(PlaceholderDefinition placeholder, ItemLink itemLink)
      {
        string targetPath = itemLink.TargetPath;
        string targetItemId = itemLink.TargetItemID.ToString();

        return string.Equals(placeholder.MetaDataItemId, targetPath, StringComparison.InvariantCultureIgnoreCase) ||
               string.Equals(placeholder.MetaDataItemId, targetItemId, StringComparison.InvariantCultureIgnoreCase);
      }

      private static bool CheckRenderingToRemove(RenderingDefinition rendering, ItemLink itemLink)
      {
        return rendering.ItemID == itemLink.TargetItemID.ToString();
      }
      #endregion
    }
  }
}