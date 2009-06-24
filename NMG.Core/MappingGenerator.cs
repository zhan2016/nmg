using System.IO;
using System.Xml;
using NMG.Core.Domain;
using NMG.Core.Util;

namespace NMG.Core
{
    public abstract class MappingGenerator : Generator
    {
        private readonly Preferences preferences;

        protected MappingGenerator(string path, string tableName, string nameSpace, string assemblyName, string sequenceName,
                                   ColumnDetails columnDetails, Preferences preferences)
            : base(path, tableName, nameSpace, assemblyName, sequenceName, columnDetails)
        {
            this.preferences = preferences;
        }

        protected abstract void AddIdGenerator(XmlDocument xmldoc, XmlElement idElement);

        public override void Generate()
        {
            string fileName = filePath + tableName.GetFormattedText() + ".hbm.xml";
            using (var stringWriter = new StringWriter())
            {
                var xmldoc = CreateMappingDocument();
                xmldoc.Save(stringWriter);
                string generatedXML = RemoveEmptyNamespaces(stringWriter.ToString());

                using (var writer = new StreamWriter(fileName))
                {
                    writer.Write(generatedXML);
                    writer.Flush();
                }
            }
        }

        private static string RemoveEmptyNamespaces(string mappingContent)
        {
            return mappingContent.Replace("xmlns=\"\"", "");
        }

        public XmlDocument CreateMappingDocument()
        {
            var xmldoc = new XmlDocument();
            var xmlDeclaration = xmldoc.CreateXmlDeclaration("1.0", "utf-8", "");
            xmldoc.AppendChild(xmlDeclaration);
            var root = xmldoc.CreateElement("hibernate-mapping", "urn:nhibernate-mapping-2.2");
            root.SetAttribute("assembly", assemblyName);
            xmldoc.AppendChild(root);

            var classElement = xmldoc.CreateElement("class");
            classElement.SetAttribute("name", nameSpace + "." + tableName.GetFormattedText() + ", " + assemblyName);
            classElement.SetAttribute("table", tableName);
            classElement.SetAttribute("lazy", "true");
            root.AppendChild(classElement);
            var primaryKeyColumn = columnDetails.Find(detail => detail.IsPrimaryKey);
            if (primaryKeyColumn != null)
            {
                var idElement = xmldoc.CreateElement("id");
                idElement.SetAttribute("name", "id");
                var mapper = new DataTypeMapper();
                idElement.SetAttribute("type",
                                       mapper.MapFromDBType(primaryKeyColumn.DataType, primaryKeyColumn.DataLength, primaryKeyColumn.DataPrecision,
                                                            primaryKeyColumn.DataScale).Name);
                idElement.SetAttribute("column", primaryKeyColumn.ColumnName);
                idElement.SetAttribute("access", "field");
                classElement.AppendChild(idElement);
                AddIdGenerator(xmldoc, idElement);
            }

            AddAllProperties(xmldoc, classElement);
            return xmldoc;
        }

        private void AddAllProperties(XmlDocument xmldoc, XmlNode classElement)
        {
            foreach (var columnDetail in columnDetails)
            {
                if (columnDetail.IsPrimaryKey)
                    continue;
                var xmlNode = xmldoc.CreateElement("property");
                xmlNode.SetAttribute("name", columnDetail.ColumnName.GetPreferenceFormattedText(preferences));
                xmlNode.SetAttribute("column", columnDetail.ColumnName);
                xmlNode.SetAttribute("access", "field");
                classElement.AppendChild(xmlNode);
            }
        }
    }
}