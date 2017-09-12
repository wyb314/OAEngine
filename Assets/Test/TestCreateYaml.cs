using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using YamlDotNet.RepresentationModel;
using YamlDotNet.Samples.Helpers;

public class TestCreateYaml : MonoBehaviour {

    private class CustomTestOutputHelper : ITestOutputHelper
    {
        private StringBuilder output = new StringBuilder();
        public void WriteLine()
        {
            output.AppendLine();
        }
        public void WriteLine(string value)
        {
            output.AppendLine(value);
        }
        public void WriteLine(string format, params object[] args)
        {
            output.AppendFormat(format, args);
            output.AppendLine();
        }

        public override string ToString() { return output.ToString(); }
        public void Clear() { output = new StringBuilder(); }
    }
    // Use this for initialization
    void Start ()
    {
        var address = new YamlMappingNode
        (
            new YamlScalarNode("street"),
            new YamlScalarNode("123 Tornado Alley\nSuite 16") { Style = YamlDotNet.Core.ScalarStyle.Literal },
            new YamlScalarNode("city"),
            new YamlScalarNode("East Westville"),
            new YamlScalarNode("state"),
            new YamlScalarNode("KS")
        ){ Anchor = "main-address" };

        var yaml = new YamlStream
        (
            new YamlDocument(
            new YamlMappingNode(
            new YamlScalarNode("repeipt"),
            new YamlScalarNode("Oz-Ware Purchase Invoice"),
            new YamlScalarNode("date"),
            new YamlScalarNode("2007-08-06"),
            new YamlScalarNode("customer"),
            new YamlMappingNode(
                new YamlScalarNode("given"),
                new YamlScalarNode("Dorothy"),
                new YamlScalarNode("family"),
                new YamlScalarNode("Gale")
            ),
            new YamlScalarNode("items"),
            new YamlSequenceNode(
                new YamlMappingNode(
                    new YamlScalarNode("part_no"),
                    new YamlScalarNode("A4786"),
                    new YamlScalarNode("descrip"),
                    new YamlScalarNode("Water Bucket (Filled)"),
                    new YamlScalarNode("price"),
                    new YamlScalarNode("1.47"),
                    new YamlScalarNode("quantity"),
                    new YamlScalarNode("4")
                ),
                new YamlMappingNode(
                    new YamlScalarNode("part_no"),
                    new YamlScalarNode("E1628"),
                    new YamlScalarNode("descrip"),
                    new YamlScalarNode("High Heeled \"Ruby\" Slippers"),
                    new YamlScalarNode("price"),
                    new YamlScalarNode("100.27"),
                    new YamlScalarNode("quantity"),
                    new YamlScalarNode("1")
                )
            ),
            new YamlScalarNode("bill-to"), address,
            new YamlScalarNode("ship-to"), address,
            new YamlScalarNode("specialDelivery"),
            new YamlScalarNode("Follow the Yellow Brick\n" +
                               "Road to the Emerald City.\n" +
                               "Pay no attention to the\n" +
                               "man behind the curtain.")
            {
                Style = YamlDotNet.Core.ScalarStyle.Literal
            }
        )
    )
            );

        

        StringBuilder sb = new StringBuilder();
        StringWriter sw = new StringWriter(sb);
        
        yaml.Save(sw);

        Debug.LogError(sb.ToString());
        
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
