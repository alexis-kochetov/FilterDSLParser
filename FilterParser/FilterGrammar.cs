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
            select firstNode + string.Join(string.Empty, theRest)).Token().Named("node");

        public static Parser<BinaryElement> BinaryParser = 
            from node in Node
            from op in GreaterThanOrEquals.XOr(LessThanOrEquals).XOr(EqualsOp).XOr(LessThan).XOr(GreaterThan)
            from val in String.Select(s => (object)s).XOr(Bool.Select(b => (object)b)).XOr(Decimal.Select(d => (object)d))
            select new BinaryElement(new NodeElement(node), op, val);

       

//        static readonly Parser<Expression> Factor =
//            (from lparen in Parse.Char('(')
//                from expr in Parse.Ref(() => Expr)
//                from rparen in Parse.Char(')')
//                select expr).Named("expression")
//            .XOr(Constant);
//
//        static readonly Parser<Expression> Operand =
//        ((from sign in Parse.Char('-')
//            from factor in Factor
//            select Expression.Negate(factor)
//        ).XOr(Factor)).Token();
//
//        static readonly Parser<Expression> Term = Parse.XChainOperator(Multiply.XOr(Divide), Operand, Expression.MakeBinary);
//
//        static readonly Parser<Expression> Expr = Parse.XChainOperator(Add.XOr(Subtract), Term, Expression.MakeBinary);

        //        public static Parser<Element> ElementParser = 


        public static Parser<LogicalOperator> And = Parse.String("AND").Token().Return(LogicalOperator.And);
        public static Parser<LogicalOperator> Or = Parse.String("OR").Token().Return(LogicalOperator.Or);

        public static Parser<Operator> GreaterThanOrEquals = Parse.String(">=").Token().Return(Operator.GreaterThanOrEquals);
        public static Parser<Operator> LessThanOrEquals = Parse.String("<=").Token().Return(Operator.LessThanOrEquals);
        public static Parser<Operator> EqualsOp = Parse.Char('=').Token().Return(Operator.Equals);
        public static Parser<Operator> GreaterThan = Parse.Char('>').Token().Return(Operator.GreaterThan);
        public static Parser<Operator> LessThan = Parse.Char('<').Token().Return(Operator.LessThan);

        public static Parser<LogicalGroup> OrGroup = GroupParser(Or);
        public static Parser<LogicalGroup> AndGroup = GroupParser(And);

        private static Parser<LogicalGroup> GroupParser(Parser<LogicalOperator> op)
        {
            return
                from first in BinaryParser
                from p in op
                from second in BinaryParser
                from theRest in (
                    from pN in op
                    from n in BinaryParser
                    select n
                ).Many()
                select new LogicalGroup(p, new[] { first, second }.Concat(theRest).ToList());
        }

        public static Parser<string> String = 
           (from qStart in Parse.Char('"')
            from str in Parse.CharExcept("\"\r\n").XMany().Text()
            from qEnd in Parse.Char('"')
            select str).Token();

        public static Parser<bool> Bool = Parse.IgnoreCase("true").Or(Parse.IgnoreCase("false")).Text().Select(x => x.ToLower() == "true");

        public static Parser<decimal> Decimal = Parse.DecimalInvariant.Select(s => decimal.Parse(s.Replace(',','.'), NumberStyles.Any, NumberFormatInfo.InvariantInfo));


    }
}