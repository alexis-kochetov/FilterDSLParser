using System;
using System.Linq;
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
        public void ShortNodeTest()
        {
            var node = FilterGrammar.Node.Parse(@" ABC");

            Assert.Equal("ABC", node);
        }

        [Fact]
        public void LongNodeTest()
        {
            var node = FilterGrammar.Node.Parse(@"A.B.C.D.[E].F9");

            Assert.Equal("A.B.C.D.E.F9", node);
        }


        [Fact]
        public void NodeTest()
        {
            var node = FilterGrammar.Node.Parse(@" Aasd.ASDDD098 ");

            Assert.Equal("Aasd.ASDDD098", node);
        }

        [Fact]
        public void BinaryTest()
        {
            var binaryElement = FilterGrammar.BinaryParser.Parse(@"
Aasd.ASDDD098.[asedsdf sdfsd 0909] = 123");

            Assert.Equal(FilterGrammar.Operator.Equals, binaryElement.Operator);
            Assert.Equal("Aasd.ASDDD098.asedsdf sdfsd 0909", binaryElement.Left.Name);
            Assert.Equal(123m, binaryElement.Value);
        }

        [Fact]
        public void SimpleGroupTest()
        {
            var group = FilterGrammar.OrGroup.Parse(@"Aasd.ASDDD098 = ""asa asa"" OR AsaA >= 1234.4");

            Assert.Equal(FilterGrammar.LogicalOperator.Or, group.LogicalOperator);
            Assert.Equal(2, group.Elements.Count);
            Assert.Equal("asa asa", group.Elements.First().Value);
            Assert.Equal(1234.4m, group.Elements.ElementAt(1).Value);
        }


        [Fact]
        public void LongGroupTest()
        {
            var group = FilterGrammar.OrGroup.Parse(@"Aasd.ASDDD098 = ""asa asa"" OR AsaA >= 1234.4 OR Xyz = true");

            Assert.Equal(FilterGrammar.LogicalOperator.Or, group.LogicalOperator);
            Assert.Equal(3, group.Elements.Count);
            Assert.Equal(true, group.Elements.ElementAt(2).Value);
        }
    }
}
