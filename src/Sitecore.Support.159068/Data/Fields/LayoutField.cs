namespace Sitecore.Support.Data.Fields
{
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Diagnostics;
  using Sitecore.Layouts;
  using Sitecore.Links;
  using Sitecore.Pipelines;
  using Sitecore.Pipelines.ResolveRenderingDatasource;
  using Sitecore.Text;
  using System;
  using System.Collections;
  using System.Xml.Linq;

  public partial class LayoutField: Sitecore.XA.Foundation.SitecoreExtensions.CustomFields.LayoutField
  {
    public LayoutField(Item item) : base(item)
    {
    }
    
    public LayoutField(Field innerField) : base(innerField)
    {
    }

    public LayoutField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
    {
    }

    public override void RemoveLink(ItemLink itemLink)
    {
      LinkRemover linkRemover = new LinkRemover(this, base.InnerField.Item);
      linkRemover.RemoveLink(itemLink);
    }
    public override void Relink(ItemLink itemLink, Item newLink)
    {
      Assert.ArgumentNotNull(itemLink, "itemLink");
      Assert.ArgumentNotNull(newLink, "newLink");

      string value = this.Value;
      if (string.IsNullOrEmpty(value))
      {
        return;
      }

      LayoutDefinition layoutDefinition = LayoutDefinition.Parse(value);

      ArrayList devices = layoutDefinition.Devices;
      if (devices == null)
      {
        return;
      }

      string targetItemID = itemLink.TargetItemID.ToString();
      string newLinkID = newLink.ID.ToString();

      for (int n = devices.Count - 1; n >= 0; n--)
      {
        var device = devices[n] as DeviceDefinition;
        if (device == null)
        {
          continue;
        }

        if (device.ID == targetItemID)
        {
          device.ID = newLinkID;
          continue;
        }

        if (device.Layout == targetItemID)
        {
          device.Layout = newLinkID;
          continue;
        }

        if (device.Placeholders != null)
        {
          string targetPath = itemLink.TargetPath;
          bool isLinkFound = false;
          for (int j = device.Placeholders.Count - 1; j >= 0; j--)
          {
            var placeholderDefinition = device.Placeholders[j] as PlaceholderDefinition;
            if (placeholderDefinition == null)
            {
              continue;
            }

            if (
              string.Equals(
                placeholderDefinition.MetaDataItemId, targetPath, StringComparison.InvariantCultureIgnoreCase) ||
              string.Equals(
                placeholderDefinition.MetaDataItemId, targetItemID, StringComparison.InvariantCultureIgnoreCase))
            {
              placeholderDefinition.MetaDataItemId = newLink.ID.ToString();
              isLinkFound = true;
            }
          }

          if (isLinkFound)
          {
            continue;
          }
        }

        if (device.Renderings == null)
        {
          continue;
        }

        for (int r = device.Renderings.Count - 1; r >= 0; r--)
        {
          var rendering = device.Renderings[r] as RenderingDefinition;
          if (rendering == null)
          {
            continue;
          }

          if (rendering.ItemID == targetItemID)
          {
            rendering.ItemID = newLinkID;
          }

          string currentDatasource = rendering.Datasource;
          if (!string.IsNullOrEmpty(rendering.Datasource))
          {
            using (new ContextItemSwitcher(base.InnerField.Item))
            {
              ResolveRenderingDatasourceArgs resolveRenderingDatasourceArgs = new ResolveRenderingDatasourceArgs(rendering.Datasource);
              CorePipeline.Run("resolveRenderingDatasource", resolveRenderingDatasourceArgs, false);
              currentDatasource = resolveRenderingDatasourceArgs.Datasource;
            }
          }

          if (currentDatasource == targetItemID)
          {
            rendering.Datasource = newLinkID;
          }

          if (currentDatasource != null && currentDatasource.Equals(itemLink.TargetPath, StringComparison.OrdinalIgnoreCase))
          {
            rendering.Datasource = newLink.Paths.FullPath;
          }

          if (!string.IsNullOrEmpty(rendering.Parameters))
          {
            Item layoutItem = this.InnerField.Database.GetItem(rendering.ItemID);

            if (layoutItem == null)
            {
              continue;
            }

            var renderingParametersFieldCollection = this.GetParametersFields(layoutItem, rendering.Parameters);

            foreach (var field in renderingParametersFieldCollection.Values)
            {
              if (!string.IsNullOrEmpty(field.Value))
              {
                field.Relink(itemLink, newLink);
              }
            }

            //rendering.Parameters = renderingParametersFieldCollection.GetParameters().ToString();
          }

          if (rendering.Rules != null)
          {
            var rulesField = new RulesField(this.InnerField, rendering.Rules.ToString());
            rulesField.Relink(itemLink, newLink);
            rendering.Rules = XElement.Parse(rulesField.Value);
          }
        }
      }

      this.Value = layoutDefinition.ToXml();
    }

  }
}