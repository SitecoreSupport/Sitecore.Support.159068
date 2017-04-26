namespace Sitecore.Support.Data.Fields
{
    using Sitecore.Data.Fields;
    using Sitecore.Data.Items;
    using Sitecore.Diagnostics;
    using Sitecore.Layouts;
    using Sitecore.Links;
    using Sitecore.Text;
    using Sitecore.Xml;
    using System;
    using System.Collections;
    using System.Xml;
    using System.Xml.Linq;

    public class LayoutField : Sitecore.Data.Fields.LayoutField
    {
        private readonly XmlDocument data;

        public LayoutField(Field innerField) : base(innerField)
        {
            Assert.ArgumentNotNull(innerField, "innerField");
            this.data = this.LoadData();
        }

        public LayoutField(Item item) : this(item.Fields[FieldIDs.FinalLayoutField])
        {
        }

        public LayoutField(Field innerField, string runtimeValue) : base(innerField, runtimeValue)
        {
            Assert.ArgumentNotNull(innerField, "innerField");
            Assert.ArgumentNotNullOrEmpty(runtimeValue, "runtimeValue");
            this.data = this.LoadData();
        }

        private RenderingParametersFieldCollection GetParametersFields(Item layoutItem, string renderingParameters)
        {
            RenderingParametersFieldCollection fields;
            UrlString str = new UrlString(renderingParameters);
            RenderingParametersFieldCollection.TryParse(layoutItem, str, out fields);
            return fields;
        }

        private XmlDocument LoadData()
        {
            string str = base.Value;
            if (!string.IsNullOrEmpty(str))
            {
                return XmlUtil.LoadXml(str);
            }
            return XmlUtil.LoadXml("<r/>");
        }

        public override void RemoveLink(ItemLink itemLink)
        {
            Assert.ArgumentNotNull(itemLink, "itemLink");
            string str = base.Value;
            if (!string.IsNullOrEmpty(str))
            {
                LayoutDefinition definition = LayoutDefinition.Parse(str);
                ArrayList list = definition.Devices;
                if (list != null)
                {
                    string b = itemLink.TargetItemID.ToString();
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        DeviceDefinition definition2 = list[i] as DeviceDefinition;
                        if (definition2 != null)
                        {
                            if (definition2.ID == b)
                            {
                                list.Remove(definition2);
                            }
                            else if (definition2.Layout == b)
                            {
                                definition2.Layout = null;
                            }
                            else
                            {
                                if (definition2.Placeholders != null)
                                {
                                    string str3 = itemLink.TargetPath;
                                    bool flag = false;
                                    for (int j = definition2.Placeholders.Count - 1; j >= 0; j--)
                                    {
                                        PlaceholderDefinition definition3 = definition2.Placeholders[j] as PlaceholderDefinition;
                                        if ((definition3 != null) && (string.Equals(definition3.MetaDataItemId, str3, StringComparison.InvariantCultureIgnoreCase) || string.Equals(definition3.MetaDataItemId, b, StringComparison.InvariantCultureIgnoreCase)))
                                        {
                                            definition2.Placeholders.Remove(definition3);
                                            flag = true;
                                        }
                                    }
                                    if (flag)
                                    {
                                        continue;
                                    }
                                }
                                if (definition2.Renderings != null)
                                {
                                    for (int k = definition2.Renderings.Count - 1; k >= 0; k--)
                                    {
                                        RenderingDefinition definition4 = definition2.Renderings[k] as RenderingDefinition;
                                        if (definition4 != null)
                                        {
                                            if (definition4.Datasource == itemLink.TargetPath)
                                            {
                                                definition4.Datasource = string.Empty;
                                            }
                                            if (definition4.ItemID == b)
                                            {
                                                definition2.Renderings.Remove(definition4);
                                            }
                                            if (definition4.Datasource == b)
                                            {
                                                definition4.Datasource = string.Empty;
                                            }
                                            if (!string.IsNullOrEmpty(definition4.Parameters))
                                            {
                                                Item layoutItem = base.InnerField.Database.GetItem(definition4.ItemID);
                                                if (layoutItem != null)
                                                {
                                                    RenderingParametersFieldCollection parametersFields = this.GetParametersFields(layoutItem, definition4.Parameters);
                                                    foreach (CustomField field in parametersFields.Values)
                                                    {
                                                        if (!string.IsNullOrEmpty(field.Value))
                                                        {
                                                            field.RemoveLink(itemLink);
                                                        }
                                                    }
                                                }
                                            }
                                            if (definition4.Rules != null)
                                            {
                                                RulesField field2 = new RulesField(base.InnerField, definition4.Rules.ToString());
                                                field2.RemoveLink(itemLink);
                                                definition4.Rules = XElement.Parse(field2.Value);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    base.Value = definition.ToXml();
                }
            }
        }
    }
}
