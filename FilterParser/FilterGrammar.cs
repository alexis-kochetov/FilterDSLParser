using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
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

        public enum ValType
        {
            String,
            Bool,
            Decimal
        }

        public class Val
        {
            private readonly object _value;

            public Val(string stringValue)
            {
                _value = stringValue;
                Type = ValType.String;
            }

            public Val(bool boolValue)
            {
                _value = boolValue;
                Type = ValType.Bool;
            }
            public Val(decimal decimalValue)
            {
                _value = decimalValue;
                Type = ValType.Decimal;
            }

            public string StringValue => (string) _value;
            public bool BoolValue => (bool)_value;
            public decimal DecimalValue => (decimal)_value;

            public ValType Type { get; }

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

        public class FunctionElement : NodeElement
        {
            public FunctionElement(string name, IReadOnlyList<Val> arguments) : base(name)
            {
                Arguments = arguments;
            }

            public IReadOnlyList<Val> Arguments { get; }
        }

        public class BinaryElement : Element
        {
            public BinaryElement(NodeElement left, Operator @operator, Val value)
            {
                Left = left;
                Operator = @operator;
                Value = value;
            }

            public NodeElement Left { get; }
            public Operator Operator { get; }
            public Val Value { get; }
        }

        public class LogicalGroup : Element
        {
            public LogicalGroup(LogicalOperator @operator, IReadOnlyCollection<Element> elements)
            {
                LogicalOperator = @operator;
                Elements = elements;
            }

            public LogicalOperator LogicalOperator { get; }
            public IReadOnlyCollection<Element> Elements { get; }
        }

        public static Parser<string> NodeItem =
            from firstLetter in Parse.Letter.AtLeastOnce().Text()
            from theRest in Parse.LetterOrDigit.Many().Text()
            select firstLetter + theRest;

        public static Parser<string> EscapedNodeItem =
            from lBracket in Parse.Char('[')
            from first in Parse.LetterOrDigit.XAtLeastOnce().Text()
            from theRest in Parse.CharExcept("{}\r\n[]").XMany().Text()
            from rBracket in Parse.Char(']')
            select first + theRest;

        public static Parser<FunctionElement> FunctionItem =
           (from name in Parse.Letter.XAtLeastOnce().Text()
            from lParen in Parse.Char('(')
            from args in ValParser.DelimitedBy(Parse.Char(',').Token()).Optional()
            from rParen in Parse.Char(')')
            select new FunctionElement(name, args.IsDefined ? args.Get().ToList() : new List<Val>())).Token().Named("function");

        public static Parser<string> Node = NodeItem.Or(EscapedNodeItem).XDelimitedBy(Parse.Char('.'))
            .Select(items => string.Join(".", items))
            .Token()
            .Named("node");

        public static Parser<LogicalOperator> And = Parse.String("AND").Token().Return(LogicalOperator.And);
        public static Parser<LogicalOperator> Or = Parse.String("OR").Token().Return(LogicalOperator.Or);

        public static Parser<Operator> GreaterThanOrEquals = Parse.String(">=").Token().Return(Operator.GreaterThanOrEquals);
        public static Parser<Operator> LessThanOrEquals = Parse.String("<=").Token().Return(Operator.LessThanOrEquals);
        public static Parser<Operator> EqualsOp = Parse.Char('=').Token().Return(Operator.Equals);
        public static Parser<Operator> GreaterThan = Parse.Char('>').Token().Return(Operator.GreaterThan);
        public static Parser<Operator> LessThan = Parse.Char('<').Token().Return(Operator.LessThan);

        public static Parser<string> String =
        (from qStart in Parse.Char('"')
            from str in Parse.CharExcept("\"\r\n").XMany().Text()
            from qEnd in Parse.Char('"')
            select str).Token();

        public static Parser<bool> Bool = Parse.IgnoreCase("true").Or(Parse.IgnoreCase("false")).Text().Select(x => x.ToLower() == "true");

        public static Parser<decimal> Decimal = Parse.DecimalInvariant.Select(s => decimal.Parse(s.Replace(',', '.'), NumberStyles.Any, NumberFormatInfo.InvariantInfo));

        public static Parser<Val> ValParser =
            String.Select(s => new Val(s))
                .XOr(Bool.Select(b => new Val(b))
                .XOr(Decimal.Select(d => new Val(d))));

        public static Parser<BinaryElement> BinaryParser = 
           (from node in FunctionItem.Or(Node.Select(n => new NodeElement(n)))
            from op in GreaterThanOrEquals.XOr(LessThanOrEquals).XOr(EqualsOp).XOr(LessThan).XOr(GreaterThan)
            from val in ValParser
            select new BinaryElement(node, op, val)).Named("binary");

        public static Parser<LogicalGroup> OrGroup = GroupParser(Or);
        public static Parser<LogicalGroup> AndGroup = GroupParser(And);

        public static Parser<LogicalGroup> InnerGroup = 
           (from lParen in Parse.Char('(')
            from ls in Parse.WhiteSpace.Many().Optional()
            from g in AndGroup.Or(OrGroup)
            from rs in Parse.WhiteSpace.Many().Optional()
            from rParen in Parse.Char(')')
            select g).XOr(AndGroup.Or(OrGroup));

        public static Parser<LogicalGroup> Group =
            from ls in Parse.WhiteSpace.Many().Optional()
            from g in InnerGroup
            from rs in Parse.WhiteSpace.Many().Optional()
            select g;

        public static Parser<LogicalGroup> Filter = Group.End();

        private static Parser<LogicalGroup> GroupParser(Parser<LogicalOperator> op)
        {
            var binOrGroup = BinaryParser.Select(e => (Element)e).XOr(Parse.Ref(() => Group));

            return
                from first in binOrGroup
                from p in op
                from second in binOrGroup
                from theRest in (
                    from pN in op
                    from n in binOrGroup
                    select n
                ).Many()
                select new LogicalGroup(p, new[] { first, second }.Concat(theRest).ToList());
        }
    }
}