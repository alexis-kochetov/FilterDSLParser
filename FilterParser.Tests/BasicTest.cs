using System;
using Sprache;
using Xunit;

namespace FilterParser.Tests
{
    public class BasicTest
    {
        [Fact]
        public void NodeItemTest()
        {
            var node = FilterGrammar.NodeItem.Parse(@"ASDDD098");

            Assert.Equal("ASDDD098", node);
        }

        [Fact]
        public void EscapedNodeItemTest()
        {
            var node = FilterGrammar.EscapedNodeItem.Parse(@"[ASDDD0 - 98]");

            Assert.Equal("ASDDD0 - 98", node);
        }


        [Fact]
        public void NodeTest()
        {
            var node = FilterGrammar.Node.Parse(@" Aasd.ASDDD098 ");

            Assert.Equal("Aasd.ASDDD098", node);
        }

        [Fact]
        public void SimpleParsingTest()
        {
            var binaryElement = FilterGrammar.BinaryParser.Parse(@"
Aasd.ASDDD098.[asedsdf sdfsd 0909] = 123");

            Assert.Equal(FilterGrammar.Operator.Equals, binaryElement.Operator);
            Assert.Equal("Aasd.ASDDD098.asedsdf sdfsd 0909", binaryElement.Left.Name);
            Assert.Equal(123m, binaryElement.Value);
        }
    }
}
