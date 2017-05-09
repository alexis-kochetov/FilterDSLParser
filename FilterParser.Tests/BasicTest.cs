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
            Assert.Equal(123m, binaryElement.Value.DecimalValue);
        }

        [Fact]
        public void SimpleGroupTest()
        {
            var group = FilterGrammar.OrGroup.Parse(@"Aasd.ASDDD098 = ""asa asa"" OR AsaA >= 1234.4");
            var binaryElements = group.Elements.OfType<FilterGrammar.BinaryElement>().ToList();
            
            Assert.Equal(FilterGrammar.LogicalOperator.Or, group.LogicalOperator);
            Assert.Equal(2, binaryElements.Count);
            Assert.Equal("asa asa", binaryElements.First().Value.StringValue);
            Assert.Equal(1234.4m, binaryElements.ElementAt(1).Value.DecimalValue);
        }


        [Fact]
        public void LongGroupTest()
        {
            var group = FilterGrammar.OrGroup.Parse(@"Aasd.ASDDD098 = ""asa asa"" OR AsaA >= 1234.4 OR Xyz = true");
            var binaryElements = group.Elements.OfType<FilterGrammar.BinaryElement>().ToList();

            Assert.Equal(FilterGrammar.LogicalOperator.Or, group.LogicalOperator);
            Assert.Equal(3, binaryElements.Count);
            Assert.Equal(true, binaryElements.ElementAt(2).Value.BoolValue);
        }

        [Fact]
        public void ParenGroupTest()
        {
            var group = FilterGrammar.Group.Parse(@"Aasd.ASDDD098 = ""asa asa"" OR ( AsaA >= 1234.4 AND Xyz = true )");

            Assert.Equal(FilterGrammar.LogicalOperator.Or, group.LogicalOperator);
        }

        [Fact]
        public void RootParenGroupTest()
        {
            var group = FilterGrammar.Group.Parse(@"(Aasd.ASDDD098 = ""asa asa"" AND (AsaA >= 1234.4 OR Xyz = true ))");

            Assert.Equal(FilterGrammar.LogicalOperator.And, group.LogicalOperator);
        }

        [Fact]
        public void FilterTest()
        {
            var group = FilterGrammar.Filter.Parse(@"
    
(
    Aasd.ASDDD098 = ""asa asa"" 
    AND 
    (
        AsaA >= 1234.4 
        OR  Xyz = true 
    )  
)  
");

            Assert.Equal(FilterGrammar.LogicalOperator.And, group.LogicalOperator);
        }
    }
}
