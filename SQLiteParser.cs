using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SQLite
{
    public class SQLiteParser
    {
        public static string SelectWord = "SELECT";
        public static string FromWord = "FROM";

        public static string JoinWord = "JOIN";
        public static string InnerWord = "INNER";
        public static string LeftWord = "LEFT";
        public static string RightWord = "RIGHT";
        public static string OuterWord = "OUTER";

        public static string OnWord = "ON";

        public static string WhereWord = "WHERE";
        public static string GroupWord = "GROUP";
        public static string GroupByWord = "BY";

        public static string HavingWord = "HAVING";

        public Node[] Parse(string command)
        {
            List<Node> tokens = new List<Node>();
            var tokenReader = new SQLiteTokenReader((command + "").Trim());

            if (tokenReader.EqualTo(SelectWord))
            {
                ParseSelectOrGroupBy(tokenReader, tokens);
            }

            return tokens.ToArray();
        }

        private bool ParseWhereOrHaving(SQLiteTokenReader tokenReader, List<Node> tokens, bool isHaving = false, bool isOnClause = false)
        {
            tokenReader.MoveNext();
            if (isOnClause)
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.OnClause });
            }
            else
            {
                if (isHaving)
                {
                    tokens.Add(new Node() { Code = JSqlOpCodes.HavingClause });
                }
                else
                {
                    tokens.Add(new Node() { Code = JSqlOpCodes.WhereClause });
                }
            }


            do
            {
                if (tokenReader.EqualTo("("))
                {
                    // start of epxression...
                    tokens.Add(new Node() { Code = JSqlOpCodes.OpenBrackets });
                    ParseExpression(tokenReader, tokens, isHaving, isOnClause);

                    if (!tokenReader.EqualTo(")"))
                    {
                        throw new Exception("Expected closing ')'");
                    }
                    else
                    {
                        tokens.Add(new Node() { Code = JSqlOpCodes.CloseBrackets });
                        tokenReader.MoveNext();
                    }
                }
                else
                {
                    ParseVarOrLit(tokenReader, tokens);
                }
                if (tokenReader.CanMoveNext)
                {
                    if (!isOnClause && !isHaving && tokenReader.EqualTo(GroupWord, GroupByWord))
                    {
                        ParseSelectOrGroupBy(tokenReader, tokens);
                        return false;
                    }
                    else if (!isOnClause && !isHaving && tokenReader.EqualTo(HavingWord))
                    {
                        ParseWhereOrHaving(tokenReader, tokens, true);
                        return false;
                    }
                    else if (tokenReader.EqualTo(")"))
                    {
                        // inside expression.
                        return false;
                    }
                    else
                    {
                        int prevpos = tokenReader._pos;
                        if (ParseSymbol(tokenReader, tokens))
                        {
                            // finished
                            return true;
                        }
                        if (tokenReader._pos == prevpos)
                        {
                            if (isOnClause)
                            {
                                // process in caller!
                                return false;
                            }
                            else
                            {
                                if (isHaving)
                                {
                                    throw new Exception("Expected , or ;");
                                }
                                else
                                {
                                    throw new Exception("Expected , or ; or Group By or Having!");
                                }
                            }
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            while (true);
        }

        private bool ParseSymbol(SQLiteTokenReader tokenReader, List<Node> tokens)
        {
            if (tokenReader.EqualTo("+"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Plus });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("-"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Minus });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("/"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Div });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("*"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Mul });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo(">"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Larger });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("<"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Smaller });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("="))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Equal });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("<", ">") || tokenReader.EqualTo("!", "="))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.NotEqual });
                tokenReader.MoveNext(2);
            }
            else if (tokenReader.EqualTo("|") || tokenReader.EqualTo("or"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Or });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("^"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.BitWiseOr });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("&") || tokenReader.EqualTo("and"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.And });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("%"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Mod });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo("&", "&"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.And });
                tokenReader.MoveNext(2);
            }
            else if (tokenReader.EqualTo("|", "|"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Or });
                tokenReader.MoveNext(2);
            }
            else if (tokenReader.EqualTo(">", "="))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.LargerEqual });
                tokenReader.MoveNext(2);
            }
            else if (tokenReader.EqualTo("<", "="))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.SmallerEqual });
                tokenReader.MoveNext(2);
            }
            else if (tokenReader.EqualTo(">", ">"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.BitShiftRight });
                tokenReader.MoveNext(2);
            }
            else if (tokenReader.EqualTo("<", "<"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.BitShiftLeft });
                tokenReader.MoveNext(2);
            }
            else if (tokenReader.EqualTo("!") || tokenReader.EqualTo("not"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.Not });
                tokenReader.MoveNext();
            }
            else if (tokenReader.EqualTo(";"))
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.EndOfQuery });
                if (tokenReader.CanMoveNext)
                    tokenReader.MoveNext();
                return true;
            }

            return false;
        }

        private void ParseFrom(SQLiteTokenReader tokenReader, List<Node> tokens)
        {
            tokenReader.MoveNext();
            tokens.Add(new Node() { Code = JSqlOpCodes.FromClause });
            bool skip = false;
            do
            {
                if (!skip)
                {
                    if (tokenReader.EqualTo("("))
                    {
                        // start of epxression...
                        tokens.Add(new Node() { Code = JSqlOpCodes.OpenBrackets });
                        ParseExpression(tokenReader, tokens);

                        if (!tokenReader.EqualTo(")"))
                        {
                            throw new Exception("Expected closing ')'");
                        }
                        else
                        {
                            tokens.Add(new Node() { Code = JSqlOpCodes.CloseBrackets });
                            tokenReader.MoveNext();
                        }

                        if (tokenReader.EqualTo("`"))
                        {
                            tokens.Add(new NodeString() { Code = JSqlOpCodes.Alias, Data1 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2) });
                            tokenReader.MoveNext();
                        }
                        else
                        {
                            throw new Exception("From Clause on Expression needs Alias!");
                        }
                    }
                    else
                    {
                        ParseTableName(tokenReader, tokens);
                    }
                }
                else
                {
                    skip = false;
                }

                if (tokenReader.CanMoveNext)
                {
                    if (tokenReader.EqualTo(WhereWord))
                    {
                        ParseWhereOrHaving(tokenReader, tokens);
                        return;
                    }
                    else if (tokenReader.EqualTo(GroupWord, GroupByWord))
                    {
                        ParseSelectOrGroupBy(tokenReader, tokens, true);
                        return;
                    }
                    else if (tokenReader.EqualTo(HavingWord))
                    {
                        ParseWhereOrHaving(tokenReader, tokens, true);
                        return;
                    }
                    else if (tokenReader.EqualTo(InnerWord, JoinWord))
                    {
                        // continue;
                        tokens.Add(new Node() { Code = JSqlOpCodes.InnerJoin });
                        tokenReader.MoveNext(2);
                    }
                    else if (tokenReader.EqualTo(LeftWord, JoinWord))
                    {
                        // continue;
                        tokens.Add(new Node() { Code = JSqlOpCodes.LeftJoin });
                        tokenReader.MoveNext(2);
                    }
                    else if (tokenReader.EqualTo(RightWord, JoinWord))
                    {
                        // continue;
                        tokens.Add(new Node() { Code = JSqlOpCodes.RightJoin });
                        tokenReader.MoveNext(2);
                    }
                    else if (tokenReader.EqualTo(OuterWord, JoinWord))
                    {
                        // continue;
                        tokens.Add(new Node() { Code = JSqlOpCodes.OuterJoin });
                        tokenReader.MoveNext(2);
                    }
                    else if (tokenReader.EqualTo(OnWord))
                    {
                        // continue;
                        if (ParseWhereOrHaving(tokenReader, tokens, false, true))
                        {
                            return;
                        }
                    }
                    else if (tokenReader.EqualTo(","))
                    {
                        // continue;
                        tokenReader.MoveNext();
                    }
                    else if (tokenReader.Current.StartsWith("`"))
                    {
                        tokens.Add(new NodeString() { Code = JSqlOpCodes.Alias, Data1 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2) });
                        tokenReader.MoveNext();
                        skip = true;
                    }
                    else if (tokenReader.EqualTo(";"))
                    {
                        tokens.Add(new Node() { Code = JSqlOpCodes.EndOfQuery });
                        tokenReader.MoveNext();
                        return;
                    }
                    else if (tokenReader.EqualTo(")"))
                    {
                        // inside expression.
                        return;
                    }
                    else
                    {
                        throw new Exception("Expected , or ; or from!");
                    }
                }
                else
                {
                    return;
                }
            }
            while (true);
        }

        private void ParseSelectOrGroupBy(SQLiteTokenReader tokenReader, List<Node> tokens, bool isGroupBy = false)
        {
            tokenReader.MoveNext();
            if (isGroupBy)
            {
                tokenReader.MoveNext();
                tokens.Add(new Node() { Code = JSqlOpCodes.GroupClause });

            }
            else
            {
                tokens.Add(new Node() { Code = JSqlOpCodes.SelectClause });
            }
            bool skip = false;
            // we need to long all items....
            do
            {
                if (!skip)
                {
                    if (tokenReader.EqualTo("("))
                    {
                        // start of epxression...
                        tokens.Add(new Node() { Code = JSqlOpCodes.OpenBrackets });
                        ParseExpression(tokenReader, tokens, false, isGroupBy);

                        if (!tokenReader.EqualTo(")"))
                        {
                            throw new Exception("Expected closing ')'");
                        }
                        else
                        {
                            tokens.Add(new Node() { Code = JSqlOpCodes.CloseBrackets });
                            tokenReader.MoveNext();
                        }
                    }
                    else if (!isGroupBy && tokenReader.EqualTo("*"))
                    {
                        tokens.Add(new Node() { Code = JSqlOpCodes.SelectAll });
                        tokenReader.MoveNext();
                    }
                    else
                    {
                        ParseVarOrLit(tokenReader, tokens);
                    }
                }
                else
                {
                    skip = false;
                }

                if (tokenReader.CanMoveNext)
                {
                    if (!isGroupBy && tokenReader.EqualTo(FromWord))
                    {
                        ParseFrom(tokenReader, tokens);
                        return;
                    }
                    else if (isGroupBy && tokenReader.EqualTo(HavingWord))
                    {
                        ParseWhereOrHaving(tokenReader, tokens, true);
                        return;
                    }
                    else if (tokenReader.EqualTo(","))
                    {
                        // continue;
                        tokenReader.MoveNext();
                    }
                    else if (!isGroupBy && tokenReader.Current.StartsWith("`"))
                    {
                        tokens.Add(new NodeString() { Code = JSqlOpCodes.Alias, Data1 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2) });
                        tokenReader.MoveNext();
                        skip = true;
                    }
                    else if (tokenReader.EqualTo(";"))
                    {
                        tokens.Add(new Node() { Code = JSqlOpCodes.EndOfQuery });
                        tokenReader.MoveNext();
                        return;
                    }
                    else if (tokenReader.EqualTo(")"))
                    {
                        // inside expression.
                        return;
                    }
                    else
                    {
                        int prevpos = tokenReader._pos;
                        ParseSymbol(tokenReader, tokens);
                        if (tokenReader._pos == prevpos)
                        {
                            throw new Exception("Expected , or ; or from!");
                        }
                    }
                }
                else
                {
                    return;
                }
            }
            while (true);
        }

        private bool ParseVarOrLit(SQLiteTokenReader tokenReader, List<Node> tokens)
        {
            if (tokenReader.Current.StartsWith("`"))
            {
                // name of column...
                string data1 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2);
                string data2 = "";
                string data3 = "";

                tokenReader.MoveNext();

                if (tokenReader.Current == ".")
                {
                    tokenReader.MoveNext();

                    if (tokenReader.Current.StartsWith("`"))
                    {
                        data2 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2);

                        tokenReader.MoveNext();

                        if (tokenReader.Current == ".")
                        {
                            tokenReader.MoveNext();

                            if (tokenReader.Current.StartsWith("`"))
                            {
                                data3 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2);

                                tokenReader.MoveNext();

                                tokens.Add(new NodeString3()
                                {
                                    Code = JSqlOpCodes.SelectName,
                                    Data1 = data3,
                                    Data2 = data2,
                                    Data3 = data1
                                });
                            }
                            else
                            {
                                throw new Exception("Expected ` after .");
                            }
                        }
                        else
                        {
                            tokens.Add(new NodeString2()
                            {
                                Code = JSqlOpCodes.SelectName,
                                Data1 = data2,
                                Data2 = data1,
                            });
                        }
                    }
                    else
                    {
                        throw new Exception("Expected ` after .");
                    }
                }
                else
                {
                    tokens.Add(new NodeString()
                    {
                        Code = JSqlOpCodes.SelectName,
                        Data1 = data1
                    });
                }

                return true;
            }
            else if (tokenReader.Current.StartsWith("'"))
            {
                tokens.Add(new NodeString()
                {
                    Code = JSqlOpCodes.SelectString,
                    Data1 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2)
                });
            }
            else if (tokenReader.IsNumberLiteral())
            {
                // add literal.
                tokens.Add(new NodeNumber() { Code = JSqlOpCodes.SelectNumber, Number1 = tokenReader.GetValue() });
            }
            else if (tokenReader.Current.StartsWith("@"))
            {
                // add argument placement.
                tokens.Add(new NodeString() { Code = JSqlOpCodes.ArgumentPlacement, Data1 = tokenReader.Current.Substring(1) });
            }
            else
            {
                throw new Exception("Expected Value or Literal or Argument");
            }
            tokenReader.MoveNext();
            return false;
        }

        private void ParseTableName(SQLiteTokenReader tokenReader, List<Node> tokens)
        {
            if (tokenReader.Current.StartsWith("`"))
            {
                // name of column...
                string data1 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2);
                string data2 = "";

                tokenReader.MoveNext();

                if (tokenReader.Current.StartsWith("."))
                {
                    tokenReader.MoveNext();
                    if (tokenReader.Current.StartsWith("`"))
                    {
                        data2 = tokenReader.Current.Substring(1, tokenReader.Current.Length - 2);
                        tokenReader.MoveNext();
                        tokens.Add(new NodeString2() { Code = JSqlOpCodes.FromField, Data1 = data1, Data2 = data2 });
                    }
                    else
                    {
                        throw new Exception("Expected ` after . in from");
                    }
                }
                else
                {
                    tokens.Add(new NodeString() { Code = JSqlOpCodes.FromField, Data1 = data1 });
                }
            }
            else
            {
                throw new Exception("Expected Value or Literal or Argument");
            }
        }

        private void ParseExpression(SQLiteTokenReader tokenReader, List<Node> tokens, bool isHaving = false, bool isGroupBy = false, bool isOnClause = false)
        {
            tokenReader.MoveNext();

            if (tokenReader.EqualTo(SelectWord))
            {
                if (isHaving)
                {
                    throw new Exception("Cannot have select in a having clause!");
                }
                if (isGroupBy)
                {
                    throw new Exception("Cannot have select in a group by clause!");
                }
                if (isOnClause)
                {
                    throw new Exception("Cannot have select in a on clause!");
                }
                ParseSelectOrGroupBy(tokenReader, tokens);
            }
            else
            {
                do
                {
                    if (tokenReader.EqualTo("("))
                    {
                        // start of epxression...
                        tokens.Add(new Node() { Code = JSqlOpCodes.OpenBrackets });
                        ParseExpression(tokenReader, tokens, isHaving);

                        if (!tokenReader.EqualTo(")"))
                        {
                            throw new Exception("Expected closing ')'");
                        }
                        else
                        {
                            tokens.Add(new Node() { Code = JSqlOpCodes.CloseBrackets });
                            tokenReader.MoveNext();
                        }
                    }
                    else
                    {
                        ParseVarOrLit(tokenReader, tokens);
                    }
                    if (tokenReader.CanMoveNext)
                    {
                        if (!isOnClause && !isHaving && !isGroupBy && tokenReader.EqualTo(GroupWord, GroupByWord))
                        {
                            ParseSelectOrGroupBy(tokenReader, tokens, true);
                            return;
                        }
                        else if (!isOnClause && !isHaving && tokenReader.EqualTo(HavingWord))
                        {
                            ParseWhereOrHaving(tokenReader, tokens, true);
                            return;
                        }
                        else if (tokenReader.EqualTo(")"))
                        {
                            // inside expression.
                            return;
                        }
                        else
                        {
                            int prevpos = tokenReader._pos;
                            ParseSymbol(tokenReader, tokens);
                            if (tokenReader._pos == prevpos)
                            {
                                if (isOnClause)
                                {
                                    return;
                                }
                                else
                                {
                                    throw new Exception("Expected , or ; or from!");
                                }
                            }
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                while (true);
            }
        }
    }

    public enum JSqlOpCodes
    {
        SelectClause,
        SelectNumber,
        SelectString,
        SelectName,
        SelectAll,
        FromClause,
        FromField,
        Alias,
        ArgumentPlacement,
        WhereClause,
        HavingClause,
        GroupClause,
        OpenBrackets,
        CloseBrackets,
        Plus,
        Minus,
        Div,
        Mul,
        Larger,
        Smaller,
        Equal,
        NotEqual,
        Or,
        BitWiseOr,
        And,
        Mod,
        LargerEqual,
        SmallerEqual,
        BitShiftRight,
        BitShiftLeft,
        Not,
        InnerJoin,
        LeftJoin,
        RightJoin,
        OnClause,
        OuterJoin,
        EndOfQuery // ;
    }

    public class Node
    {
        public JSqlOpCodes Code;
    }

    public class NodeNumber : Node
    {
        public decimal Number1;
    }

    public class NodeString : Node
    {
        public string Data1;
    }
    public class NodeString2 : NodeString
    {
        public string Data2;
    }

    public class NodeString3 : NodeString2
    {
        public string Data3;
    }
}
