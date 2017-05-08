using System.Collections.Generic;
using Sprache;

namespace FilterParser
{
    public class FilterGrammar
    {
        public enum LogicalOperator
        {
            And,
            Or
        }

        public enum Operator
        {
            GreaterThan,
            LessThan,
            Equals,
            GreaterThanOrEquals,
            LessThanOrEquals
        }

        public abstract class Element
        {
            
        }

        public class NodeElement
        {
            public NodeElement(string name)
            {
                Name = name;
            }

            public string Name { get; }
        }

        public class BinaryElement : Element
        {
            public BinaryElement(NodeElement left, Operator @operator, object value)
            {
                Left = left;
                Operator = @operator;
                Value = value;
            }

            public NodeElement Left { get; }
            public Operator Operator { get; }
            public object Value { get; }
        }

        public class LogicalGroup : Element
        {
            public LogicalGroup(LogicalOperator @operator, IReadOnlyCollection<BinaryElement> elements)
            {
                LogicalOperator = @operator;
                Elements = elements;
            }

            public LogicalOperator LogicalOperator { get; }
            public IReadOnlyCollection<BinaryElement> Elements { get; }
        }

        public static Parser<string> NodeItem =
            from firstLetter in Parse.Letter.AtLeastOnce().Text()
            from theRest in Parse.LetterOrDigit.Many().Text()
            select firstLetter + theRest;

        public static Parser<string> EscapedNodeItem =
            from lBracket in Parse.Char('[')
            from firstLetter in Parse.LetterOrDigit.XAtLeastOnce().Text()
            from theRest in Parse.CharExcept("{}\r\n[]").XMany().Text()
            from rBracket in Parse.Char(']')
            select firstLetter + theRest;

        public static Parser<string> Node =
           (from firstNode in NodeItem
            from theRest in (
                from dot in Parse.Char('.')
                from item in NodeItem.Or(EscapedNodeItem)
                select dot + item
            ).XMany()
            select firstNode + string.Join(string.Empty, theRest)).Token();

        public static Parser<BinaryElement> BinaryParser = 
            from node in Node
            from op in GreaterThan.Or(LessThan).Or(LessThanOrEquals).Or(EqualsOp).Or(GreaterThanOrEquals)
            from val in String.Select(s => (object)s).Or(Bool.Select(b => (object)b)).Or(Decimal.Select(d => (object)d))
            select new BinaryElement(new NodeElement(node), op, val);

//        public static Parser<Element> ElementParser = 


        public static Parser<LogicalOperator> And = Parse.String("AND").Token().Select(_ => LogicalOperator.And);
        public static Parser<LogicalOperator> Or = Parse.String("OR").Token().Select(_ => LogicalOperator.Or);

        public static Parser<Operator> GreaterThan = Parse.Char('>').Token().Select(_ => Operator.GreaterThan);
        public static Parser<Operator> LessThan = Parse.Char('<').Token().Select(_ => Operator.LessThan);
        public static Parser<Operator> EqualsOp = Parse.Char('=').Token().Select(_ => Operator.Equals);
        public static Parser<Operator> GreaterThanOrEquals = Parse.String(">=").Token().Select(_ => Operator.GreaterThanOrEquals);
        public static Parser<Operator> LessThanOrEquals = Parse.String("<=").Token().Select(_ => Operator.LessThanOrEquals);
        
        public static Parser<string> String = 
           (from qStart in Parse.Char('"')
            from str in Parse.CharExcept("\"\r\n").XMany().Text()
            from qEnd in Parse.Char('"')
            select str).Token();

        public static Parser<bool> Bool = Parse.IgnoreCase("true").Or(Parse.IgnoreCase("false")).Text().Select(x => x.ToLower() == "true");

        public static Parser<decimal> Decimal = Parse.Decimal.Select(decimal.Parse);


    }
}