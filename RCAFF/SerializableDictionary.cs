/****************************************************************************
**
**  Developer: Caleb Amoa Buahin, Utah State University
**  Email: caleb.buahin@aggiemailgmail.com
** 
**  This file is part of the RCAFF.exe, a flood inundation forecasting tool was created as part of a project for the National
**  Flood Interoperability Experiment (NFIE) Summer Institute held at the National Water Center at University of Alabama Tuscaloosa from June 1st through July 17.
**  Special thanks to the following project members who made significant contributed to the approaches used in this code and its testing.
**  Nikhil Sangwan, Purdue University, Indiana
**  Cassandra Fagan, University of Texas, Austin
**  Samuel Rivera, University of Illinois at Urbana-Champaign
**  Curtis Rae, Brigham Young University, Utah
**  Marc Girons-Lopez Uppsala University, Sweden
**  Special thanks to our advisors, Dr.Jeffery Horsburgh, Dr. Jim Nelson, and Dr. Maidment who were instrumetal to the success of this project
**  RCAFF.exe and its associated files are free software; you can redistribute it and/or modify
**  it under the terms of the Lesser GNU General Public License as published by
**  the Free Software Foundation; either version 3 of the License, or
**  (at your option) any later version.
**
**  RCAFF.exe and its associated files is distributed in the hope that it will be useful,
**  but WITHOUT ANY WARRANTY; without even the implied warranty of
**  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
**  Lesser GNU General Public License for more details.
**
**  You should have received a copy of the Lesser GNU General Public License
**  along with this program.  If not, see <http://www.gnu.org/licenses/>
**
****************************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

[XmlRoot("dictionary")]
public class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, IXmlSerializable
{
    private string keyAlias;
    private string valueAlias;
    private string itemAlias;

    public string ItemAlias
    {
        get { return itemAlias; }
        set { itemAlias = value; }
    }

    public string ValueAlias
    {
        get { return valueAlias; }
        set { valueAlias = value; }
    }


    public string KeyAlias
    {
        get { return keyAlias; }
        set { keyAlias = value; }
    }

    public SerializableDictionary()
        : base()
    {
        keyAlias = "key";
        valueAlias = "value";
        itemAlias = "item";
    }

    public SerializableDictionary(string itemalias = "item", string keyalias = "key", string valuealias = "value")
        : base()
    {
        keyAlias = keyalias;
        valueAlias = valuealias;
        itemAlias = itemalias;
    }
    public System.Xml.Schema.XmlSchema GetSchema()
    {
        return null;
    }

    public void ReadXml(System.Xml.XmlReader reader)
    {
        XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
        XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

        bool wasEmpty = reader.IsEmptyElement;


        if (wasEmpty)
            return;

        string line = "";

        if ((line = reader.GetAttribute("ItemAlias")) != null)
            itemAlias = line;
        if ((line = reader.GetAttribute("KeyAlias")) != null)
            keyAlias = line;
        if ((line = reader.GetAttribute("ValueAlias")) != null)
            valueAlias = line;

        reader.Read();


        while (reader.NodeType != System.Xml.XmlNodeType.EndElement)
        {
            reader.ReadStartElement(itemAlias);

            reader.ReadStartElement(keyAlias);
            TKey key = (TKey)keySerializer.Deserialize(reader);
            reader.ReadEndElement();

            reader.ReadStartElement(valueAlias);
            TValue value = (TValue)valueSerializer.Deserialize(reader);
            reader.ReadEndElement();

            this.Add(key, value);

            reader.ReadEndElement();
            reader.MoveToContent();
        }

        reader.ReadEndElement();
    }

    public void WriteXml(System.Xml.XmlWriter writer)
    {
        XmlSerializer keySerializer = new XmlSerializer(typeof(TKey));
        XmlSerializer valueSerializer = new XmlSerializer(typeof(TValue));

        writer.WriteAttributeString("ItemAlias", itemAlias);
        writer.WriteAttributeString("KeyAlias", keyAlias);
        writer.WriteAttributeString("ValueAlias", valueAlias);

        foreach (TKey key in this.Keys)
        {
            writer.WriteStartElement(itemAlias);

            writer.WriteStartElement(keyAlias);
            keySerializer.Serialize(writer, key);
            writer.WriteEndElement();

            writer.WriteStartElement(valueAlias);
            TValue value = this[key];
            valueSerializer.Serialize(writer, value);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }
    }
}
