namespace Sitecore.Support.Data.Fields
{
  using Sitecore.Data.Fields;
  using Sitecore.Data.Items;
  using Sitecore.Links;
  public partial class LayoutField: Sitecore.Data.Fields.LayoutField
  {
    public LayoutField(Item item) : base(item)
    {
    }

    public LayoutField(Item item, string runtimeValue) : base(item, runtimeValue)
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
      LinkRemover linkRemover = new LinkRemover(this);
      linkRemover.RemoveLink(itemLink);
    }
  }
}