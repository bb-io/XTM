using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Apps.XTM.Models.Response.Glossaries
{
    [XmlRoot(ElementName = "martif")]
    public class XTMBasicTbxDto
    {
        [XmlElement(ElementName = "martifHeader")]
        public MartifHeader MartifHeader { get; set; }

        [XmlElement(ElementName = "text")]
        public List<Text> Text { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlAttribute(AttributeName = "lang")]
        public string Lang { get; set; }
    }

    // using System.Xml.Serialization;
    // XmlSerializer serializer = new XmlSerializer(typeof(Martif));
    // using (StringReader reader = new StringReader(xml))
    // {
    //    var test = (Martif)serializer.Deserialize(reader);
    // }

    [XmlRoot(ElementName = "sourceDesc")]
    public class SourceDesc
    {

        [XmlElement(ElementName = "p")]
        public string P { get; set; }
    }

    [XmlRoot(ElementName = "fileDesc")]
    public class FileDesc
    {

        [XmlElement(ElementName = "sourceDesc")]
        public SourceDesc SourceDesc { get; set; }
    }

    [XmlRoot(ElementName = "martifHeader")]
    public class MartifHeader
    {

        [XmlElement(ElementName = "fileDesc")]
        public FileDesc FileDesc { get; set; }
    }

    [XmlRoot(ElementName = "descrip")]
    public class Descrip
    {

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "termNote")]
    public class TermNote
    {

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "termCompList")]
    public class TermCompList
    {

        [XmlElement(ElementName = "termComp")]
        public string TermComp { get; set; }

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "termGrp")]
    public class TermGrp
    {

        [XmlElement(ElementName = "term")]
        public string Term { get; set; }

        [XmlElement(ElementName = "termNote")]
        public TermNote TermNote { get; set; }

        [XmlElement(ElementName = "termCompList")]
        public TermCompList TermCompList { get; set; }
    }

    [XmlRoot(ElementName = "transac")]
    public class Transac
    {

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "transacNote")]
    public class TransacNote
    {

        [XmlAttribute(AttributeName = "type")]
        public string Type { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "transacGrp")]
    public class TransacGrp
    {

        [XmlElement(ElementName = "transac")]
        public Transac Transac { get; set; }

        [XmlElement(ElementName = "transacNote")]
        public TransacNote TransacNote { get; set; }

        [XmlElement(ElementName = "date")]
        public DateTime Date { get; set; }
    }

    [XmlRoot(ElementName = "ntig")]
    public class Ntig
    {

        [XmlElement(ElementName = "termGrp")]
        public TermGrp TermGrp { get; set; }

        [XmlElement(ElementName = "transacGrp")]
        public List<TransacGrp> TransacGrp { get; set; }
    }

    [XmlRoot(ElementName = "langSet")]
    public class LangSet
    {

        [XmlElement(ElementName = "ntig")]
        public Ntig Ntig { get; set; }

        [XmlAttribute(AttributeName = "lang")]
        public string Lang { get; set; }

        [XmlText]
        public string Text { get; set; }
    }

    [XmlRoot(ElementName = "termEntry")]
    public class TermEntry
    {

        [XmlElement(ElementName = "descrip")]
        public List<Descrip> Descrip { get; set; }

        [XmlElement(ElementName = "langSet")]
        public List<LangSet> LangSet { get; set; }
    }

    [XmlRoot(ElementName = "body")]
    public class Body
    {

        [XmlElement(ElementName = "termEntry")]
        public List<TermEntry> TermEntry { get; set; }
    }

    [XmlRoot(ElementName = "text")]
    public class Text
    {

        [XmlElement(ElementName = "body")]
        public Body Body { get; set; }
    }
}
